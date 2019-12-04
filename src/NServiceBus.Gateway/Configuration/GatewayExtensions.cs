namespace NServiceBus
{
    using Configuration.AdvancedExtensibility;
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
        public static GatewaySettings Gateway(this EndpointConfiguration config)
        {
            return config.Gateway(new LegacyDeduplicationStorageConfiguration());
        }

        /// <summary>
        /// Allows the user to control how the gateway behaves.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration"/> instance to apply the settings to.</param>
        /// <param name="storageConfiguration">the storage configuration for the gateway's deduplication mechanism</param>
        public static GatewaySettings Gateway(this EndpointConfiguration config, GatewayDeduplicationConfiguration storageConfiguration)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNull(nameof(storageConfiguration), storageConfiguration);

            config.EnableFeature<Features.Gateway>();

            config.GetSettings().Set<GatewayDeduplicationConfiguration>(storageConfiguration);

            return new GatewaySettings(config);
        }
    }
}