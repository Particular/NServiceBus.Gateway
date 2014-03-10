namespace NServiceBus.Gateway.V2.Notifications
{
    using System;

    public interface INotifyAboutMessages
    {
        event EventHandler<MessageReceivedOnChannelArgs> MessageForwarded;
    }
}