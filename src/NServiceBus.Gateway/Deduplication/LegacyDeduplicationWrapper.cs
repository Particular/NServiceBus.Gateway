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

        public async Task<IDuplicationCheckSession> CheckForDuplicate(string messageId, ContextBag context)
        {
            var isNewMessage = await legacyPersister.DeduplicateMessage(messageId, DateTime.UtcNow, context)
                .ConfigureAwait(false);
            return new LegacyDuplicationCheckSession(!isNewMessage);
        }

        IDeduplicateMessages legacyPersister;

        class LegacyDuplicationCheckSession : IDuplicationCheckSession
        {
            public LegacyDuplicationCheckSession(bool isDuplicate)
            {
                IsDuplicate = isDuplicate;
            }

            public void Dispose()
            {
            }

            public Task MarkAsDispatched()
            {
                return Task.FromResult(0);
            }

            public bool IsDuplicate { get; }
        }
    }
}