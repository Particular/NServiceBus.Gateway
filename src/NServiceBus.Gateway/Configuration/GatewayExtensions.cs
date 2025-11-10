namespace NServiceBus
{
    using System;
    using Configuration.AdvancedExtensibility;
    using Features;
    using Gateway;

    /// <summary>
    /// Provides a fluent api to allow the configuration of <see cref="GatewaySettings"/>.
    /// </summary>
    public static class GatewayExtensions
    {
        /// <summary>
        /// Allows the user to control how the gateway behaves.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration"/> instance to apply the settings to.</param>
        /// <param name="storageConfiguration">the storage configuration for the gateway's deduplication mechanism</param>
        public static GatewaySettings Gateway<TGatewayDeduplicationConfiguration>(this EndpointConfiguration config, TGatewayDeduplicationConfiguration storageConfiguration)
            where TGatewayDeduplicationConfiguration : GatewayDeduplicationConfiguration
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(storageConfiguration);

            var settings = config.GetSettings();

            settings.EnableFeature<Features.Gateway>();

            storageConfiguration.EnableFeature(settings);

            settings.Set(storageConfiguration);

            return new GatewaySettings(settings);
        }
    }
}