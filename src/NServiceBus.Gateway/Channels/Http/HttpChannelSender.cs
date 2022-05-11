namespace NServiceBus.Gateway.Channels.Http
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;

    [ChannelType("http")]
    [ChannelType("https")]
    class HttpChannelSender : IChannelSender
    {
        static HttpChannelSender()
        {
            httpClientHandler = new HttpClientHandler
            {
                UseDefaultCredentials = true
            };
        }

        public async Task Send(string remoteUrl, IDictionary<string, string> headers, Stream data, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, remoteUrl)
            {
                Content = new StreamContent(data)
            };

            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            foreach (var pair in headers)
            {
                if (!request.Headers.TryAddWithoutValidation(pair.Key, pair.Value))
                {
                    if (!request.Content.Headers.TryAddWithoutValidation(pair.Key, pair.Value))
                    {
                        throw new Exception("Invalid header");
                    }
                }
            }

            HttpStatusCode statusCode;

            //todo make the receiver send the md5 back so that we can double check that the transmission went ok
            using (var httpClient = new HttpClient(httpClientHandler, disposeHandler: false))
            {
                var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                statusCode = response.StatusCode;
            }

            Logger.Debug("Got HTTP response with status code " + statusCode);

            if (statusCode != HttpStatusCode.OK)
            {
                Logger.Warn("Message not transferred successfully. Trying again...");
                throw new Exception("Retrying");
            }
        }

        static ILog Logger = LogManager.GetLogger<HttpChannelSender>();
        static readonly HttpClientHandler httpClientHandler;
    }
}