namespace NServiceBus
{
    using System;

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
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            config.EnableFeature<Features.Gateway>();

            return new GatewaySettings(config);
        }
    }
}