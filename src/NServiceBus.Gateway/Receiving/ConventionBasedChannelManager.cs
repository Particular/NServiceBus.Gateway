namespace NServiceBus.Gateway.Receiving
{
    using System.Collections.Generic;
    using System.Linq;
    using Channels;

    class ConventionBasedChannelManager : IManageReceiveChannels
    {
        public ConventionBasedChannelManager(string endpointName)
        {
            this.endpointName = endpointName;
        }
        public IEnumerable<ReceiveChannel> GetReceiveChannels()
        {
            yield return new ReceiveChannel
            {
                Address = $"http://localhost/{endpointName}/",
                Type = "Http",
                MaxConcurrency = 1
            };
        }

        public Channel GetDefaultChannel()
        {
            return GetReceiveChannels().First();
        }

        readonly string endpointName;
    }
}