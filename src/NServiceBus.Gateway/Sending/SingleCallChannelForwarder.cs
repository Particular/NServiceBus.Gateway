namespace NServiceBus.Gateway.Sending
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Channels;
    using Channels.Http;
    using DataBus;
    using HeaderManagement;
    using Logging;
    using Routing;
    using Utils;

    class SingleCallChannelForwarder : IForwardMessagesToSites
    {
        public SingleCallChannelForwarder(IChannelFactory channelFactory)
        {
            this.channelFactory = channelFactory;
        }

        public bool IsMsmqTransport { get; set; }

        public IDataBus DataBus { get; set; }

        public void Forward(byte[] body, Dictionary<string, string> headers, Site targetSite)
        {
            var toHeaders = MapToHeaders(headers);

            var channelSender = channelFactory.GetSender(targetSite.Channel.Type);

            //databus properties have to be available at the receiver site
            //before the body of the message is forwarded on the bus
            TransmitDataBusProperties(channelSender, targetSite, toHeaders);

            using (var messagePayload = new MemoryStream(body))
            {
                Transmit(channelSender, targetSite, CallType.SingleCallSubmit, toHeaders, messagePayload);
            }
        }

        Dictionary<string,string> MapToHeaders(Dictionary<string, string> fromHeaders)
        {
            var to = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase)
            {
                [NServiceBus + Id] = fromHeaders[Headers.MessageId],
                [NServiceBus + CorrelationId] = GetCorrelationForBackwardsCompatibility(fromHeaders),
                // TODO: How to handle this?
                //[NServiceBus + Recoverable] = @from.Recoverable.ToString(),
                //[NServiceBus + TimeToBeReceived] = @from.TimeToBeReceived.ToString()
            };

            string reply;
            if (fromHeaders.TryGetValue(Headers.ReplyToAddress, out reply)) //Handles SendOnly endpoints, where ReplyToAddress is not set
            {
                to[NServiceBus + ReplyToAddress] = reply;
            }

            SetBackwardsCompatibilityHeaders(to);

            string replyToAddress;
            if (fromHeaders.TryGetValue(ReplyToAddress, out replyToAddress))
            {
                to[Headers.RouteTo] = replyToAddress;
            }

            fromHeaders.ToList()
                .ForEach(header => to[NServiceBus + Headers.HeaderName + "." + header.Key] = header.Value);

            return to;
        }

        void Transmit(IChannelSender channelSender, Site targetSite, CallType callType,
            IDictionary<string, string> headers, Stream data)
        {
            headers[GatewayHeaders.IsGatewayMessage] = Boolean.TrueString;
            headers["NServiceBus.CallType"] = Enum.GetName(typeof(CallType), callType);
            headers[HttpHeaders.ContentMD5] = Hasher.Hash(data);

            Logger.DebugFormat("Sending message - {0} to: {1}", callType, targetSite.Channel.Address);

            channelSender.Send(targetSite.Channel.Address, headers, data);
        }

        void TransmitDataBusProperties(IChannelSender channelSender, Site targetSite,
            IDictionary<string, string> headers)
        {
            var headersToSend = new Dictionary<string, string>(headers);

            foreach (
                var headerKey in headers.Keys.Where(headerKey => headerKey.Contains("NServiceBus.DataBus.")))
            {
                if (DataBus == null)
                {
                    throw new InvalidOperationException(
                        "Can't send a message with a databus property without a databus configured");
                }

                headersToSend[GatewayHeaders.DatabusKey] = headerKey;

                var databusKeyForThisProperty = headers[headerKey];

                using (var stream = DataBus.Get(databusKeyForThisProperty).GetAwaiter().GetResult())
                {
                    Transmit(channelSender, targetSite, CallType.SingleCallDatabusProperty, headersToSend, stream);
                }
            }
        }

        void SetBackwardsCompatibilityHeaders(IDictionary<string, string> to)
        {
            if (IsMsmqTransport)
            {
                to[NServiceBus + IdForCorrelation] = to[NServiceBus + CorrelationId];
            }
        }

        string GetCorrelationForBackwardsCompatibility(Dictionary<string, string> headers)
        {
            var correlationIdToStore = headers[Headers.CorrelationId];

            if (IsMsmqTransport)
            {
                Guid correlationId;

                if (Guid.TryParse(headers[Headers.CorrelationId], out correlationId))
                {
                    correlationIdToStore = headers[Headers.CorrelationId] + "\\0";
                    //msmq required the id's to be in the {guid}\{incrementing number} format so we need to fake a \0 at the end to make it compatible                
                }
            }

            return correlationIdToStore;
        }

      


        const string NServiceBus = "NServiceBus.";
        const string Id = "Id";
        
        const string CorrelationId = "CorrelationId";
        const string Recoverable = "Recoverable";
        const string ReplyToAddress = "ReplyToAddress";
        const string TimeToBeReceived = "TimeToBeReceived";
        const string IdForCorrelation = "IdForCorrelation";

        static ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");
        IChannelFactory channelFactory;
    }
}