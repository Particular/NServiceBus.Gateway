namespace NServiceBus
{
    using System;
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
        [ObsoleteEx(Message = "Gateway with no configuration is not supported.", ReplacementTypeOrMember = "Gateway(GatewayDeduplicationConfiguration)", TreatAsErrorFromVersion = "4.0.0", RemoveInVersion = "5.0.0")]
        public static GatewaySettings Gateway(this EndpointConfiguration config)
        {
            throw new NotImplementedException();
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

            config.GetSettings().Set(storageConfiguration);

            return new GatewaySettings(config);
        }
    }
}