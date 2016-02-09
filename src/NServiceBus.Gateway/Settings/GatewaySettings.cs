namespace NServiceBus.Settings
{
    using System;
    using Configuration.AdvanceExtensibility;
    using global::NServiceBus.Gateway;

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
        /// Register custom factories for creating channel receivers and channel senders. This allows for overriding the default Http implementation.
        /// </summary>
        /// <param name="senderFactory">The sender factory to use. The factory takes a string with the channel type as parameter.</param>
        /// <param name="receiverFactory">The receiver factory to use. The factory takes a string with the channel type as parameter.</param>
        public void ChannelFactories(Func<string, IChannelSender> senderFactory, Func<string, IChannelSender> receiverFactory)
        {
            if (senderFactory == null)
            {
                throw new ArgumentNullException(nameof(senderFactory));
            }

            if (receiverFactory == null)
            {
                throw new ArgumentNullException(nameof(receiverFactory));
            }

            config.GetSettings().Set("GatewayChannelSenderFactory", senderFactory);
            config.GetSettings().Set("GatewayChannelReceiverFactory", receiverFactory);
        }
    }
}