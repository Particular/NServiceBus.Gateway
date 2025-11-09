namespace NServiceBus.Gateway
{
    using System;
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using Settings;

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
        protected internal override void EnableFeature(SettingsHolder settings) => settings.EnableFeature<NonDurableDeduplication>();

        class NonDurableDeduplication : Feature
        {
            protected override void Setup(FeatureConfigurationContext context) => context.Services.AddSingleton<IGatewayDeduplicationStorage>(_ => new NonDurableDeduplicationStorage(context.Settings.Get<NonDurableDeduplicationConfiguration>().CacheSize));
        }
    }
}