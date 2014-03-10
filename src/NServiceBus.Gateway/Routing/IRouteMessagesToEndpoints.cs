namespace NServiceBus.Gateway.V2.Routing
{
    public interface IRouteMessagesToEndpoints
    {
        Address GetDestinationFor(TransportMessage messageToSend);
    }
}