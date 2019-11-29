namespace NServiceBus.Gateway.Deduplication
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;

    class LegacyDeduplicationWrapper : IGatewayDeduplicationStorage
    {
        IDeduplicateMessages legacyPersister;

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
    }
}