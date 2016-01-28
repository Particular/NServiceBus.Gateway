namespace NServiceBus.Gateway.Receiving
{
    using System;
    using System.Threading.Tasks;
    using Channels;
    using Notifications;

    interface IReceiveMessagesFromSites : IDisposable
    {
        void Start(Channel channel, int numberOfWorkerThreads, Func<MessageReceivedOnChannelArgs, Task> messageReceivedHandler);
    }
}