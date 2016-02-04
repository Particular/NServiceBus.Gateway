namespace NServiceBus.Gateway.Channels
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Implement to create a channel receiver.
    /// </summary>
    public interface IChannelReceiver : IDisposable
    {
        /// <summary>
        /// Called to start the receiving threads.
        /// </summary>
        /// <param name="address">The address to listen on.</param>
        /// <param name="maxConcurrency">The maximum number of messages that should be processed at any given time.</param>
        /// <param name="dataReceivedOnChannel">The handler fired when data is received.</param>
        void Start(string address, int maxConcurrency, Func<DataReceivedOnChannelArgs, Task> dataReceivedOnChannel);
    }
}