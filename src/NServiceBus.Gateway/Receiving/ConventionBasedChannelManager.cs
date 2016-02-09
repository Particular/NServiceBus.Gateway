namespace NServiceBus.Gateway.Receiving
{
    using System.Collections.Generic;
    using System.Linq;
    using Channels;

    class ConventionBasedChannelManager : IManageReceiveChannels
    {
        public IEnumerable<ReceiveChannel> GetReceiveChannels()
        {
            yield return new ReceiveChannel
            {
                Address = $"http://localhost/{EndpointName}/",
                Type = "Http",
                MaxConcurrency = 1
            };
        }

        public Channel GetDefaultChannel()
        {
            return GetReceiveChannels().First();
        }

        public string EndpointName { get; set; }
    }
}