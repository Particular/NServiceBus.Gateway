namespace NServiceBus.Gateway.Channels
{
    using System;

    /// <summary>
    /// Implement to create a channel receiver.
    /// </summary>
    public interface IChannelReceiver : IDisposable
    {
        /// <summary>
        /// Fires when data is received.
        /// </summary>
        event EventHandler<DataReceivedOnChannelArgs> DataReceived;

        /// <summary>
        /// Called to start the receiving threads.
        /// </summary>
        /// <param name="address">The address to listen on.</param>
        /// <param name="numberOfWorkerThreads">The maximum number of worker threads to use for receiving data concurrently.</param>
        void Start(string address, int numberOfWorkerThreads);
    }
}