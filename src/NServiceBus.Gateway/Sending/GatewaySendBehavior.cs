namespace NServiceBus.Gateway.Sending
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Gateway.Notifications;
    using NServiceBus.Gateway.Receiving;
    using NServiceBus.Gateway.Routing;
    using NServiceBus.Gateway.Routing.Sites;
    using NServiceBus.Routing;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    class GatewaySendBehavior
    {
        public GatewaySendBehavior(string inputAddress, IManageReceiveChannels channelManager, MessageNotifier notifier, IDispatchMessages dispatchMessages, ReadOnlySettings settings, SingleCallChannelForwarder forwarder, ConfigurationBasedSiteRouter configRouter)
        {
            this.configRouter = configRouter;
            messageNotifier = notifier;
            this.settings = settings;
            this.forwarder = forwarder;
            dispatcher = dispatchMessages;
            this.channelManager = channelManager;
            this.inputAddress = inputAddress;
        }

        protected async Task Terminate(PushContext context)
        {
            var message = new IncomingMessage(context.MessageId, context.Headers, context.BodyStream);
            var headers = message.Headers;
            var body = message.Body;

            var intent = GetMessageIntent(message);

            var destinationSites = GetDestinationSitesFor(headers, intent);

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

        static MessageIntentEnum GetMessageIntent(IncomingMessage message)
        {
            string messageIntentString;
            MessageIntentEnum messageIntent;

            if (message.Headers.TryGetValue(Headers.MessageIntent, out messageIntentString) && Enum.TryParse(messageIntentString, true, out messageIntent))
            {
                return messageIntent;
            }
            return MessageIntentEnum.Send;
        }


        IList<Site> GetDestinationSitesFor(Dictionary<string, string> headers, MessageIntentEnum intent)
        {
            if (intent == MessageIntentEnum.Reply)
            {
                return OriginatingSiteHeaderRouter.GetDestinationSitesFor(headers).ToList();
            }

            var conventionRoutes = KeyPrefixConventionSiteRouter.GetDestinationSitesFor(headers);
            var configuredRoutes = configRouter.GetDestinationSitesFor(headers);

            return conventionRoutes.Concat(configuredRoutes).ToList();
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

        IManageReceiveChannels channelManager;
        IDispatchMessages dispatcher;
        ReadOnlySettings settings;
        SingleCallChannelForwarder forwarder;
        ConfigurationBasedSiteRouter configRouter;
        MessageNotifier messageNotifier;
        string inputAddress;
    }
}