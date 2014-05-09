namespace NServiceBus.Connect.Receiving
{
    using System.Collections.Generic;
    using Channels;

    public interface IManageReceiveChannels
    {
        IEnumerable<ReceiveChannel> GetReceiveChannels();
        Channel GetDefaultChannel(IEnumerable<string> types);
    }
}