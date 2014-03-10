namespace NServiceBus.Gateway.V2.Sending
{
    using Routing;

    public interface IForwardMessagesToSites
    {
        void Forward(TransportMessage message, Site targetSite);
    }
}