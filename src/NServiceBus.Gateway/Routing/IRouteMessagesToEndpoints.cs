namespace NServiceBus.Gateway.Routing
{
    interface IRouteMessagesToEndpoints
    {
        Address GetDestinationFor(TransportMessage messageToSend);
    }
}