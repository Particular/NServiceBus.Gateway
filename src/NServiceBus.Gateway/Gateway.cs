namespace NServiceBus.Features
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Config;
    using ConsistencyGuarantees;
    using DeliveryConstraints;
    using Extensibility;
    using Installation;
    using Logging;
    using NServiceBus.DataBus;
    using NServiceBus.Gateway;
    using NServiceBus.Gateway.Channels;
    using NServiceBus.Gateway.Deduplication;
    using NServiceBus.Gateway.HeaderManagement;
    using NServiceBus.Gateway.Notifications;
    using NServiceBus.Gateway.Receiving;
    using NServiceBus.Gateway.Routing;
    using NServiceBus.Gateway.Routing.Endpoints;
    using NServiceBus.Gateway.Routing.Sites;
    using NServiceBus.Gateway.Sending;
    using Performance.TimeToBeReceived;
    using Transports;

    /// <summary>
    /// Used to configure the gateway.
    /// </summary>
    public class Gateway : Feature
    {
        internal Gateway()
        {
            
        }

        /// <summary>
        ///     Called when the features is activated
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            ConfigureTransaction(context);

            string gatewayInputAddress;

            var requiredTransactionSupport = context.Settings.GetRequiredTransactionModeForReceives();

            var gatewayPipeline = context.AddSatellitePipeline("Gateway", "gateway", requiredTransactionSupport, PushRuntimeSettings.Default, out gatewayInputAddress);

            var channelManager = CreateChannelManager(context);
            var channelFactory = CreateChannelFactory(context);
            
            gatewayPipeline.Register("GatewaySendProcessor", b => new GatewaySendBehavior(gatewayInputAddress, channelManager, new MessageNotifier(), b.Build<IDispatchMessages>(), context.Settings, CreateForwarder(context, channelFactory, b.BuildAll<IDataBus>()?.FirstOrDefault()), CreateSiteRouters(context)), "Processes messages to be sent to the gateway");
            context.Pipeline.Register("RouteToGateway", b => new RouteToGatewayBehaviour(gatewayInputAddress), "Reroutes gateway messages to the gateway");

            RegisterHttpListenerInstaller(context);
            context.Pipeline.Register("GatewayIncomingBehavior", typeof(GatewayIncomingBehavior), "Extracts gateway related information from the incoming message");
            context.Pipeline.Register("GatewayOutgoingBehavior", typeof(GatewayOutgoingBehavior), "Puts gateway related information on the headers of outgoing messages");
            context.RegisterStartupTask(b => new GatewayReceiverStartupTask(channelManager, channelFactory, GetEndpointRouter(context), b.Build<IDispatchMessages>(), b.Build<IDeduplicateMessages>(), b.BuildAll<IDataBus>()?.FirstOrDefault(), gatewayInputAddress));
        }

        static IForwardMessagesToSites CreateForwarder(FeatureConfigurationContext context, ChannelFactory channelFactory, IDataBus databus)
        {
            return context.Settings.HasSetting("GatewayMessageForwarder") ? context.Settings.Get<IForwardMessagesToSites>("GatewayMessageForwarder") : new SingleCallChannelForwarder(channelFactory, databus);
        }
        
        static IRouteMessagesToEndpoints GetEndpointRouter(FeatureConfigurationContext context)
        {
            return context.Settings.HasSetting("GatewayEndpointRouter") ? context.Settings.Get<IRouteMessagesToEndpoints>("GatewayEndpointRouter") : new DefaultEndpointRouter
            {
                MainInputAddress = context.Settings.EndpointName().ToString()
            };
        }

        static void ConfigureTransaction(FeatureConfigurationContext context)
        {
            var configSection = context.Settings.GetConfigSection<GatewayConfig>();

            if (configSection != null)
            {
                GatewayTransaction.ConfiguredTimeout = configSection.TransactionTimeout;
            }
        }

        static ChannelFactory CreateChannelFactory(FeatureConfigurationContext context)
        {
            var channelFactory = new ChannelFactory();

            foreach (
                var type in
                    context.Settings.GetAvailableTypes().Where(t => typeof(IChannelReceiver).IsAssignableFrom(t) && !t.IsInterface))
            {
                channelFactory.RegisterReceiver(type);
            }

            foreach (
                var type in
                    context.Settings.GetAvailableTypes().Where(t => typeof(IChannelSender).IsAssignableFrom(t) && !t.IsInterface))
            {
                channelFactory.RegisterSender(type);
            }
            return channelFactory;
        }

        static IManageReceiveChannels CreateChannelManager(FeatureConfigurationContext context)
        {
            var configSection = context.Settings.GetConfigSection<GatewayConfig>();

            if (configSection != null && configSection.GetChannels().Any())
            {
                return new ConfigurationBasedChannelManager { ReceiveChannels = configSection.GetChannels().ToList() };
            }
            
            return new ConventionBasedChannelManager { EndpointName = context.Settings.EndpointName().ToString() };

        }

        
        static IEnumerable<IRouteMessagesToSites> CreateSiteRouters(FeatureConfigurationContext context)
        {
            var messageToSitesRouters = new List<IRouteMessagesToSites>();

            if (context.Settings.HasSetting("GatewaySiteRouter"))
            {
                messageToSitesRouters.Add(context.Settings.Get<IRouteMessagesToSites>("GatewaySiteRouter"));
            }
            var sites = new Dictionary<string, Site>();

            var section = context.Settings.GetConfigSection<GatewayConfig>();
            if (section != null)
            {
                sites = section.SitesAsDictionary();
            }

            var configurationBasedRouter = new ConfigurationBasedSiteRouter { Sites = sites};
            messageToSitesRouters.Add(configurationBasedRouter);

            messageToSitesRouters.Add(new OriginatingSiteHeaderRouter());
            messageToSitesRouters.Add(new KeyPrefixConventionSiteRouter());

            return messageToSitesRouters;
        }

       static void RegisterHttpListenerInstaller(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<GatewayHttpListenerInstaller>(DependencyLifecycle.InstancePerCall)
                  .ConfigureProperty(t => t.Enabled, true);
        }

        class GatewayReceiverStartupTask : FeatureStartupTask
        {
            public GatewayReceiverStartupTask(IManageReceiveChannels channelManager, ChannelFactory channelFactory, IRouteMessagesToEndpoints endpointRouter, IDispatchMessages dispatcher, IDeduplicateMessages deduplicator, IDataBus databus, string replyToAddress)
            {
                dispatchMessages = dispatcher;
                this.deduplicator = deduplicator;
                this.databus = databus;
                routeMessagesToEndpoints = endpointRouter;
                manageReceiveChannels = channelManager;
                this.channelFactory = channelFactory;
                this.replyToAddress = replyToAddress;
            }
            
            protected override Task OnStart(IBusSession context)
            {
                foreach (var receiveChannel in manageReceiveChannels.GetReceiveChannels())
                {
                    var receiver = new SingleCallChannelReceiver(channelFactory, deduplicator, databus);

                    receiver.Start(receiveChannel, receiveChannel.NumberOfWorkerThreads, MessageReceivedOnChannel);
                    activeReceivers.Add(receiver);

                    Logger.InfoFormat("Receive channel started: {0}", receiveChannel);
                }

                return Task.FromResult(0);
            }


            protected override Task OnStop(IBusSession context)
            {
                Logger.InfoFormat("Receiver is shutting down");

                foreach (var channelReceiver in activeReceivers)
                {
                    Logger.InfoFormat("Stopping channel - {0}", channelReceiver.GetType());

                    channelReceiver.Dispose();
                }

                activeReceivers.Clear();

                Logger.InfoFormat("Receiver shutdown complete");
                return Task.FromResult(0);
            }

            async Task MessageReceivedOnChannel(MessageReceivedOnChannelArgs e)
            {
                var body = e.Body;
                var headers = e.Headers;
                var id = e.Id;
                var recoverable = e.Recoverable;
                var timeToBeReceived = e.TimeToBeReceived;

                var destination = routeMessagesToEndpoints.GetDestinationFor(headers);

                Logger.Info("Sending message to " + destination);

                var outgoingMessage = new OutgoingMessage(id, headers, body);
                outgoingMessage.Headers[Headers.ReplyToAddress] = replyToAddress;

                var deliveryConstraints = new List<DeliveryConstraint>
                {
                    new DiscardIfNotReceivedBefore(timeToBeReceived)
                };

                if(!recoverable)
                {
                    deliveryConstraints.Add(new NonDurableDelivery());
                }

                var operation = new UnicastTransportOperation(outgoingMessage, destination, deliveryConstraints);
                await dispatchMessages.Dispatch(WrapInOperations(operation), new ContextBag()).ConfigureAwait(false);
            }

            static TransportOperations WrapInOperations(UnicastTransportOperation operation)
            {
                return new TransportOperations(Enumerable.Empty<MulticastTransportOperation>(), new[]
                {
                operation
            });
            }

            static ILog Logger = LogManager.GetLogger<GatewayReceiverStartupTask>();
            readonly ICollection<IReceiveMessagesFromSites> activeReceivers = new List<IReceiveMessagesFromSites>();
            readonly IManageReceiveChannels manageReceiveChannels;
            readonly ChannelFactory channelFactory;
            readonly IRouteMessagesToEndpoints routeMessagesToEndpoints;
            readonly IDispatchMessages dispatchMessages;
            readonly IDeduplicateMessages deduplicator;
            readonly IDataBus databus;
            readonly string replyToAddress;
        }
    }
}