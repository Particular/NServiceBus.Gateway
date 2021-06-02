namespace NServiceBus.Gateway.Channels.Http
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using Logging;
    using Receiving;

    class HttpChannelReceiver : IChannelReceiver
    {
        public void Start(string address, int maxConcurrency, Func<DataReceivedOnChannelArgs, CancellationToken, Task> dataReceivedOnChannel)
        {
            dataReceivedHandler = dataReceivedOnChannel;

            concurrencyLimiter = new SemaphoreSlim(maxConcurrency, maxConcurrency);

            messageReceivingCancellationTokenSource = new CancellationTokenSource();
            messageProcessingCancellationTokenSource = new CancellationTokenSource();

            listener = new HttpListener();

            try
            {
                listener.Prefixes.Add(address);
            }
            catch (Exception ex)
            {
                var message = $"Unable to listen on {address}";
                throw new Exception(message, ex);
            }

            try
            {
                listener.Start();
            }
            catch (Exception ex)
            {
                var message = $"Failed to start listener for {address} make sure that you have admin privileges";
                throw new Exception(message, ex);
            }

            // Task.Run() so the call returns immediately instead of waiting for the first await or return down the call stack
            messageReceivingTask = Task.Run(() => ReceiveMessagesAndSwallowExceptions(messageReceivingCancellationTokenSource.Token), CancellationToken.None);
        }

        public async Task Stop(CancellationToken cancellationToken = default)
        {
            Logger.InfoFormat("Stopping channel - {0}", typeof(HttpChannelReceiver));

            messageReceivingCancellationTokenSource?.Cancel();

            using (cancellationToken.Register(() => messageProcessingCancellationTokenSource?.Cancel()))
            {
                listener?.Close();

                var allTasks = messageProcessingTasks.Values.Concat(new[] { messageReceivingTask });

                await Task.WhenAll(allTasks).ConfigureAwait(false);
            }

            concurrencyLimiter.Dispose();
            messageProcessingTasks.Clear();
            messageReceivingCancellationTokenSource?.Dispose();
            messageProcessingCancellationTokenSource?.Dispose();
        }

        async Task ReceiveMessagesAndSwallowExceptions(CancellationToken messageReceivingCancellationToken)
        {
            while (!messageReceivingCancellationToken.IsCancellationRequested)
            {
#pragma warning disable PS0021 // Highlight when a try block passes multiple cancellation tokens - justification:
                // The message processing cancellation token is being used for processing messages,
                // since we want that only to be canceled when the public token passed to Stop() is canceled.
                // The message receiving token is being used elsewhere, because we want those operations to be canceled as soon as Stop() is called.
                // The catch clause is correctly filtered on the message receiving cancellation token.
                try
#pragma warning restore PS0021 // Highlight when a try block passes multiple cancellation tokens
                {
                    HttpListenerContext context;

                    try
                    {
                        context = await listener.GetContextAsync().ConfigureAwait(false);
                    }
                    catch (HttpListenerException ex) when (messageReceivingCancellationToken.IsCancellationRequested)
                    {
                        Logger.Debug("Assuming HttpListener.GetContextAsync() failed due to the receiver stopping.", ex);
                        break;
                    }
                    catch (ObjectDisposedException ex) when (messageReceivingCancellationToken.IsCancellationRequested && ex.ObjectName == typeof(HttpListener).FullName)
                    {
                        Logger.Debug("Assuming HttpListener.GetContextAsync() failed due to the receiver stopping.", ex);
                        break;
                    }

                    await concurrencyLimiter.WaitAsync(messageReceivingCancellationToken).ConfigureAwait(false);

                    // no Task.Run() here to avoid a closure
                    var messageProcessingTask = ProcessMessageSwallowExceptionsAndReleaseConcurrencyLimiter(context, messageProcessingCancellationTokenSource.Token);

                    _ = messageProcessingTasks.TryAdd(messageProcessingTask, messageProcessingTask);

                    _ = messageProcessingTask.ContinueWith(__ => _ = messageProcessingTasks.TryRemove(messageProcessingTask, out _), TaskContinuationOptions.ExecuteSynchronously);
                }
                catch (Exception ex) when (ex.IsCausedBy(messageReceivingCancellationToken))
                {
                    // private token, poller is being canceled, log exception in case stack trace is ever needed for debugging
                    Logger.Debug("Operation canceled while stopping HTTP channel receiver.", ex);
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Error("Gateway failed to receive incoming request.", ex);
                    break;
                }
            }
        }

        async Task ProcessMessageSwallowExceptionsAndReleaseConcurrencyLimiter(HttpListenerContext context, CancellationToken messageProcessingCancellationToken)
        {
            try
            {
                var headers = GetHeaders(context);
                var dataStream = await GetMessageStream(context, messageProcessingCancellationToken).ConfigureAwait(false);

                await dataReceivedHandler(
                    new DataReceivedOnChannelArgs
                    {
                        Headers = headers,
                        Data = dataStream
                    },
                    messageProcessingCancellationToken).ConfigureAwait(false);

                ReportSuccess(context);

                Logger.Debug("Http request processing complete.");
            }
            catch (Exception ex) when (ex.IsCausedBy(messageProcessingCancellationToken))
            {
                Logger.Debug("Message processing canceled.", ex);
            }
            catch (ChannelException ex)
            {
                TryReportFailure(context, ex.Message, ex.StatusCode);
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error", ex);
                TryReportFailure(context, "Unexpected server error", 502);
            }
            finally
            {
                concurrencyLimiter.Release();
            }
        }

        static async Task<MemoryStream> GetMessageStream(HttpListenerContext context, CancellationToken messageProcessingCancellationToken)
        {
            if (context.Request.QueryString.AllKeys.Contains("Message"))
            {
                var message = HttpUtility.UrlDecode(context.Request.QueryString["Message"]);

                return new MemoryStream(Encoding.UTF8.GetBytes(message));
            }

            var streamToReturn = new MemoryStream();

            await context.Request.InputStream.CopyToAsync(streamToReturn, MaximumBytesToRead, messageProcessingCancellationToken).ConfigureAwait(false);
            streamToReturn.Position = 0;

            return streamToReturn;
        }

        static IDictionary<string, string> GetHeaders(HttpListenerContext context)
        {
            var headers = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

            foreach (string header in context.Request.Headers.Keys)
            {
                headers.Add(HttpUtility.UrlDecode(header), HttpUtility.UrlDecode(context.Request.Headers[header]));
            }

            foreach (string header in context.Request.QueryString.Keys)
            {
                headers[HttpUtility.UrlDecode(header)] = HttpUtility.UrlDecode(context.Request.QueryString[header]);
            }

            return headers;
        }

        static void ReportSuccess(HttpListenerContext context)
        {
            Logger.Debug("Sending HTTP 200 response.");

            context.Response.StatusCode = 200;
            context.Response.StatusDescription = "OK";

            WriteData(context, "OK");
        }

        static void WriteData(HttpListenerContext context, string status)
        {
            var newStatus = status;

            var jsonCallback = context.Request.QueryString["callback"];
            if (string.IsNullOrEmpty(jsonCallback) == false)
            {
                newStatus = jsonCallback + "({ status: '" + newStatus + "'})";
                context.Response.AddHeader("Content-Type", "application/javascript; charset=utf-8");
            }
            else
            {
                context.Response.AddHeader("Content-Type", "application/json; charset=utf-8");
            }
            context.Response.Close(Encoding.ASCII.GetBytes(newStatus), false);
        }

        static void TryReportFailure(HttpListenerContext context, string warning, int statusCode)
        {
            try
            {
                Logger.WarnFormat("Cannot process HTTP request from {0}. Reason: {1}.", context.Request.RemoteEndPoint, warning);
                context.Response.StatusCode = statusCode;
                context.Response.StatusDescription = warning;

                WriteData(context, warning);
            }
            catch (Exception ex)
            {
                Logger.Error("Could not return warning to client.", ex);
            }
        }

        const int MaximumBytesToRead = 100000;

        static readonly ILog Logger = LogManager.GetLogger<HttpChannelReceiver>();

        readonly ConcurrentDictionary<Task, Task> messageProcessingTasks = new ConcurrentDictionary<Task, Task>();

        SemaphoreSlim concurrencyLimiter;
        HttpListener listener;
        CancellationTokenSource messageReceivingCancellationTokenSource;
        CancellationTokenSource messageProcessingCancellationTokenSource;
        Task messageReceivingTask;
        Func<DataReceivedOnChannelArgs, CancellationToken, Task> dataReceivedHandler;
    }
}
