namespace NServiceBus.Gateway.Channels
{
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Implement to add a new channel sender.
    /// </summary>
    public interface IChannelSender
    {
        /// <summary>
        /// Sends the given data to the remote address.
        /// </summary>
        /// <param name="remoteAddress">The destination address.</param>
        /// <param name="headers">Extra headers.</param>
        /// <param name="data">The data to be sent.</param>
        void Send(string remoteAddress, IDictionary<string, string> headers, Stream data);
    }
}