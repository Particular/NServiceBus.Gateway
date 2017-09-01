namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Configuration.AdvancedExtensibility;
    using Gateway;
    using Gateway.Channels;
    using Gateway.Routing;
    using Settings;
    using Transport;
#if NET452
    using System.Configuration;
    using System.Linq;
    using Config;
#endif
    /// <summary>
    /// Placeholder for the various settings and extension points related to gateway.
    /// </summary>
    public class GatewaySettings
    {
        internal GatewaySettings(EndpointConfiguration config)
        {
            settings = config.GetSettings();
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
            Guard.AgainstNull(nameof(senderFactory), senderFactory);
            Guard.AgainstNull(nameof(receiverFactory), receiverFactory);

            settings.Set("GatewayChannelSenderFactory", senderFactory);
            settings.Set("GatewayChannelReceiverFactory", receiverFactory);
        }


        /// <summary>
        /// Set the number of retries and time increase between them for messages failing to be sent through the gateway.
        /// </summary>
        /// <param name="numberOfRetries">The total number of retries to do. 0 means no retry.</param>
        /// <param name="timeIncrease">The time to wait between each retry.</param>
        public void Retries(int numberOfRetries, TimeSpan timeIncrease)
        {
            Guard.AgainstNegative(nameof(numberOfRetries), numberOfRetries);
            Guard.AgainstNegative(nameof(timeIncrease), timeIncrease);

            SetDefaultRetryPolicySettings(numberOfRetries, timeIncrease);
        }

        /// <summary>
        /// Set a retry policy that returns a TimeSpan to delay between attempts based on the number of retries attempted. Return
        /// <see cref="TimeSpan.Zero" /> to abort retries.
        /// </summary>
        /// <param name="customRetryPolicy">The custom retry policy to use.</param>
        public void CustomRetryPolicy(Func<IncomingMessage, Exception, int, TimeSpan> customRetryPolicy)
        {
            Guard.AgainstNull(nameof(customRetryPolicy), customRetryPolicy);

            settings.Set("Gateway.Retries.RetryPolicy", customRetryPolicy);
        }

        /// <summary>
        /// Failed messages will not be retried and will be sent directly to the configured error queue.
        /// </summary>
        public void DisableRetries()
        {
            SetDefaultRetryPolicySettings(0, TimeSpan.MinValue);
        }

        /// <summary>
        /// The site key to use, this goes hand in hand with Bus.SendToSites(key, message).
        /// </summary>
        /// <param name="siteKey"></param>
        /// <param name="address">The channel address.</param>
        /// <param name="type">The channel type. Default is `http`.</param>
        /// <param name="legacyMode">Pass `true` to set the forwarding mode for this site to legacy mode.</param>
        public void AddSite(string siteKey, string address, string type = "http", bool legacyMode = false)
        {
            Guard.AgainstNullAndEmpty(nameof(siteKey), siteKey);
            Guard.AgainstNullAndEmpty(nameof(address), address);
            Guard.AgainstNullAndEmpty(nameof(type), type);

            var site = new Site
            {
                Channel = new Channel
                {
                    Address = address,
                    Type = type
                },
                Key = siteKey,
                LegacyMode = legacyMode
            };

            if (settings.TryGet(out List<Site> sites))
            {
                sites.Add(site);
            }

            settings.Set<List<Site>>(new List<Site>
            {
                site
            });
        }

        /// <summary>
        /// Adds a receive channel that the gateway should listen to.
        /// </summary>
        /// <param name="address">The channel address.</param>
        /// <param name="maxConcurrency">Maximum number of receive connections. Default is `1`.</param>
        /// <param name="type">The channel type. Default is `http`.</param>
        /// <param name="isDefault">True if this should be the default channel for send operations. Default is `false`.</param>
        public void AddReceiveChannel(string address, string type = "http", int maxConcurrency = 1, bool isDefault = false)
        {
            Guard.AgainstNullAndEmpty(nameof(address), address);
            Guard.AgainstNullAndEmpty(nameof(type), type);
            Guard.AgainstNegativeAndZero(nameof(maxConcurrency), 1);

            var channel = new ReceiveChannel
            {
                Address = address,
                MaxConcurrency = maxConcurrency,
                Type = type,
                Default = isDefault
            };

            if (settings.TryGet(out List<ReceiveChannel> channels))
            {
                channels.Add(channel);
            }

            settings.Set<List<ReceiveChannel>>(new List<ReceiveChannel>
            {
                channel
            });
        }

        internal static TimeSpan? GetTransactionTimeout(ReadOnlySettings settings)
        {
            if (settings.TryGet("Gateway.TransactionTimeout", out TimeSpan? timeout))
            {
                return timeout;
            }
#if NETSTANDARD2_0
            return null;
#endif
#if NET452
            var configSection = GetConfigSection(settings);


            return configSection?.TransactionTimeout;
#endif
        }

        internal static List<Site> GetConfiguredSites(ReadOnlySettings settings)
        {
            if (settings.TryGet(out List<Site> sites))
            {
                return sites;
            }
#if NETSTANDARD2_0
            return new List<Site>();
#endif
#if NET452
            var configSection = GetConfigSection(settings);

            if (configSection == null)
            {
                return new List<Site>();
            }

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
#endif
        }

        internal static List<ReceiveChannel> GetConfiguredChannels(ReadOnlySettings settings)
        {
            if (settings.TryGet(out List<ReceiveChannel> channels))
            {
                return channels;
            }
#if NETSTANDARD2_0
            return new List<ReceiveChannel>();
#endif
#if NET452

            var configSection = GetConfigSection(settings);

            if (configSection == null)
            {
                return new List<ReceiveChannel>();
            }

            return (from ChannelConfig channel in configSection.Channels
                    select new ReceiveChannel
                    {
                        Address = channel.Address,
                        Type = channel.ChannelType,
                        MaxConcurrency = channel.MaxConcurrency,
                        Default = channel.Default
                    }).ToList();
#endif
        }
#if NET452
        static GatewayConfig GetConfigSection(ReadOnlySettings settings)
        {

            if (settings.TryGet(out GatewayConfig config))
            {
                return config;
            }

            return ConfigurationManager.GetSection(typeof(GatewayConfig).Name) as GatewayConfig;
        }
#endif
        void SetDefaultRetryPolicySettings(int numberOfRetries, TimeSpan timeIncrease)
        {
            settings.Set("Gateway.Retries.RetryPolicy", DefaultRetryPolicy.Build(numberOfRetries, timeIncrease));
        }

        SettingsHolder settings;
    }
}