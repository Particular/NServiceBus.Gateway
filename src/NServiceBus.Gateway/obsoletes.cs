#pragma warning disable 1591

namespace NServiceBus.Config
{
    using System;
    using System.Configuration;

    /// <summary>
    /// Config section for the gateway
    /// </summary>
    public partial class GatewayConfig
    {
        /// <summary>
        /// Property for getting/setting the period of time when the outgoing gateway transaction times out.
        /// Defaults to the TransactionTimeout of the main transport.
        /// </summary>
        [ObsoleteEx(
            RemoveInVersion = "4.0",
            TreatAsErrorFromVersion = "3.0",
            Message = "No longer used and can safely be removed")]
        [ConfigurationProperty("TransactionTimeout", IsRequired = false, DefaultValue = "00:00:00")]
        public TimeSpan TransactionTimeout
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }
}
#pragma warning restore 1591