namespace NServiceBus.Gateway
{
    using System;

    /// <summary>
    /// Configuration class for the in-memory gateway deduplication storage.
    /// </summary>
    public class NonDurableDeduplicationConfiguration : GatewayDeduplicationConfiguration
    {
        int cacheSize = 10000;

        /// <summary>
        /// Configures the size of the LRU cache. This values defines the maximum amount of messages which can be tracked for duplicates.
        /// </summary>
        public int CacheSize
        {
            get => cacheSize;
            set
            {
                Guard.AgainstNegativeAndZero(nameof(value), value);
                cacheSize = value;
            }
        }

        /// <inheritdoc />
        public override IGatewayDeduplicationStorage CreateStorage(IServiceProvider builder)
        {
            return new NonDurableDeduplicationStorage(CacheSize);
        }
    }
}