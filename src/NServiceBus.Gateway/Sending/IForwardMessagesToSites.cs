namespace NServiceBus.Gateway.Sending
{
    using Routing;

    /// <summary>
    /// Implement this interface to override the default message forwarding between sites.
    /// </summary>
    public interface IForwardMessagesToSites
    {
        /// <summary>
        /// Forwards the given message to the given site destination.
        /// </summary>
        /// <param name="message">The message to be forwarded.</param>
        /// <param name="targetSite">The destination site.</param>
        void Forward(TransportMessage message, Site targetSite);
    }
}