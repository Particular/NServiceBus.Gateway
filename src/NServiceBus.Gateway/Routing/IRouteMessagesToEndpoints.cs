namespace NServiceBus.Gateway.Routing
{
    /// <summary>
    /// Implement this interface to override the default implementation of how messages are routed to the endpoint when received. 
    /// </summary>
    public interface IRouteMessagesToEndpoints
    {
        /// <summary>
        /// Retrieves the <see cref="Address"/> to forward the message to.
        /// </summary>
        /// <param name="messageToSend">The message to send.</param>
        /// <returns>The destination address.</returns>
// ReSharper disable UnusedParameter.Global
        Address GetDestinationFor(TransportMessage messageToSend);
// ReSharper restore UnusedParameter.Global
    }
}