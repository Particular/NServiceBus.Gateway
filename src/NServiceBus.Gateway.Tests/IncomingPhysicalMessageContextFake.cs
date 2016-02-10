namespace NServiceBus.Gateway.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Extensibility;
    using HeaderManagement;
    using NServiceBus.Pipeline;
    using ObjectBuilder;
    using Transports;

    class IncomingPhysicalMessageContextFake : IIncomingPhysicalMessageContext
    {
        public IncomingPhysicalMessageContextFake(GatewayIncomingBehavior.ReturnState state = null, Dictionary < string, string> headers = null)
        {
            var messageHeaders  = headers ?? new Dictionary<string, string>();
            Message = new IncomingMessage(Guid.NewGuid().ToString(), messageHeaders, new MemoryStream());

            Extensions = new ContextBag();
            if (state != null)
            {
                Extensions.Set(state);
            }
        }

        public ContextBag Extensions { get; }
        public IBuilder Builder { get; }
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

        public Task Reply(object message, ReplyOptions options)
        {
            throw new NotImplementedException();
        }

        public Task Reply<T>(Action<T> messageConstructor, ReplyOptions options)
        {
            throw new NotImplementedException();
        }

        public Task ForwardCurrentMessageTo(string destination)
        {
            throw new NotImplementedException();
        }

        public string MessageId { get; }
        public string ReplyToAddress { get; }
        public IReadOnlyDictionary<string, string> MessageHeaders { get; }
        public IncomingMessage Message { get; }
    }
}