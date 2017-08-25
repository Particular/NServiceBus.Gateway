namespace NServiceBus.Config
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using Gateway.Channels;

    /// <summary>
    /// Config section for the gateway
    /// </summary>
    public class GatewayConfig : ConfigurationSection
    {
        /// <summary>
        /// Property for getting/setting the period of time when the outgoing gateway transaction times out.
        /// Defaults to the TransactionTimeout of the main transport.
        /// </summary>
        [ConfigurationProperty("TransactionTimeout", IsRequired = false, DefaultValue = "00:00:00")]
        public TimeSpan TransactionTimeout
        {
            get => (TimeSpan)this["TransactionTimeout"];
            set => this["TransactionTimeout"] = value;
        }

        /// <summary>
        /// Collection of sites
        /// </summary>
        [ConfigurationProperty("Sites", IsRequired = true)]
        [ConfigurationCollection(typeof(SiteCollection), AddItemName = "Site")]
        public SiteCollection Sites
        {
            get => this["Sites"] as SiteCollection;
            set => this["Sites"] = value;
        }

        /// <summary>
        /// Collection of channels
        /// </summary>
        [ConfigurationProperty("Channels", IsRequired = true)]
        [ConfigurationCollection(typeof(ChannelCollection), AddItemName = "Channel")]
        public ChannelCollection Channels
        {
            get => this["Channels"] as ChannelCollection;
            set => this["Channels"] = value;
        }

        internal Dictionary<string, Gateway.Routing.Site> SitesAsDictionary()
        {
            return Sites.Cast<SiteConfig>().ToDictionary(site => site.Key, site => new Gateway.Routing.Site
            {
                Key = site.Key,
                Channel = new Channel {Type = site.ChannelType, Address = site.Address},
                LegacyMode = site.LegacyMode
            });
        }

        internal IEnumerable<ReceiveChannel> GetChannels()
        {
            return (from ChannelConfig channel in Channels
                select new ReceiveChannel
                {
                    Address = channel.Address,
                    Type = channel.ChannelType,
                    MaxConcurrency = channel.MaxConcurrency,
                    Default = channel.Default
                }).ToList();
        }
    }
}
