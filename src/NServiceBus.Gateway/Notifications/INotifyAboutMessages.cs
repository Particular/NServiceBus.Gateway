namespace NServiceBus.Connect.Notifications
{
    using System;

    public interface INotifyAboutMessages
    {
        event EventHandler<MessageReceivedOnChannelArgs> MessageForwarded;
    }
}