namespace NServiceBus.Gateway
{
    using System;
    using System.Collections.Generic;
    using Deduplication;
    using ObjectBuilder;
    using Settings;

    class LegacyDeduplicationStorageConfiguration : GatewayDeduplicationConfiguration
    {
        public override void Setup(ReadOnlySettings settings)
        {
            if (settings.TryGet("ResultingSupportedStorages", out List<Type> supportedStorages))
            {
                if (!supportedStorages.Contains(typeof(StorageType.GatewayDeduplication)))
                {
                    throw new Exception("The selected persistence doesn't have support for gateway deduplication storage. Please configure one that supports gateway deduplication storage.");
                }
            }
            else
            {
                throw new Exception("No persistence configured, please configure one that supports gateway deduplication storage or provide a deduplication storage via the 'endpointConfiguration.Gateway' API.");
            }

            base.Setup(settings);
        }

        public override IGatewayDeduplicationStorage CreateStorage(IBuilder builder)
        {
            return new LegacyDeduplicationWrapper(builder.Build<IDeduplicateMessages>());
        }
    }
}