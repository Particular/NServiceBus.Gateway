namespace NServiceBus.Gateway.HeaderManagement
{
    using System;
    using System.Threading.Tasks;
    using MessageMutator;

    class GatewayHeaderManager : IMutateIncomingTransportMessages, IMutateOutgoingTransportMessages, INeedInitialization
    {
        public Task MutateIncoming(MutateIncomingTransportMessageContext context)
        {
            returnInfo = null;

            var headers = context.Headers;
            if (!headers.ContainsKey(Headers.HttpFrom) &&
                !headers.ContainsKey(Headers.OriginatingSite))
            {
                return Task.FromResult(0);
            }

            string originatingSite;
            headers.TryGetValue(Headers.OriginatingSite, out originatingSite);
            string httpFrom;
            headers.TryGetValue(Headers.HttpFrom, out httpFrom);
            returnInfo = new HttpReturnInfo
            {
                //we preserve the httpFrom to be backwards compatible with NServiceBus 2.X 
                HttpFrom = httpFrom,
                OriginatingSite = originatingSite,
                ReplyToAddress = headers[Headers.ReplyToAddress],
                LegacyMode = headers.IsLegacyGatewayMessage()
            };

            return Task.FromResult(0);
        }

        public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
        {
            if (returnInfo == null)
            {
                return Task.FromResult(0);
            }

            var headers = context.OutgoingHeaders;
            if (string.IsNullOrEmpty(headers[Headers.CorrelationId]))
            {
                return Task.FromResult(0);
            }

            if (headers.ContainsKey(Headers.HttpTo) ||
                headers.ContainsKey(Headers.DestinationSites))
            {
                return Task.FromResult(0);
            }

            headers[Headers.HttpTo] = returnInfo.HttpFrom;
            headers[Headers.OriginatingSite] = returnInfo.OriginatingSite;

            if (!headers.ContainsKey(Headers.RouteTo))
            {
                headers[Headers.RouteTo] = returnInfo.ReplyToAddress;
            }

            // send to be backwards compatible with Gateway 3.X
            headers[GatewayHeaders.LegacyMode] = returnInfo.LegacyMode.ToString();

            return Task.FromResult(0);
        }

        public void Customize(BusConfiguration builder)
        {
            builder.RegisterComponents(c => c.ConfigureComponent<GatewayHeaderManager>(
                DependencyLifecycle.InstancePerCall));
        }

        // TODO: Evil!!! 
        // TODO: We need a way to float data between incoming and outgoing mutators
        [ThreadStatic] static HttpReturnInfo returnInfo;

        class HttpReturnInfo
        {
            public string HttpFrom { get; set; }
            public string OriginatingSite { get; set; }
            public string ReplyToAddress { get; set; }
            public bool LegacyMode { get; set; }
        }
    }
}