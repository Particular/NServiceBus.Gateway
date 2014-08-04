namespace NServiceBus.Gateway.Notifications
{
    interface IMessageNotifier : INotifyAboutMessages
    {
        void RaiseMessageForwarded(string fromChannel, string toChannel, TransportMessage message);
    }
}