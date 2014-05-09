namespace NServiceBus.Connect
{
    using System;
    using Gateway.Deduplication;
    using NServiceBus.Features;

    public static class ConfigureGateway
    {
        /// <summary>
        /// The Gateway is turned on by default for the Master role. Call DisableGateway method to turn the Gateway off.
        /// </summary>
        public static Configure DisableGateway(this Configure config)
        {
            Feature.Disable<Features.Gateway>();
            return config;
        }

        /// <summary>
        /// Configuring to run the Gateway. By default Gateway will use RavenPersistence (see GatewayDefaults class).
        /// </summary>
        public static Configure RunGateway(this Configure config)
        {
            Feature.Enable<Features.Gateway>();

            return config;
        }


        public static Configure RunGateway(this Configure config, Type persistence)
        {
            config.Configurer.ConfigureComponent(persistence, DependencyLifecycle.SingleInstance);
            Feature.Enable<Features.Gateway>();
            return config;
        }

        /// <summary>
        /// Use in-memory message deduplication for the gateway.
        /// </summary>
        public static Configure UseInMemoryGatewayDeduplication(this Configure config)
        {
            config.Configurer.ConfigureComponent<InMemoryDeduplication>(DependencyLifecycle.SingleInstance);
            return config;
        }

    }
}