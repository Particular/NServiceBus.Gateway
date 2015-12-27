namespace NServiceBus.Gateway.Sending
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Extensibility;
    using Notifications;
    using NServiceBus.Routing;
    using ObjectBuilder;
    using Pipeline;
    using Receiving;
    using Routing;
    using Settings;
    using Transports;

    class GatewaySendBehavior : SatelliteBehavior
    {
        readonly IBuilder builder;
        readonly IManageReceiveChannels channelManager;
        readonly IDispatchMessages dispatcher;
        // ReSharper disable once NotAccessedField.Local
        readonly GatewayTransaction gatewayTransaction;
        readonly ReadOnlySettings settings;
        readonly IMessageNotifier messageNotifier;

        public GatewaySendBehavior(IBuilder builder, IManageReceiveChannels channelManager, IMessageNotifier notifier, IDispatchMessages dispatchMessages, ReadOnlySettings settings, GatewayTransaction transaction)
        {
            messageNotifier = notifier;
            this.settings = settings;
            gatewayTransaction = transaction;
            dispatcher = dispatchMessages;
            this.channelManager = channelManager;
            this.builder = builder;
        }

        public string InputAddress { get; set; }

        protected override async Task Terminate(IIncomingPhysicalMessageContext context)
        {
            var message = context.Message;
            var headers = message.Headers;
            var body = message.Body;
            
            var destinationSites = GetDestinationSitesFor(headers);

            //if there is more than 1 destination we break it up into multiple dispatches
            if (destinationSites.Count > 1)
            {
                foreach (var destinationSite in destinationSites)
                {
                    await CloneAndSendLocal(body, headers, destinationSite).ConfigureAwait(false);
                }

                return;
            }

            var destination = destinationSites.FirstOrDefault();

            if (destination == null)
            {
                throw new InvalidOperationException("No destination found for message");
            }

            await SendToSite(body, headers, destination).ConfigureAwait(false);
        }


        IList<Site> GetDestinationSitesFor(Dictionary<string, string> headers)
        {
            return builder.BuildAll<IRouteMessagesToSites>()
                .SelectMany(r => r.GetDestinationSitesFor(headers)).ToList();
        }

        Task CloneAndSendLocal(byte[] body, Dictionary<string, string> headers, Site destinationSite)
        {
            headers[Headers.DestinationSites] = destinationSite.Key;

            var message = new OutgoingMessage(headers[Headers.MessageId], headers, body);
            var operation = new UnicastTransportOperation(message, InputAddress);

            return dispatcher.Dispatch(WrapInOperations(operation), new ContextBag());
        }

        static TransportOperations WrapInOperations(UnicastTransportOperation operation)
        {
            return new TransportOperations(Enumerable.Empty<MulticastTransportOperation>(), new[]
            {
                operation
            });
        }

        async Task SendToSite(byte[] body, Dictionary<string, string> headers, Site targetSite)
        {
            headers[Headers.OriginatingSite] = GetDefaultAddressForThisSite();

            var forwarder = builder.Build<IForwardMessagesToSites>();

            await forwarder.Forward(body, headers, targetSite).ConfigureAwait(false);

            messageNotifier.RaiseMessageForwarded(settings.LocalAddress(), targetSite.Channel.Type, body, headers);
        }

        string GetDefaultAddressForThisSite()
        {
            var defaultChannel = channelManager.GetDefaultChannel();
            return $"{defaultChannel.Type},{defaultChannel.Address}";
        }

        //TODO: public Action<TransportReceiver> GetReceiverCustomization()
        //{
        //    return transport =>
        //    {
        //        transport.TransactionSettings.TransactionTimeout =
        //            GatewayTransaction.Timeout(transport.TransactionSettings.TransactionTimeout);
        //    };
        //}

        public class Registration : RegisterStep
        {
            public Registration()
                : base("GatewaySendProcessor", typeof(GatewaySendBehavior), "Processes messages to be sent to the gateway")
            {
            }
        }
    }
}
