namespace NServiceBus.Settings
{
    using System;
    using global::NServiceBus.Configuration.AdvanceExtensibility;
    using global::NServiceBus.Gateway.Routing;
    using global::NServiceBus.Gateway.Sending;

    /// <summary>
    /// Placeholder for the various settings and extension points related to gateway.
    /// </summary>
    public class GatewaySettings
    {
        BusConfiguration config;

        internal GatewaySettings(BusConfiguration config)
        {
            this.config = config;
        }

        /// <summary>
        /// Register a custom implemention of a message forwarder to be used by the gateway.
        /// </summary>
        /// <param name="forwarder">The message forwarder to use.</param>
        public void MessageForwarder(IForwardMessagesToSites forwarder)
        {
            if (forwarder == null)
            {
                throw new ArgumentNullException(nameof(forwarder));
            }

            config.GetSettings().Set("GatewayMessageForwarder", forwarder);
        }

        /// <summary>
        /// Register a custom implemention of a message to sites router to be used by the gateway.
        /// </summary>
        /// <param name="router">The message router to use.</param>
        public void SiteRouter(IRouteMessagesToSites router)
        {
            if (router == null)
            {
                throw new ArgumentNullException(nameof(router));
            }

            config.GetSettings().Set("GatewaySiteRouter", router);
        }

        /// <summary>
        /// Register a custom implemention of a message to endpoint router to be used by the gateway.
        /// </summary>
        /// <param name="router">The message router to use.</param>
        public void EndpointRouter(IRouteMessagesToEndpoints router)
        {
            if (router == null)
            {
                throw new ArgumentNullException(nameof(router));
            }

            config.GetSettings().Set("GatewayEndpointRouter", router);
        }
    }
}