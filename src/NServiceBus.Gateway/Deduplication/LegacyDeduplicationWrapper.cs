namespace NServiceBus.Gateway
{
    using System;
    using System.Threading.Tasks;
    using Deduplication;
    using Extensibility;

    class LegacyDeduplicationWrapper : IGatewayDeduplicationStorage
    {
        public LegacyDeduplicationWrapper(IDeduplicateMessages legacyPersister)
        {
            this.legacyPersister = legacyPersister;
        }

        public bool SupportsDistributedTransactions { get; } = true;

        public async Task<bool> IsDuplicate(string messageId, ContextBag context)
        {
            var isNewMessage = await legacyPersister.DeduplicateMessage(messageId, DateTime.UtcNow, context)
                .ConfigureAwait(false);
            return !isNewMessage;
        }

        public Task MarkAsDispatched(string messageId, ContextBag context)
        {
            return Task.FromResult(0);
        }

        IDeduplicateMessages legacyPersister;
    }
}