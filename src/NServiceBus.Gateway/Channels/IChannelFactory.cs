namespace NServiceBus.Gateway.Channels
{
    interface IChannelFactory
    {
        IChannelReceiver GetReceiver(string channelType);
        IChannelSender GetSender(string channelType);
    }
}