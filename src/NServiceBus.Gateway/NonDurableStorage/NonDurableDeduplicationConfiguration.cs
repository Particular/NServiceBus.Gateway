namespace NServiceBus.Gateway
{
    using System;

    /// <summary>
    /// Configuration class for the in-memory gateway deduplication storage.
    /// </summary>
    public class NonDurableDeduplicationConfiguration : GatewayDeduplicationConfiguration
    {
        /// <summary>
        /// Configures the size of the LRU cache. This values defines the maximum amount of messages which can be tracked for duplicates.
        /// </summary>
        public int CacheSize
        {
            get;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
                field = value;
            }
        } = 10000;

        /// <inheritdoc />
        public override IGatewayDeduplicationStorage CreateStorage(IServiceProvider builder) => new NonDurableDeduplicationStorage(CacheSize);
    }
}