namespace NServiceBus.Gateway.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Transports;

    public class FakeDispatcher : IDispatchMessages
    {
        public FakeDispatcher()
        {
            messageReceived = new ManualResetEvent(false);
        }

        public SendDetails GetResultingMessage()
        {
            messageReceived.WaitOne(TimeSpan.FromSeconds(200));
            return details;
        }

        SendDetails details;
        ManualResetEvent messageReceived;

        public class SendDetails
        {
            public OutgoingMessage Message { get; set; }
            public string Destination { get; set; }
        }

        public Task Dispatch(IEnumerable<TransportOperation> outgoingMessages)
        {
            // TODO: How to do this?
            details = new SendDetails
            {
                // Destination = sendOptions.Destination,
                // Message = message
            };

            messageReceived.Set();
            return Task.FromResult(0);
        }
    }
}