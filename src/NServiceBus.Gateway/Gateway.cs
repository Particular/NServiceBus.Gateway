namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Threading.Tasks;
    using Config;
    using ConsistencyGuarantees;
    using DeliveryConstraints;
    using Extensibility;
    using Installation;
    using Logging;
    using NServiceBus.Gateway;
    using NServiceBus.Gateway.Channels;
    using NServiceBus.Gateway.HeaderManagement;
    using NServiceBus.Gateway.Notifications;
    using NServiceBus.Gateway.Receiving;
    using NServiceBus.Gateway.Routing;
    using NServiceBus.Gateway.Routing.Endpoints;
    using NServiceBus.Gateway.Routing.Sites;
    using NServiceBus.Gateway.Sending;
    using Performance.TimeToBeReceived;
    using Pipeline;
    using Routing;
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
            context.Pipeline.Register<GatewayIncomingBehavior.Registration>();
            context.Pipeline.Register<GatewayOutgoingBehavior.Registration>();

            var txConfig = context.Container.ConfigureComponent<GatewayTransaction>(DependencyLifecycle.InstancePerCall);

            var configSection = context.Settings.GetConfigSection<GatewayConfig>();

            if (configSection != null)
            {
                txConfig.ConfigureProperty(c => c.ConfiguredTimeout, configSection.TransactionTimeout);
            }

            string gatewayInputAddress;

            var requiredTransactionSupport = context.Settings.GetRequiredTransactionModeForReceives();

            var gatewayPipeline = context.AddSatellitePipeline("Gateway", "gateway", requiredTransactionSupport, PushRuntimeSettings.Default, out gatewayInputAddress);

            ConfigureChannels(context);

            ConfigureReceiver(context);
            ConfigureSender(context, gatewayPipeline, gatewayInputAddress);


            context.RegisterStartupTask(b => new GatewayReceiverStartupTask(b.Build<IManageReceiveChannels>(), b.Build<IRouteMessagesToEndpoints>(), b.Build<IDispatchMessages>(), b.Build<Func<IReceiveMessagesFromSites>>(), gatewayInputAddress));
        }

        static void ConfigureChannels(FeatureConfigurationContext context)
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

            context.Container.RegisterSingleton<IChannelFactory>(channelFactory);
        }

        static void ConfigureSender(FeatureConfigurationContext context, PipelineSettings gateWayPipeline, string gatewayInputAddress)
        {
            if (!context.Container.HasComponent<IForwardMessagesToSites>())
            {
                context.Container.ConfigureComponent<SingleCallChannelForwarder>(DependencyLifecycle.InstancePerCall);
            }

            context.Container.ConfigureComponent<MessageNotifier>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<GatewaySendBehavior>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(b => b.InputAddress, gatewayInputAddress);
            gateWayPipeline.Register<GatewaySendBehavior.Registration>();

            var configSection = context.Settings.GetConfigSection<GatewayConfig>();

            if (configSection != null && configSection.GetChannels().Any())
            {
                context.Container.ConfigureComponent<ConfigurationBasedChannelManager>(DependencyLifecycle.SingleInstance)
                    .ConfigureProperty(c => c.ReceiveChannels, configSection.GetChannels());
            }
            else
            {
                context.Container.ConfigureComponent<ConventionBasedChannelManager>(DependencyLifecycle.SingleInstance)
                    .ConfigureProperty(t => t.EndpointName, context.Settings.EndpointName());
            }

            ConfigureSiteRouters(context);
        }

        static void ConfigureSiteRouters(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<OriginatingSiteHeaderRouter>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<KeyPrefixConventionSiteRouter>(DependencyLifecycle.SingleInstance);

            var sites = new Dictionary<string, Site>();

            var section = context.Settings.GetConfigSection<GatewayConfig>();
            if (section != null)
            {
                sites = section.SitesAsDictionary();
            }

            context.Container.ConfigureComponent<ConfigurationBasedSiteRouter>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(p => p.Sites, sites);
        }

        static void ConfigureReceiver(FeatureConfigurationContext context)
        {
            if (!context.Container.HasComponent<IReceiveMessagesFromSites>())
            {
                context.Container.ConfigureComponent<SingleCallChannelReceiver>(DependencyLifecycle.InstancePerCall);
                context.Container.ConfigureComponent<Func<IReceiveMessagesFromSites>>(builder => () => builder.Build<SingleCallChannelReceiver>(), DependencyLifecycle.InstancePerCall);
            }
            else
            {
                context.Container.ConfigureComponent<Func<IReceiveMessagesFromSites>>(builder => () => builder.Build<IReceiveMessagesFromSites>(), DependencyLifecycle.InstancePerCall);
            }
            
            context.Container.ConfigureComponent<DataBusHeaderManager>(DependencyLifecycle.InstancePerCall);

            var endpointName = context.Settings.EndpointName().ToString();

            context.Container.ConfigureComponent<GatewayHttpListenerInstaller>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(t => t.Enabled, true);

            context.Container.ConfigureComponent<DefaultEndpointRouter>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.MainInputAddress, endpointName);
        }

        class GatewayReceiverStartupTask : FeatureStartupTask
        {
            public GatewayReceiverStartupTask(IManageReceiveChannels channelManager, IRouteMessagesToEndpoints endpointRouter, IDispatchMessages dispatcher, Func<IReceiveMessagesFromSites> receiveMessagesFromSitesFactory, string replyToAddress)
            {
                dispatchMessages = dispatcher;
                routeMessagesToEndpoints = endpointRouter;
                this.receiveMessagesFromSitesFactory = receiveMessagesFromSitesFactory;
                manageReceiveChannels = channelManager;
                this.replyToAddress = replyToAddress;
            }
            
            protected override Task OnStart(IBusSession context)
            {
                foreach (var receiveChannel in manageReceiveChannels.GetReceiveChannels())
                {
                    var receiver = receiveMessagesFromSitesFactory();

                    receiver.MessageReceived += MessageReceivedOnChannel;
                    receiver.Start(receiveChannel, receiveChannel.NumberOfWorkerThreads);
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

                    channelReceiver.MessageReceived -= MessageReceivedOnChannel;

                    channelReceiver.Dispose();
                }

                activeReceivers.Clear();

                Logger.InfoFormat("Receiver shutdown complete");
                return Task.FromResult(0);
            }

            async void MessageReceivedOnChannel(object sender, MessageReceivedOnChannelArgs e)
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
            readonly Func<IReceiveMessagesFromSites> receiveMessagesFromSitesFactory;
            readonly IRouteMessagesToEndpoints routeMessagesToEndpoints;
            readonly IDispatchMessages dispatchMessages;
            readonly string replyToAddress;
        }
    }
}