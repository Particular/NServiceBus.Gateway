namespace NServiceBus.Gateway.V2.Channels
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class DataReceivedOnChannelArgs : EventArgs
    {
        public IDictionary<string, string> Headers { get; set; }

        public Stream Data { get; set; }
    }
}