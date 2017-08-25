namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using Config;
    using Configuration.AdvancedExtensibility;
    using Gateway;
    using Gateway.Channels;
    using Gateway.Routing;
    using Settings;
    using Transport;

    /// <summary>
    /// Placeholder for the various settings and extension points related to gateway.
    /// </summary>
    public class GatewaySettings
    {
        internal GatewaySettings(EndpointConfiguration config)
        {
            this.config = config;
        }

      

        /// <summary>
        /// Register custom factories for creating channel receivers and channel senders. This allows for overriding the default
        /// Http implementation.
        /// </summary>
        /// <param name="senderFactory">The sender factory to use. The factory takes a string with the channel type as parameter.</param>
        /// <param name="receiverFactory">
        /// The receiver factory to use. The factory takes a string with the channel type as
        /// parameter.
        /// </param>
        public void ChannelFactories(Func<string, IChannelSender> senderFactory, Func<string, IChannelReceiver> receiverFactory)
        {
            if (senderFactory == null)
                throw new ArgumentNullException(nameof(senderFactory));

            if (receiverFactory == null)
                throw new ArgumentNullException(nameof(receiverFactory));

            config.GetSettings().Set("GatewayChannelSenderFactory", senderFactory);
            config.GetSettings().Set("GatewayChannelReceiverFactory", receiverFactory);
        }


        /// <summary>
        /// Set the number of retries and time increase between them for messages failing to be sent through the gateway.
        /// </summary>
        /// <param name="numberOfRetries">The total number of retries to do. 0 means no retry.</param>
        /// <param name="timeIncrease">The time to wait between each retry.</param>
        public void Retries(int numberOfRetries, TimeSpan timeIncrease)
        {
            if (numberOfRetries < 0)
                throw new ArgumentOutOfRangeException(nameof(numberOfRetries), numberOfRetries, $"{nameof(numberOfRetries)} must be non-negative");

            if (timeIncrease < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeIncrease), timeIncrease, $"{nameof(timeIncrease)} must be non-negative");

            SetDefaultRetryPolicySettings(numberOfRetries, timeIncrease);
        }

        /// <summary>
        /// Set a retry policy that returns a TimeSpan to delay between attempts based on the number of retries attempted. Return
        /// <see cref="TimeSpan.Zero" /> to abort retries.
        /// </summary>
        /// <param name="customRetryPolicy">The custom retry policy to use.</param>
        public void CustomRetryPolicy(Func<IncomingMessage, Exception, int, TimeSpan> customRetryPolicy)
        {
            config.GetSettings().Set("Gateway.Retries.RetryPolicy", customRetryPolicy);
        }

        /// <summary>
        /// Failed messages will not be retried and will be sent directly to the configured error queue.
        /// </summary>
        public void DisableRetries()
        {
            SetDefaultRetryPolicySettings(0, TimeSpan.MinValue);
        }

        /// <summary>
        /// The sites that this Gateway should communicate with.
        /// </summary>
        /// <param name="sites"></param>
        public void Sites(IEnumerable<Site> sites)
        {
            config.GetSettings().Set<List<Site>>(sites);
        }

        internal static TimeSpan? GetTransactionTimeout(ReadOnlySettings settings)
        {
            if (settings.TryGet("Gateway.TransactionTimeout", out TimeSpan? timeout))
                return timeout;

            var configSection = GetConfigSection(settings);


            return configSection?.TransactionTimeout;
        }

        internal static List<Site> GetConfiguredSites(ReadOnlySettings settings)
        {
            if (settings.TryGet(out List<Site> sites))
                return sites;

            var configSection = GetConfigSection(settings);

            if (configSection == null)
                return new List<Site>();

            return configSection.Sites.Cast<SiteConfig>().Select(site => new Site
            {
                Key = site.Key,
                Channel = new Channel
                {
                    Type = site.ChannelType,
                    Address = site.Address
                },
                LegacyMode = site.LegacyMode
            }).ToList();
        }

        internal static List<ReceiveChannel> GetConfiguredChannels(ReadOnlySettings settings)
        {
            if (settings.TryGet(out List<ReceiveChannel> channels))
                return channels;

            var configSection = GetConfigSection(settings);

            if (configSection == null)
                return new List<ReceiveChannel>();

            return (from ChannelConfig channel in configSection.Channels
                select new ReceiveChannel
                {
                    Address = channel.Address,
                    Type = channel.ChannelType,
                    MaxConcurrency = channel.MaxConcurrency,
                    Default = channel.Default
                }).ToList();
        }

        static GatewayConfig GetConfigSection(ReadOnlySettings settings)
        {
            if (settings.TryGet(out GatewayConfig config))
                return config;

            return ConfigurationManager.GetSection(typeof(GatewayConfig).Name) as GatewayConfig;
        }

        void SetDefaultRetryPolicySettings(int numberOfRetries, TimeSpan timeIncrease)
        {
            config.GetSettings().Set("Gateway.Retries.RetryPolicy", DefaultRetryPolicy.Build(numberOfRetries, timeIncrease));
        }

        EndpointConfiguration config;
    }
}