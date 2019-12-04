namespace NServiceBus.Gateway
{
    using Deduplication;
    using ObjectBuilder;

    class LegacyDeduplicationStorageConfiguration : GatewayDeduplicationConfiguration
    {
        public override IGatewayDeduplicationStorage CreateStorage(IBuilder builder)
        {
            return new LegacyDeduplicationWrapper(builder.Build<IDeduplicateMessages>());
        }
    }
}