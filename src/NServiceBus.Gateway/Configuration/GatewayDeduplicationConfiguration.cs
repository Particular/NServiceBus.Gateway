namespace NServiceBus.Gateway
{
    using ObjectBuilder;

    /// <summary>
    /// Configures the deduplication storage.
    /// </summary>
    public abstract class GatewayDeduplicationConfiguration
    {
        /// <summary>
        /// Creates an instance of the deduplication storage.
        /// </summary>
        public abstract IGatewayDeduplicationStorage CreateStorage(IBuilder builder);
    }
}