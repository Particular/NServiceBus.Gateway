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
    public class GatewaySettings : ExposeSettings
    {
        internal GatewaySettings(SettingsHolder settings) : base(settings)
        {
            this.settings = settings;

            settings.SetDefault("Gateway.Retries.RetryPolicy", DefaultRetryPolicy.BuildWithDefaults());
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
            ArgumentNullException.ThrowIfNull(senderFactory);
            ArgumentNullException.ThrowIfNull(receiverFactory);

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
            ArgumentOutOfRangeException.ThrowIfNegative(numberOfRetries);
            ArgumentOutOfRangeException.ThrowIfLessThan(timeIncrease, TimeSpan.Zero);

            SetDefaultRetryPolicySettings(numberOfRetries, timeIncrease);
        }

        /// <summary>
        /// Set a retry policy that returns a TimeSpan to delay between attempts based on the number of retries attempted. Return
        /// <see cref="TimeSpan.Zero" /> to abort retries.
        /// </summary>
        /// <param name="customRetryPolicy">The custom retry policy to use.</param>
        public void CustomRetryPolicy(Func<IncomingMessage, Exception, int, TimeSpan> customRetryPolicy)
        {
            ArgumentNullException.ThrowIfNull(customRetryPolicy);

            settings.Set("Gateway.Retries.RetryPolicy", customRetryPolicy);
        }

        /// <summary>
        /// Failed messages will not be retried and will be sent directly to the configured error queue.
        /// </summary>
        public void DisableRetries() => SetDefaultRetryPolicySettings(0, TimeSpan.MinValue);

        /// <summary>
        /// The site key to use, this goes hand in hand with Bus.SendToSites(key, message).
        /// </summary>
        /// <param name="siteKey">The site key.</param>
        /// <param name="address">The channel address.</param>
        /// <param name="type">The channel type. Default is `http`.</param>
        /// <param name="legacyMode">Pass `true` to set the forwarding mode for this site to legacy mode.</param>
        public void AddSite(string siteKey, string address, string type = "http", bool legacyMode = false)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(siteKey);
            ArgumentException.ThrowIfNullOrWhiteSpace(address);
            ArgumentException.ThrowIfNullOrWhiteSpace(type);

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

        /// <summary>
        /// Adds a receive channel that the gateway should listen to.
        /// </summary>
        /// <param name="address">The channel address.</param>
        /// <param name="type">The channel type. Default is `http`.</param>
        /// <param name="maxConcurrency">Maximum number of receive connections. Default is `1`.</param>
        /// <param name="isDefault">True if this should be the default channel for send operations. Default is `false`.</param>
        public void AddReceiveChannel(string address, string type = "http", int maxConcurrency = 1, bool isDefault = false)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(address);
            ArgumentException.ThrowIfNullOrWhiteSpace(type);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConcurrency);

            var channels = settings.GetOrCreate<List<ReceiveChannel>>();

            channels.Add(new ReceiveChannel
            {
                Address = address,
                MaxConcurrency = maxConcurrency,
                Type = type,
                Default = isDefault
            });
        }

        /// <summary>
        /// Sets the reply-to address for messages sent from this Gateway. Useful for setting a publicly-accessible
        /// load balancer address that will be routed to this Gateway, but the local system would be unable
        /// to bind an HTTP listener (or other type of listener) to that address.
        /// </summary>
        /// <param name="replyToUri">The publicly-accessible Uri to include on outgoing messages as the Reply-To address.</param>
        /// <param name="type">The address type. Default is `http`. Must match one of the incoming receive channels.</param>
        public void SetReplyToUri(string replyToUri, string type = "http")
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(replyToUri);
            ArgumentException.ThrowIfNullOrWhiteSpace(type);

            settings.Set("Gateway.ReplyToUri", (type, replyToUri));
        }

        /// <summary>
        /// Configures the transaction timeout to use when transmitting messages to remote sites. By default, the transaction timeout of the underlying transport is used.
        /// </summary>
        /// <param name="timeout">The new timeout value.</param>
        public void TransactionTimeout(TimeSpan timeout)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);

            settings.Set("Gateway.TransactionTimeout", timeout);
        }

        internal static TimeSpan? GetTransactionTimeout(IReadOnlySettings settings)
        {
            return settings.TryGet("Gateway.TransactionTimeout", out TimeSpan? timeout) ? timeout : null;
        }

        internal static List<Site> GetConfiguredSites(IReadOnlySettings settings)
        {
            return settings.TryGet(out List<Site> sites) ? sites : [];
        }

        internal static List<ReceiveChannel> GetConfiguredChannels(IReadOnlySettings settings)
        {
            return settings.TryGet(out List<ReceiveChannel> channels) ? channels : [];
        }

        internal static (string type, string address) GetReplyToUri(IReadOnlySettings settings)
        {
            return settings.TryGet("Gateway.ReplyToUri", out (string type, string address) values) ? values : (null, null);
        }

        void SetDefaultRetryPolicySettings(int numberOfRetries, TimeSpan timeIncrease)
        {
            settings.Set("Gateway.Retries.RetryPolicy", DefaultRetryPolicy.Build(numberOfRetries, timeIncrease));
        }

        readonly SettingsHolder settings;
    }
}