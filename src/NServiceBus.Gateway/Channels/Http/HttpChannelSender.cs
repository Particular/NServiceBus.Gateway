namespace NServiceBus.Gateway.Channels.Http
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using Logging;

    [ChannelType("http")]
    [ChannelType("https")]
    class HttpChannelSender : IChannelSender
    {
        public async Task Send(string remoteUrl, IDictionary<string, string> headers, Stream data, CancellationToken cancellationToken = default)
        {
            var request = WebRequest.Create(remoteUrl);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Headers = Encode(headers);
            request.UseDefaultCredentials = true;
            request.ContentLength = data.Length;

            using (var stream = await request.GetRequestStreamAsync().ConfigureAwait(false))
            {
                // 81920 is the default value in the underlying code.
                // .NET Framework does not have an overload that accepts only Stream and CancellationToken
                await data.CopyToAsync(stream, 81920, cancellationToken).ConfigureAwait(false);
            }

            HttpStatusCode statusCode;

            try
            {
                //todo make the receiver send the md5 back so that we can double check that the transmission went ok
                using (var response = (HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false))
                {
                    statusCode = response.StatusCode;
                }
            }
            catch (WebException ex)
            {
                ex.Response?.Dispose();
                throw;
            }


            Logger.Debug("Got HTTP response with status code " + statusCode);

            if (statusCode != HttpStatusCode.OK)
            {
                Logger.Warn("Message not transferred successfully. Trying again...");
                throw new Exception("Retrying");
            }
        }

        static WebHeaderCollection Encode(IDictionary<string, string> headers)
        {
            var webHeaders = new WebHeaderCollection();

            foreach (var pair in headers)
            {
                webHeaders.Add(HttpUtility.UrlEncode(pair.Key), HttpUtility.UrlEncode(pair.Value));
            }

            return webHeaders;
        }


        static ILog Logger = LogManager.GetLogger<HttpChannelSender>();
    }
}