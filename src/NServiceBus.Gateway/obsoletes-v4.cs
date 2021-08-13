#pragma warning disable 1591

namespace NServiceBus.Gateway
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    [ObsoleteEx(
        TreatAsErrorFromVersion = "8.0.0",
        RemoveInVersion = "9.0.0",
        ReplacementTypeOrMember = nameof(DataReceivedOnChannelEventArgs))]
    public class DataReceivedOnChannelArgs : EventArgs
    {
        public IDictionary<string, string> Headers { get; set; }

        public Stream Data { get; set; }
    }
}

#pragma warning restore 1591