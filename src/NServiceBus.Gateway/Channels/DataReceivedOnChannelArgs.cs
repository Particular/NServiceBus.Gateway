namespace NServiceBus.Gateway
{
    using System;
    using System.Collections.Generic;
    using System.IO;


    /// <summary>
    /// Contains the data and headers received.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "Part of public API, fixed in 4.0")]
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