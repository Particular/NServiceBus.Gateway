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

        public async Task<IDuplicationCheckSession> IsDuplicate(string messageId, ContextBag context)
        {
            var isNewMessage = await legacyPersister.DeduplicateMessage(messageId, DateTime.UtcNow, context)
                .ConfigureAwait(false);
            return new DummyLegacySession(!isNewMessage);
        }

        IDeduplicateMessages legacyPersister;

        class DummyLegacySession : IDuplicationCheckSession
        {
            public DummyLegacySession(bool isDuplicate)
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