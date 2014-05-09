namespace NServiceBus.Connect.Notifications
{
    public interface IMessageNotifier : INotifyAboutMessages
    {
        void RaiseMessageForwarded(string fromChannel, string toChannel, TransportMessage message);
    }
}