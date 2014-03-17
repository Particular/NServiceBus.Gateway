namespace NServiceBus.Connect.Sending
{
    using Routing;

    public interface IForwardMessagesToSites
    {
        void Forward(TransportMessage message, Site targetSite);
    }
}