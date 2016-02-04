namespace NServiceBus.Config
{
    using System;
    using System.Configuration;
    using Gateway.Channels;

    /// <summary>
    /// Used to configure <see cref="ReceiveChannel"/>.
    /// </summary>
    public class ChannelConfig : ConfigurationElement
    {
        /// <summary>
        /// True if this channel is the default channel
        /// </summary>
        [ConfigurationProperty("Default", IsRequired = false, DefaultValue = false, IsKey = false)]
        public bool Default
        {
            get
            {
                return (bool) this["Default"];
            }
            set
            {
                this["Default"] = value;
            }
        }

        /// <summary>
        /// The Address that the channel is listening on
        /// </summary>
        [ConfigurationProperty("Address", IsRequired = true, IsKey = false)]
        public string Address
        {
            get
            {
                return (string) this["Address"];
            }
            set
            {
                this["Address"] = value;
            }
        }

        /// <summary>
        /// The number of worker threads that will be used for this channel
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            Message = "NumberOfWorkerThreads has been removed. Please use the MaxConcurrency setting instead.")]
        [ConfigurationProperty("NumberOfWorkerThreads", IsRequired = false, DefaultValue = 1, IsKey = false)]
        public int NumberOfWorkerThreads
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// The maximum number of messages that should be processed at any given time.
        /// </summary>
        [ConfigurationProperty("MaxConcurrency", IsRequired = false, DefaultValue = 1, IsKey = false)]
        public int MaxConcurrency
        {
            get
            {
                return (int)this["MaxConcurrency"];
            }
            set
            {
                this["MaxConcurrency"] = value;
            }
        }

        /// <summary>
        /// The ChannelType
        /// </summary>
        [ConfigurationProperty("ChannelType", IsRequired = true, IsKey = false)]
        public string ChannelType
        {
            get
            {
                return ((string) this["ChannelType"]).ToLower();
            }
            set
            {
                this["ChannelType"] = value;
            }
        }
    }
}