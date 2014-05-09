namespace NServiceBus.Connect.Routing.Endpoints
{
    internal class DefaultEndpointRouter : IRouteMessagesToEndpoints
    {
        public Address MainInputAddress { get; set; }

        public Address GetDestinationFor(TransportMessage messageToSend)
        {
            return MainInputAddress;
        }
    }
}