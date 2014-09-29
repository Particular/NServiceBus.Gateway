namespace NServiceBus.Gateway.Notifications
{
    using System;

    interface INotifyAboutMessages
    {
        event EventHandler<MessageReceivedOnChannelArgs> MessageForwarded;
    }
}