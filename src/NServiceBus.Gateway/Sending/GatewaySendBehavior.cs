namespace NServiceBus.Gateway.Sending
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using Notifications;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using Routing;
    using Receiving;
    using Settings;
    using Transports;
    using IRouteMessagesToSites = NServiceBus.Gateway.Routing.Sites.IRouteMessagesToSites;

    class GatewaySendBehavior : PipelineTerminator<ISatelliteProcessingContext>
    {
        readonly IManageReceiveChannels channelManager;
        readonly IDispatchMessages dispatcher;
        readonly ReadOnlySettings settings;
        readonly SingleCallChannelForwarder forwarder;
        readonly IEnumerable<IRouteMessagesToSites> routers;
        readonly MessageNotifier messageNotifier;
        readonly string inputAddress;

        public GatewaySendBehavior(string inputAddress, IManageReceiveChannels channelManager, MessageNotifier notifier, IDispatchMessages dispatchMessages, ReadOnlySettings settings, SingleCallChannelForwarder forwarder, IEnumerable<IRouteMessagesToSites> routers)
        {
            this.routers = routers;
            messageNotifier = notifier;
            this.settings = settings;
            this.forwarder = forwarder;
            dispatcher = dispatchMessages;
            this.channelManager = channelManager;
            this.inputAddress = inputAddress;
        }

        protected override async Task Terminate(ISatelliteProcessingContext context)
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
            return routers.SelectMany(r => r.GetDestinationSitesFor(headers)).ToList();
        }

        Task CloneAndSendLocal(byte[] body, Dictionary<string, string> headers, Site destinationSite)
        {
            headers[Headers.DestinationSites] = destinationSite.Key;

            var message = new OutgoingMessage(headers[Headers.MessageId], headers, body);
            var operation = new TransportOperation(message, new UnicastAddressTag(inputAddress));

            return dispatcher.Dispatch(new TransportOperations(operation), new ContextBag());
        }

        async Task SendToSite(byte[] body, Dictionary<string, string> headers, Site targetSite)
        {
            headers[Headers.OriginatingSite] = GetDefaultAddressForThisSite();
            
            await forwarder.Forward(body, headers, targetSite).ConfigureAwait(false);

            messageNotifier.RaiseMessageForwarded(settings.LocalAddress(), targetSite.Channel.Type, body, headers);
        }

        string GetDefaultAddressForThisSite()
        {
            var defaultChannel = channelManager.GetDefaultChannel();
            return $"{defaultChannel.Type},{defaultChannel.Address}";
        }
    }
}
