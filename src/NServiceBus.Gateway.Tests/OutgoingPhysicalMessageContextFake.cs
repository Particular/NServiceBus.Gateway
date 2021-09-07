namespace NServiceBus.Gateway.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using HeaderManagement;
    using NServiceBus.Routing;
    using Pipeline;

    class OutgoingPhysicalMessageContextFake : IOutgoingPhysicalMessageContext
    {
        public OutgoingPhysicalMessageContextFake(GatewayIncomingBehavior.ReturnState state = null, Dictionary<string, string> headers = null)
        {
            Headers = headers ?? new Dictionary<string, string>();
            if (headers == null)
            {
                Headers = new Dictionary<string, string>
                {
                    [NServiceBus.Headers.CorrelationId] = Guid.NewGuid().ToString()
                };
            }

            Extensions = new ContextBag();
            if (state != null)
            {
                Extensions.Set(state);
            }
        }

        public ContextBag Extensions { get; }
        public IServiceProvider Builder { get; }
        public Task Send(object message, SendOptions options)
        {
            throw new NotImplementedException();
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            throw new NotImplementedException();
        }

        public Task Publish(object message, PublishOptions options)
        {
            throw new NotImplementedException();
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            throw new NotImplementedException();
        }

        public Task Subscribe(Type eventType, SubscribeOptions options)
        {
            throw new NotImplementedException();
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            throw new NotImplementedException();
        }

        public string MessageId { get; }
        public Dictionary<string, string> Headers { get; }
        public void UpdateMessage(ReadOnlyMemory<byte> body) => throw new NotImplementedException();

        public ReadOnlyMemory<byte> Body { get; set; }
        public IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; }
#pragma warning disable PS0002 // Instance methods on types implementing ICancellableContext should not have a CancellationToken parameter
        public CancellationToken CancellationToken { get; set; }
#pragma warning restore PS0002 // Instance methods on types implementing ICancellableContext should not have a CancellationToken parameter
    }
}