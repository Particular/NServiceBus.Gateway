namespace NServiceBus.Gateway.Routing.Endpoints
{
    class DefaultEndpointRouter : IRouteMessagesToEndpoints
    {
        public string MainInputAddress { get; set; }

        public string GetDestinationFor(TransportMessage messageToSend)
        {
            return MainInputAddress;
        }
    }
}