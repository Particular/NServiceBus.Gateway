namespace NServiceBus.Gateway.Sending
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Notifications;
    using NServiceBus.Routing;
    using Routing;
    using Routing.Sites;
    using Transport;

    class GatewayMessageSender
    {
        public GatewayMessageSender(string inputAddress, MessageNotifier notifier, string localAddress, ConfigurationBasedSiteRouter configRouter, string replyToAddress)
        {
            this.configRouter = configRouter;
            messageNotifier = notifier;
            this.localAddress = localAddress;
            this.inputAddress = inputAddress;
            this.replyToAddress = replyToAddress;
        }

        public async Task SendToDestination(MessageContext context, IMessageDispatcher dispatcher, SingleCallChannelForwarder forwarder, CancellationToken cancellationToken = default)
        {
            var intent = GetMessageIntent(context.Headers);

            var destinationSites = GetDestinationSitesFor(context.Headers, intent);

            //if there is more than 1 destination we break it up into multiple dispatches
            if (destinationSites.Count > 1)
            {
                foreach (var destinationSite in destinationSites)
                {
                    await CloneAndSendLocal(context.Body, context.Headers, destinationSite, context.TransportTransaction, dispatcher, cancellationToken).ConfigureAwait(false);
                }

                return;
            }

            var destination = destinationSites.FirstOrDefault();

            if (destination == null)
            {
                throw new InvalidOperationException("No destination found for message");
            }

            await SendToSite(context.Body, context.Headers, destination, forwarder, cancellationToken).ConfigureAwait(false);
        }

        static MessageIntent GetMessageIntent(Dictionary<string, string> headers)
        {
            if (headers.TryGetValue(Headers.MessageIntent, out var messageIntentString) && Enum.TryParse(messageIntentString, true, out MessageIntent messageIntent))
            {
                return messageIntent;
            }
            return MessageIntent.Send;
        }


        IList<Site> GetDestinationSitesFor(Dictionary<string, string> headers, MessageIntent intent)
        {
            if (intent == MessageIntent.Reply)
            {
                return OriginatingSiteHeaderRouter.GetDestinationSitesFor(headers).ToList();
            }

            var conventionRoutes = KeyPrefixConventionSiteRouter.GetDestinationSitesFor(headers);
            var configuredRoutes = configRouter.GetDestinationSitesFor(headers);

            return conventionRoutes.Concat(configuredRoutes).ToList();
        }

        Task CloneAndSendLocal(byte[] body, Dictionary<string, string> headers, Site destinationSite, TransportTransaction transportTransaction, IMessageDispatcher dispatcher, CancellationToken cancellationToken)
        {
            headers[Headers.DestinationSites] = destinationSite.Key;

            var message = new OutgoingMessage(headers[Headers.MessageId], headers, body);
            var operation = new TransportOperation(message, new UnicastAddressTag(inputAddress));

            return dispatcher.Dispatch(new TransportOperations(operation), transportTransaction, cancellationToken);
        }

        async Task SendToSite(byte[] body, Dictionary<string, string> headers, Site targetSite, SingleCallChannelForwarder forwarder, CancellationToken cancellationToken)
        {
            headers[Headers.OriginatingSite] = replyToAddress;

            await forwarder.Forward(body, headers, targetSite, cancellationToken).ConfigureAwait(false);

            messageNotifier.RaiseMessageForwarded(localAddress, targetSite.Channel.Type, body, headers);
        }

        string localAddress;
        ConfigurationBasedSiteRouter configRouter;
        MessageNotifier messageNotifier;
        string inputAddress;
        string replyToAddress;
    }
}