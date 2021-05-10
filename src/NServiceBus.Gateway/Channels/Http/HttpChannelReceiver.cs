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

            // CancellationToken.None because otherwise the task simply won't start if the token is cancelled
            messageReceivingTask = Task.Run(() => ReceiveMessages(messageReceivingCancellationTokenSource.Token), CancellationToken.None);
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

        async Task ReceiveMessages(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var context = await listener.GetContextAsync().ConfigureAwait(false);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    await concurrencyLimiter.WaitAsync(cancellationToken).ConfigureAwait(false);

                    var messageProcessingTask = ProcessMessage(context, messageProcessingCancellationTokenSource.Token);

                    _ = messageProcessingTasks.TryAdd(messageProcessingTask, messageProcessingTask);

                    _ = messageProcessingTask.ContinueWith(t =>
                    {
                        _ = messageProcessingTasks.TryRemove(messageProcessingTask, out _);
                    }, TaskContinuationOptions.ExecuteSynchronously);
                }
                catch (HttpListenerException ex)
                {
                    // a HttpListenerException can occur on listener.GetContext when we shutdown. this can be ignored
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        Logger.Error("Gateway failed to receive incoming request.", ex);
                    }
                    break;
                }
                catch (ObjectDisposedException ex)
                {
                    // a ObjectDisposedException can occur on listener.GetContext when we shutdown. this can be ignored
                    if (!cancellationToken.IsCancellationRequested && ex.ObjectName == typeof(HttpListener).FullName)
                    {
                        Logger.Error("Gateway failed to receive incoming request.", ex);
                    }
                    break;
                }
                catch (InvalidOperationException ex)
                {
                    Logger.Error("Gateway failed to receive incoming request.", ex);
                    break;
                }
            }
        }

        async Task ProcessMessage(HttpListenerContext context, CancellationToken cancellationToken)
        {
            try
            {
                var headers = GetHeaders(context);
                var dataStream = await GetMessageStream(context, cancellationToken).ConfigureAwait(false);

                await dataReceivedHandler(
                    new DataReceivedOnChannelArgs
                    {
                        Headers = headers,
                        Data = dataStream
                    },
                    cancellationToken).ConfigureAwait(false);

                ReportSuccess(context);

                Logger.Debug("Http request processing complete.");
            }
            catch (OperationCanceledException ex)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Logger.Debug("Operation cancelled while shutting down the gateway.", ex);
                }
                else
                {
                    Logger.Warn("Operation cancelled.", ex);
                }
            }
            catch (ChannelException ex)
            {
                CloseResponseAndWarn(context, ex.Message, ex.StatusCode);
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error", ex);
                CloseResponseAndWarn(context, "Unexpected server error", 502);
            }
            finally
            {
                concurrencyLimiter.Release();
            }
        }

        static async Task<MemoryStream> GetMessageStream(HttpListenerContext context, CancellationToken cancellationToken)
        {
            if (context.Request.QueryString.AllKeys.Contains("Message"))
            {
                var message = HttpUtility.UrlDecode(context.Request.QueryString["Message"]);

                return new MemoryStream(Encoding.UTF8.GetBytes(message));
            }

            var streamToReturn = new MemoryStream();

            await context.Request.InputStream.CopyToAsync(streamToReturn, MaximumBytesToRead, cancellationToken).ConfigureAwait(false);
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

        static void CloseResponseAndWarn(HttpListenerContext context, string warning, int statusCode)
        {
            try
            {
                Logger.WarnFormat("Cannot process HTTP request from {0}. Reason: {1}.", context.Request.RemoteEndPoint, warning);
                context.Response.StatusCode = statusCode;
                context.Response.StatusDescription = warning;

                WriteData(context, warning);
            }
            catch (Exception e)
            {
                Logger.Error("Could not return warning to client.", e);
            }
        }

        const int MaximumBytesToRead = 100000;

        static ILog Logger = LogManager.GetLogger<HttpChannelReceiver>();
        SemaphoreSlim concurrencyLimiter;
        HttpListener listener;
        CancellationTokenSource messageReceivingCancellationTokenSource;
        CancellationTokenSource messageProcessingCancellationTokenSource;
        Task messageReceivingTask;
        ConcurrentDictionary<Task, Task> messageProcessingTasks = new ConcurrentDictionary<Task, Task>();
        Func<DataReceivedOnChannelArgs, CancellationToken, Task> dataReceivedHandler;
    }
}
