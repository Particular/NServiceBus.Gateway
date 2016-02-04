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
        public void Start(string address, int numberOfWorkerThreads, Func<DataReceivedOnChannelArgs, Task> dataReceivedOnChannel)
        {
            dataReceivedHandler = dataReceivedOnChannel;

            concurencyLimiter = new SemaphoreSlim(numberOfWorkerThreads, numberOfWorkerThreads);
            tokenSource = new CancellationTokenSource();
            listener = new HttpListener();

            listener.Prefixes.Add(address);

            try
            {
                listener.Start();
            }
            catch (Exception ex)
            {
                var message = $"Failed to start listener for {address} make sure that you have admin privileges";
                throw new Exception(message, ex);
            }

            var token = tokenSource.Token;
            receiverTask = Task.Factory.StartNew(HandleRequestAsync, token, token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        }

        public void Dispose()
        {
            tokenSource?.Cancel();
            listener?.Close();

            try
            {
                receiverTask.Wait(TimeSpan.FromSeconds(10));
            }
            catch (AggregateException ex)
            {
                ex.Handle(e => e is TaskCanceledException);
            }

            try
            {
                Task.WaitAll(runningTasks.Values.ToArray());
            }
            catch (AggregateException ex)
            {
                ex.Handle(e => e is TaskCanceledException);
            }

            // Do not dispose the task, see http://blogs.msdn.com/b/pfxteam/archive/2012/03/25/10287435.aspx
            concurencyLimiter?.Dispose();
        }

        private async Task HandleRequestAsync(object o)
        {
            var cancellationToken = (CancellationToken)o;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var context = await listener.GetContextAsync().ConfigureAwait(false);

                    var worker = Task.Run(() => Handle(context, cancellationToken), CancellationToken.None);
                    runningTasks.TryAdd(worker, worker);

                    worker.ContinueWith(t =>
                    {
                        Task task;
                        runningTasks.TryRemove(worker, out task);
                    }, TaskContinuationOptions.ExecuteSynchronously)
                    .Forget();
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

        async Task Handle(HttpListenerContext context, CancellationToken token)
        {
            try
            {
                await concurencyLimiter.WaitAsync(token).ConfigureAwait(false);

                await dataReceivedHandler(new DataReceivedOnChannelArgs
                {
                    Headers = GetHeaders(context),
                    Data = await GetMessageStream(context, token).ConfigureAwait(false)
                }).ConfigureAwait(false);

                ReportSuccess(context);

                Logger.Debug("Http request processing complete.");
            }
            catch (OperationCanceledException ex)
            {
                Logger.Info("Operation cancelled while shutting down the gateway", ex);
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
                concurencyLimiter.Release();
            }
        }

        static async Task<MemoryStream> GetMessageStream(HttpListenerContext context, CancellationToken token)
        {
            if (context.Request.QueryString.AllKeys.Contains("Message"))
            {
                var message = HttpUtility.UrlDecode(context.Request.QueryString["Message"]);

                return new MemoryStream(Encoding.UTF8.GetBytes(message));
            }

            var streamToReturn = new MemoryStream();

            await context.Request.InputStream.CopyToAsync(streamToReturn, MaximumBytesToRead, token).ConfigureAwait(false);
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
        Task receiverTask;
        ConcurrentDictionary<Task, Task> runningTasks = new ConcurrentDictionary<Task, Task>();
        SemaphoreSlim concurencyLimiter;
        HttpListener listener;
        CancellationTokenSource tokenSource;
        private Func<DataReceivedOnChannelArgs, Task> dataReceivedHandler;
    }
}
