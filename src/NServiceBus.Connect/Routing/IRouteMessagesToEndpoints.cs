namespace NServiceBus.Connect.Routing
{
    public interface IRouteMessagesToEndpoints
    {
        Address GetDestinationFor(TransportMessage messageToSend);
    }
}