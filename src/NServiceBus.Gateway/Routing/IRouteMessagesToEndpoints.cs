namespace NServiceBus.Gateway.Routing
{
    using System.Collections.Generic;

    /// <summary>
    /// Implement this interface to override the default implementation of how messages are routed to the endpoint when received. 
    /// </summary>
    public interface IRouteMessagesToEndpoints
    {
        /// <summary>
        /// Retrieves the address to forward the message to.
        /// </summary>
        /// <param name="headers">The headers of the message to send.</param>
        /// <returns>The destination address.</returns>
        // ReSharper disable once UnusedParameter.Global
        string GetDestinationFor(Dictionary<string, string> headers);
    }
}