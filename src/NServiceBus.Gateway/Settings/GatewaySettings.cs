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
        /// <param name="siteKey">The site key.</param>
        /// <param name="address">The channel address.</param>
        /// <param name="type">The channel type. Default is `http`.</param>
        /// <param name="legacyMode">Pass `true` to set the forwarding mode for this site to legacy mode.</param>
        public void AddSite(string siteKey, string address, string type = "http", bool legacyMode = false)
        {
            Guard.AgainstNullAndEmpty(nameof(siteKey), siteKey);
            Guard.AgainstNullAndEmpty(nameof(address), address);
            Guard.AgainstNullAndEmpty(nameof(type), type);

            var sites = settings.GetOrCreate<List<Site>>();

            sites.Add(new Site
            {
                Channel = new Channel
                {
                    Address = address,
                    Type = type
                },
                Key = siteKey,
                LegacyMode = legacyMode
            });
        }

        /// <summary>Adds a receive channel that the gateway should listen to.</summary>
        /// <param name="address">The channel address.</param>
        /// <param name="type">The channel type. Default is `http`.</param>
        /// <param name="maxConcurrency">Maximum number of receive connections. Default is `1`.</param>
        /// <param name="isDefault">True if this should be the default channel for send operations. Default is `false`.</param>
        /// <param name="proxyAddress">The proxy Address to use when gateway is behind proxy, is used in reply on headers.</param>
        public void AddReceiveChannel(string address, string type = "http", int maxConcurrency = 1, bool isDefault = false, string proxyAddress = null)
        {
            Guard.AgainstNullAndEmpty(nameof(address), address);
            Guard.AgainstNullAndEmpty(nameof(type), type);
            Guard.AgainstNegativeAndZero(nameof(maxConcurrency), maxConcurrency);

            var channels = settings.GetOrCreate<List<ReceiveChannel>>();

            channels.Add(new ReceiveChannel
            {
                Address = address,
                MaxConcurrency = maxConcurrency,
                Type = type,
                Default = isDefault,
                ProxyAddress = proxyAddress
            });
        }

        /// <summary>
        /// Configures the transaction timeout to use when transmitting messages to remote sites. By default, the transaction timeout of the underlying transport is used.
        /// </summary>
        /// <param name="timeout">The new timeout value.</param>
        public void TransactionTimeout(TimeSpan timeout)
        {
            Guard.AgainstNegativeAndZero(nameof(timeout), timeout);

            settings.Set("Gateway.TransactionTimeout", timeout);
        }

        internal static TimeSpan? GetTransactionTimeout(ReadOnlySettings settings)
        {
            return settings.TryGet("Gateway.TransactionTimeout", out TimeSpan? timeout) ? timeout : null;
        }

        internal static List<Site> GetConfiguredSites(ReadOnlySettings settings)
        {
            return settings.TryGet(out List<Site> sites) ? sites : new List<Site>();
        }

        internal static List<ReceiveChannel> GetConfiguredChannels(ReadOnlySettings settings)
        {
            return settings.TryGet(out List<ReceiveChannel> channels) ? channels : new List<ReceiveChannel>();
        }

        void SetDefaultRetryPolicySettings(int numberOfRetries, TimeSpan timeIncrease)
        {
            settings.Set("Gateway.Retries.RetryPolicy", DefaultRetryPolicy.Build(numberOfRetries, timeIncrease));
        }

        SettingsHolder settings;
    }
}
