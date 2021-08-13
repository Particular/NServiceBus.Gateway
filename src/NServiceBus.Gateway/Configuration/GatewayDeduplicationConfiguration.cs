namespace NServiceBus.Gateway
{
    using System;
    using Settings;

    /// <summary>
    /// Configures the deduplication storage.
    /// </summary>
    public abstract class GatewayDeduplicationConfiguration
    {
        /// <summary>
        /// Invoked when the endpoint configuration completed to initialize the storage or verify configuration before the endpoint starts.
        /// </summary>
        public virtual void Setup(IReadOnlySettings settings)
        {
        }

        /// <summary>
        /// Creates an instance of the deduplication storage.
        /// </summary>
        public abstract IGatewayDeduplicationStorage CreateStorage(IServiceProvider builder);
    }
}