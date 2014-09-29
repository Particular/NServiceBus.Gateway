namespace NServiceBus.Gateway.Receiving
{
    using System.Collections.Generic;
    using Channels;

    interface IManageReceiveChannels
    {
        IEnumerable<ReceiveChannel> GetReceiveChannels();
        Channel GetDefaultChannel();
    }
}