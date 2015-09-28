namespace NServiceBus.Gateway.Sending
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Notifications;
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

        protected override Task Terminate(PhysicalMessageProcessingStageBehavior.Context context)
        {
            var message = context.GetPhysicalMessage();
            var destinationSites = GetDestinationSitesFor(message);

            //if there is more than 1 destination we break it up into multiple dispatcher
            if (destinationSites.Count > 1)
            {
                foreach (var destinationSite in destinationSites)
                {
                    CloneAndSendLocal(message, destinationSite);
                }

                return Task.FromResult(0);
            }

            var destination = destinationSites.FirstOrDefault();

            if (destination == null)
            {
                throw new InvalidOperationException("No destination found for message");
            }

            SendToSite(message, destination);

            return Task.FromResult(0);
        }


        IList<Site> GetDestinationSitesFor(TransportMessage messageToDispatch)
        {
            return builder.BuildAll<IRouteMessagesToSites>()
                .SelectMany(r => r.GetDestinationSitesFor(messageToDispatch)).ToList();
        }

        void CloneAndSendLocal(TransportMessage messageToDispatch, Site destinationSite)
        {
            messageToDispatch.Headers[Headers.DestinationSites] = destinationSite.Key;

            // TODO: new SendOptions(InputAddress)
            var operation = new TransportOperation(new OutgoingMessage(messageToDispatch.Id, messageToDispatch.Headers, messageToDispatch.Body), new DispatchOptions());
            dispatcher.Dispatch(new[] { operation });
        }

        void SendToSite(TransportMessage transportMessage, Site targetSite)
        {
            transportMessage.Headers[Headers.OriginatingSite] = GetDefaultAddressForThisSite();

            var forwarder = builder.Build<IForwardMessagesToSites>();

            forwarder.Forward(transportMessage, targetSite);

            messageNotifier.RaiseMessageForwarded(settings.LocalAddress(), targetSite.Channel.Type, transportMessage);
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
