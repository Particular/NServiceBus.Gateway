namespace NServiceBus.Gateway.AcceptanceTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Gateway;

    class FakeChannelReceiver : IChannelReceiver
    {
        public void Start(string address, int maxConcurrency, Func<DataReceivedOnChannelArgs, CancellationToken, Task> dataReceivedOnChannel)
        {
        }

        public Task Stop()
        {
            return Task.FromResult(0);
        }
    }
}