namespace NServiceBus.Gateway.Sending
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Channels.Http;
    using DataBus;
    using HeaderManagement;
    using Logging;
    using Routing;
    using Utils;

    class SingleCallChannelForwarder
    {
        public SingleCallChannelForwarder(Func<string, IChannelSender> senderFactory, IDataBus databus)
        {
            this.senderFactory = senderFactory;
            this.databus = databus;
        }

        public async Task Forward(byte[] body, Dictionary<string, string> headers, Site targetSite)
        {
            var toHeaders = MapToHeaders(headers);

            var channelSender = senderFactory(targetSite.Channel.Type);

            //databus properties have to be available at the receiver site
            //before the body of the message is forwarded on the bus
            await TransmitDataBusProperties(channelSender, targetSite, toHeaders).ConfigureAwait(false);

            using (var messagePayload = new MemoryStream(body))
            {
                await Transmit(channelSender, targetSite, CallType.SingleCallSubmit, toHeaders, messagePayload).ConfigureAwait(false);
            }
        }

        Dictionary<string,string> MapToHeaders(Dictionary<string, string> fromHeaders)
        {
            var to = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase)
            {
                [NServiceBus + Id] = fromHeaders[Headers.MessageId],
                [NServiceBus + CorrelationId] = fromHeaders[Headers.CorrelationId]
            };

            if (fromHeaders.ContainsKey(Headers.NonDurableMessage))
            {
                var recoverable = true;
                bool nonDurable;
                if (bool.TryParse(fromHeaders[Headers.NonDurableMessage], out nonDurable))
                {
                    recoverable = !nonDurable;
                }

                to.Add(NServiceBus + Recoverable, recoverable.ToString());
            }

            if (fromHeaders.ContainsKey(Headers.TimeToBeReceived))
            {
                to.Add(NServiceBus + TimeToBeReceived, fromHeaders[Headers.TimeToBeReceived]);
            }

            string reply;
            if (fromHeaders.TryGetValue(Headers.ReplyToAddress, out reply)) //Handles SendOnly endpoints, where ReplyToAddress is not set
            {
                to[NServiceBus + ReplyToAddress] = reply;
            }

            string replyToAddress;
            if (fromHeaders.TryGetValue(ReplyToAddress, out replyToAddress))
            {
                to[Headers.RouteTo] = replyToAddress;
            }

            fromHeaders.ToList()
                .ForEach(header => to[NServiceBus + Headers.HeaderName + "." + header.Key] = header.Value);

            return to;
        }

        async Task Transmit(IChannelSender channelSender, Site targetSite, CallType callType,
            IDictionary<string, string> headers, Stream data)
        {
            headers[GatewayHeaders.IsGatewayMessage] = bool.TrueString;
            headers["NServiceBus.CallType"] = Enum.GetName(typeof(CallType), callType);
            headers[HttpHeaders.ContentMD5] = await Hasher.Hash(data).ConfigureAwait(false);

            Logger.DebugFormat("Sending message - {0} to: {1}", callType, targetSite.Channel.Address);

            if (targetSite.UsesReverseProxy)
            {
                headers = headers.EncodeHeadersForReverseProxy();
            }

            await channelSender.Send(targetSite.Channel.Address, headers, data).ConfigureAwait(false);
        }

        async Task TransmitDataBusProperties(IChannelSender channelSender, Site targetSite,
            IDictionary<string, string> headers)
        {
            var headersToSend = new Dictionary<string, string>(headers);

            foreach (
                var headerKey in headers.Keys.Where(headerKey => headerKey.Contains("NServiceBus.DataBus.")))
            {
                if (databus == null)
                {
                    throw new InvalidOperationException(
                        "Can't send a message with a databus property without a databus configured");
                }

                headersToSend[GatewayHeaders.DatabusKey] = headerKey;

                var databusKeyForThisProperty = headers[headerKey];

                using (var stream = await databus.Get(databusKeyForThisProperty).ConfigureAwait(false))
                {
                    await Transmit(channelSender, targetSite, CallType.SingleCallDatabusProperty, headersToSend, stream).ConfigureAwait(false);
                }
            }
        }

        const string NServiceBus = "NServiceBus.";
        const string Id = "Id";

        const string CorrelationId = "CorrelationId";
        const string Recoverable = "Recoverable";
        const string ReplyToAddress = "ReplyToAddress";
        const string TimeToBeReceived = "TimeToBeReceived";

        static ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");
        Func<string, IChannelSender> senderFactory;
        IDataBus databus;
    }
}