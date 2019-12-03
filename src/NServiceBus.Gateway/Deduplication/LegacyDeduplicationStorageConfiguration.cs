namespace NServiceBus.Gateway
{
    using Deduplication;
    using ObjectBuilder;

    class LegacyDeduplicationStorageConfiguration : IGatewayDeduplicationConfiguration
    {
        public IGatewayDeduplicationStorage CreateStorage(IBuilder builder)
        {
            return new LegacyDeduplicationWrapper(builder.Build<IDeduplicateMessages>());
        }
    }
}