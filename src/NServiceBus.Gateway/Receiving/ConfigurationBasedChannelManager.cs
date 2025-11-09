namespace NServiceBus.Gateway.Receiving
{
    using System.Collections.Generic;
    using System.Linq;
    using Channels;

    class ConfigurationBasedChannelManager : IManageReceiveChannels
    {
        public ConfigurationBasedChannelManager(List<ReceiveChannel> receiveChannels)
        {
            this.receiveChannels = receiveChannels;
        }

        public IEnumerable<ReceiveChannel> GetReceiveChannels()
        {
            return receiveChannels;
        }

        public Channel GetDefaultChannel()
        {
            var defaultChannel = receiveChannels.SingleOrDefault(c => c.Default);

            if (defaultChannel == null)
            {
                return receiveChannels.First();
            }

            return defaultChannel;
        }

        readonly List<ReceiveChannel> receiveChannels;
    }
}