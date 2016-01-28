namespace NServiceBus.Gateway.Routing
{
    using System.Collections.Generic;

    /// <summary>
    /// Implement this interface to override the default implementation of how messages are routed. 
    /// </summary>
    public interface IRouteMessagesToSites
    {
        /// <summary>
        /// Retrieves list of <see cref="Site">Sites</see> to send messages to.
        /// </summary>
        /// <param name="headers">The headers of the current message.</param>
        /// <returns>The list of <see cref="Site">Sites</see>.</returns>
        IEnumerable<Site> GetDestinationSitesFor(Dictionary<string, string> headers);
    }
}