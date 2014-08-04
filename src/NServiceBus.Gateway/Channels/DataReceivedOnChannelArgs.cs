namespace NServiceBus.Gateway.Channels
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Contains the data and headers received.
    /// </summary>
    public class DataReceivedOnChannelArgs : EventArgs
    {
        /// <summary>
        /// The headers received.
        /// </summary>
        public IDictionary<string, string> Headers { get; set; }

        /// <summary>
        /// The data received by the channel.
        /// </summary>
        public Stream Data { get; set; }
    }
}