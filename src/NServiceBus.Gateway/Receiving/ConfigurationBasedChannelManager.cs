namespace NServiceBus.Gateway.Receiving
{
    using System.Collections.Generic;
    using System.Linq;
    using Channels;

    class ConfigurationBasedChannelManager : IManageReceiveChannels
    {
    
        public List<ReceiveChannel> ReceiveChannels { get; set; }

        public IEnumerable<ReceiveChannel> GetReceiveChannels()
        {
            return ReceiveChannels;
        }

        public Channel GetDefaultChannel()
        {
            var defaultChannel = ReceiveChannels.SingleOrDefault(c => c.Default);

            if (defaultChannel == null)
            {
                return ReceiveChannels.First();
            }
            return defaultChannel;
        }

    }
}
