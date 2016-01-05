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
        /// <param name="numberOfWorkerThreads">The maximum number of worker threads to use for receiving data concurrently.</param>
        /// <param name="dataReceivedOnChannel">The handler fired when data is received.</param>
        void Start(string address, int numberOfWorkerThreads, Func<DataReceivedOnChannelArgs, Task> dataReceivedOnChannel);
    }
}