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
        /// <param name="storageConfiguration">the storage configuration for the gateway's deduplication mechanism</param>
        public static GatewaySettings Gateway(this EndpointConfiguration config, GatewayDeduplicationConfiguration storageConfiguration)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(storageConfiguration);

            config.EnableFeature<Features.Gateway>();

            config.GetSettings().Set(storageConfiguration);
            config.GetSettings().SetDefault("Gateway.Retries.RetryPolicy", DefaultRetryPolicy.BuildWithDefaults());

            return new GatewaySettings(config);
        }
    }
}