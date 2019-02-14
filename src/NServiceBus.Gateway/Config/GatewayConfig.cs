#if NET452
namespace NServiceBus.Config
{
    using System;
    using System.Configuration;

    /// <summary>
    /// Config section for the gateway
    /// </summary>
    [ObsoleteEx(
        Message = "Configuring the gateway via configuration section is discouraged.",
        ReplacementTypeOrMember = "EndpointConfiguration.Gateway()",
        TreatAsErrorFromVersion = "4",
        RemoveInVersion = "5")]
    public class GatewayConfig : ConfigurationSection
    {
        /// <summary>
        /// Property for getting/setting the period of time when the outgoing gateway transaction times out.
        /// Defaults to the TransactionTimeout of the main transport.
        /// </summary>
        ///
        [ObsoleteEx(
            Message = "Configuring the gateway via configuration section is discouraged.",
            ReplacementTypeOrMember = "EndpointConfiguration.Gateway().TransactionTimeout",
            TreatAsErrorFromVersion = "4",
            RemoveInVersion = "5")]
        [ConfigurationProperty("TransactionTimeout", IsRequired = false, DefaultValue = "00:00:00")]
        public TimeSpan TransactionTimeout
        {
            get => (TimeSpan)this["TransactionTimeout"];
            set => this["TransactionTimeout"] = value;
        }

        /// <summary>
        /// Collection of sites
        /// </summary>
        [ObsoleteEx(
            Message = "Configuring the gateway via configuration section is discouraged.",
            ReplacementTypeOrMember = "EndpointConfiguration.Gateway().AddSite",
            TreatAsErrorFromVersion = "4",
            RemoveInVersion = "5")]
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
        [ObsoleteEx(
            Message = "Configuring the gateway via configuration section is discouraged.",
            ReplacementTypeOrMember = "EndpointConfiguration.Gateway().AddChannel",
            TreatAsErrorFromVersion = "4",
            RemoveInVersion = "5")]
        [ConfigurationProperty("Channels", IsRequired = true)]
        [ConfigurationCollection(typeof(ChannelCollection), AddItemName = "Channel")]
        public ChannelCollection Channels
        {
            get => this["Channels"] as ChannelCollection;
            set => this["Channels"] = value;
        }
    }
}
#endif
