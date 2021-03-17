namespace NServiceBus.Gateway.AcceptanceTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Gateway;

    class FakeChannelReceiver : IChannelReceiver
    {
        public Task Start(string address, int maxConcurrency, Func<DataReceivedOnChannelArgs, CancellationToken, Task> dataReceivedOnChannel, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task Stop(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}