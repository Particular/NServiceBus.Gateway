namespace NServiceBus.Gateway.V2.Routing.Endpoints
{
    public class DefaultEndpointRouter : IRouteMessagesToEndpoints
    {
        public Address MainInputAddress { get; set; }

        public Address GetDestinationFor(TransportMessage messageToSend)
        {
            return MainInputAddress;
        }
    }
}