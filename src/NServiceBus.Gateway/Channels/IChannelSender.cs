namespace NServiceBus.Gateway
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

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
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
        Task Send(string remoteAddress, IDictionary<string, string> headers, Stream data, CancellationToken cancellationToken = default);
    }
}