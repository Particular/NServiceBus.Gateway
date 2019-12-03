namespace NServiceBus.Gateway
{
    using ObjectBuilder;

    /// <summary>
    /// Configures the deduplication storage.
    /// </summary>
    public interface IGatewayDeduplicationConfiguration
    {
        /// <summary>
        /// Creates an instance of the deduplication storage.
        /// </summary>
        /// <param name="builder"></param>
        IGatewayDeduplicationStorage CreateStorage(IBuilder builder);
    }
}