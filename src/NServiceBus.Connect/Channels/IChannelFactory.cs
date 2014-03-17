namespace NServiceBus.Connect.Channels
{
    public interface IChannelFactory
    {
        IChannelReceiver GetReceiver(string channelType);
        IChannelSender GetSender(string channelType);
    }
}