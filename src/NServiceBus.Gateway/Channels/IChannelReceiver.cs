namespace NServiceBus.Gateway
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Implement to create a channel receiver.
    /// </summary>
    public interface IChannelReceiver
    {
        /// <summary>
        /// Called to start the receiving channel.
        /// </summary>
        /// <param name="address">The address to listen on.</param>
        /// <param name="maxConcurrency">The maximum number of messages that should be processed at any given time.</param>
        /// <param name="dataReceivedOnChannel">The handler fired when data is received.</param>
        void Start(string address, int maxConcurrency, Func<DataReceivedOnChannelArgs, Task> dataReceivedOnChannel);
        
        /// <summary>
        /// Called to shut down the receive channel.
        /// </summary>
        Task Stop();
    }
}