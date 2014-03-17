namespace NServiceBus.Connect.Deduplication
{
    using System;

    public interface IDeduplicateMessages
    {
        bool DeduplicateMessage(string clientId, DateTime timeReceived);
    }
}