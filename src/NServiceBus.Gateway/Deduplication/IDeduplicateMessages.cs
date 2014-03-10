namespace NServiceBus.Gateway.V2.Deduplication
{
    using System;

    public interface IDeduplicateMessages
    {
        bool DeduplicateMessage(string clientId, DateTime timeReceived);
    }
}