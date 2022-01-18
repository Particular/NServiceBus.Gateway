#pragma warning disable 1591

namespace NServiceBus.Gateway
{
    using System;

    [ObsoleteEx(
        TreatAsErrorFromVersion = "4.0.0",
        RemoveInVersion = "5.0.0",
        ReplacementTypeOrMember = nameof(DataReceivedOnChannelEventArgs))]
#pragma warning disable CA1710 // Identifiers should have correct suffix
    public class DataReceivedOnChannelArgs : EventArgs
#pragma warning restore CA1710 // Identifiers should have correct suffix
    {
    }

    [ObsoleteEx(
        TreatAsErrorFromVersion = "4.0.0",
        RemoveInVersion = "5.0.0",
        ReplacementTypeOrMember = nameof(NonDurableDeduplicationConfiguration))]
    public class InMemoryDeduplicationConfiguration
    {
    }
}

#pragma warning restore 1591