#pragma warning disable 1591

namespace NServiceBus.Gateway
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    [ObsoleteEx(
        TreatAsErrorFromVersion = "4.0.0",
        RemoveInVersion = "5.0.0",
        ReplacementTypeOrMember = nameof(DataReceivedOnChannelEventArgs))]
#pragma warning disable CA1710 // Identifiers should have correct suffix
    public class DataReceivedOnChannelArgs : EventArgs
#pragma warning restore CA1710 // Identifiers should have correct suffix
    {
        public IDictionary<string, string> Headers { get; set; }

        public Stream Data { get; set; }
    }
}

#pragma warning restore 1591