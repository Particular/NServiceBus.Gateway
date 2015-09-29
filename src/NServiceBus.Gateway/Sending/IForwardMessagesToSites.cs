namespace NServiceBus.Gateway.Sending
{
    using System.Collections.Generic;
    using Routing;

    /// <summary>
    /// Implement this interface to override the default message forwarding between sites.
    /// </summary>
    public interface IForwardMessagesToSites
    {
        /// <summary>
        /// Forwards the given message to the given site destination.
        /// </summary>
        /// <param name="body">The message body to be forwarded.</param>
        /// <param name="headers">The message headers to be forwarded.</param>
        /// <param name="targetSite">The destination site.</param>
        void Forward(byte[] body, Dictionary<string, string> headers, Site targetSite);
    }
}