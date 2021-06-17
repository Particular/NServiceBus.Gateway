namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using ConsistencyGuarantees;
    using DeliveryConstraints;
    using Extensibility;
    using Logging;
    using NServiceBus.DataBus;
    using NServiceBus.Gateway;
    using NServiceBus.Gateway.Channels;
    using NServiceBus.Gateway.Channels.Http;
    using NServiceBus.Gateway.HeaderManagement;
    using NServiceBus.Gateway.Installer;
    using NServiceBus.Gateway.Notifications;
    using NServiceBus.Gateway.Receiving;
    using NServiceBus.Gateway.Routing.Endpoints;
    using NServiceBus.Gateway.Routing.Sites;
    using NServiceBus.Gateway.Sending;
    using Performance.TimeToBeReceived;
    using Routing;
    using Settings;
    using Transport;

    /// <summary>
    /// Used to configure the gateway.
    /// </summary>
    [ObsoleteEx(
        RemoveInVersion = "4.0",
        TreatAsErrorFromVersion = "3.0",
        Message = "Use `EndpointConfiguration.Gateway() to enable the gateway.")]
    public class Gateway : Feature
    {
        internal Gateway()
        {
            DependsOn("NServiceBus.Features.DelayedDeliveryFeature");
            Defaults(s => s.SetDefault("Gateway.Retries.RetryPolicy", DefaultRetryPolicy.BuildWithDefaults()));

            // since the installers are registered even if the feature isn't enabled we need to make
            // this a no-op if the installer runs without the feature enabled
            Defaults(c => c.Set<InstallerSettings>(new InstallerSettings()));
        }

        /// <summary>
        /// Called when the features is activated
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var storageConfiguration = context.Settings.Get<GatewayDeduplicationConfiguration>();
            storageConfiguration.Setup(context.Settings);


            ConfigureTransaction(context);

            var channelManager = CreateChannelManager(context.Settings);

            RegisterChannels(context, channelManager, out var channelSenderFactory, out var channelReceiverFactory);

            var gatewayInputAddress = context.Settings.GetTransportAddress(context.Settings.LogicalAddress().CreateQualifiedAddress("gateway"));

            var retryPolicy = context.Settings.Get<Func<IncomingMessage, Exception, int, TimeSpan>>("Gateway.Retries.RetryPolicy");

            var sender = new GatewayMessageSender(
                gatewayInputAddress,
                channelManager,
                new MessageNotifier(),
                context.Settings.LocalAddress(),
                GetConfigurationBasedSiteRouter(context));

            context.AddSatelliteReceiver("Gateway", gatewayInputAddress, PushRuntimeSettings.Default,
                (config, errorContext) => GatewayRecoverabilityPolicy.Invoke(errorContext, retryPolicy, config),
                (builder, messageContext) => sender.SendToDestination(messageContext, builder.Build<IDispatchMessages>(), CreateForwarder(channelSenderFactory, builder.BuildAll<IDataBus>()?.FirstOrDefault())));

            var configuredSitesKeys = GatewaySettings.GetConfiguredSites(context.Settings)
                .Select(s => s.Key)
                .ToList();

            context.Pipeline.Register("RouteToGateway", new RouteToGatewayBehavior(gatewayInputAddress, configuredSitesKeys), "Reroutes gateway messages to the gateway");
            context.Pipeline.Register("GatewayIncomingBehavior", new GatewayIncomingBehavior(), "Extracts gateway related information from the incoming message");
            context.Pipeline.Register("GatewayOutgoingBehavior", new GatewayOutgoingBehavior(), "Puts gateway related information on the headers of outgoing messages");

            context.RegisterStartupTask(b => new GatewayReceiverStartupTask(
                channelManager,
                channelReceiverFactory,
                GetEndpointRouter(context),
                b.Build<IDispatchMessages>(),
                storageConfiguration.CreateStorage(b),
                b.BuildAll<IDataBus>()?.FirstOrDefault(),
                gatewayInputAddress,
                GetTransportTransactionMode(context)));
        }

        static TransportTransactionMode GetTransportTransactionMode(FeatureConfigurationContext context)
        {
            try
            {
                return context.Settings.GetRequiredTransactionModeForReceives();
            }
            catch (Exception)
            {
                // GetRequiredTransactionModeForReceives throws on read-only endpoints.
                // Use the transport's default mode in that case:
                return context.Settings.Get<TransportInfrastructure>().TransactionMode;
            }
        }

        static void RegisterChannels(FeatureConfigurationContext context, IManageReceiveChannels channelManager, out Func<string, IChannelSender> channelSenderFactory, out Func<string, IChannelReceiver> channelReceiverFactory)
        {
            var usingCustomChannelProviders = context.Settings.HasSetting("GatewayChannelReceiverFactory") || context.Settings.HasSetting("GatewayChannelSenderFactory");

            if (usingCustomChannelProviders)
            {
                channelReceiverFactory = context.Settings.Get<Func<string, IChannelReceiver>>("GatewayChannelReceiverFactory");
                channelSenderFactory = context.Settings.Get<Func<string, IChannelSender>>("GatewayChannelSenderFactory");
                return;
            }

            CheckForNonWildcardDefaultChannel(channelManager);

            channelReceiverFactory = s => new ChannelReceiverFactory(typeof(HttpChannelReceiver)).GetReceiver(s);
            channelSenderFactory = s => new ChannelSenderFactory(typeof(HttpChannelSender)).GetSender(s);

            var installerSettings = context.Settings.Get<InstallerSettings>();
            installerSettings.ChannelManager = channelManager;
            installerSettings.Enabled = true;
        }

        static void CheckForNonWildcardDefaultChannel(IManageReceiveChannels channelManager)
        {
            var defaultChannel = channelManager.GetDefaultChannel();
            var address = defaultChannel.GetPublicAddress();
            if (address.Contains("*") || address.Contains("+"))
            {
                throw new Exception($"Default channel with public address {address} is using a wildcard uri. Please add an extra channel with a fully qualified non-wildcard uri in order for replies to be transmitted properly.");
            }
        }

        static SingleCallChannelForwarder CreateForwarder(Func<string, IChannelSender> channelSenderFactory, IDataBus databus)
        {
            return new SingleCallChannelForwarder(channelSenderFactory, databus);
        }

        static EndpointRouter GetEndpointRouter(FeatureConfigurationContext context)
        {
            return new EndpointRouter {MainInputAddress = context.Settings.EndpointName()};
        }

        static void ConfigureTransaction(FeatureConfigurationContext context)
        {
            GatewayTransaction.ConfiguredTimeout = GatewaySettings.GetTransactionTimeout(context.Settings);
        }

        internal static IManageReceiveChannels CreateChannelManager(ReadOnlySettings settings)
        {
            var channels = GatewaySettings.GetConfiguredChannels(settings);

            if (channels.Any())
            {
                return new ConfigurationBasedChannelManager(channels);
            }

            return new ConventionBasedChannelManager(settings.EndpointName());
        }

        static ConfigurationBasedSiteRouter GetConfigurationBasedSiteRouter(FeatureConfigurationContext context)
        {
            var sites = GatewaySettings.GetConfiguredSites(context.Settings);

            return new ConfigurationBasedSiteRouter(sites);
        }

        class GatewayReceiverStartupTask : FeatureStartupTask
        {
            public GatewayReceiverStartupTask(IManageReceiveChannels channelManager, Func<string, IChannelReceiver> channelReceiverFactory, EndpointRouter endpointRouter, IDispatchMessages dispatcher, IGatewayDeduplicationStorage deduplicationStorage, IDataBus databus, string replyToAddress, TransportTransactionMode transportTransactionMode)
            {
                dispatchMessages = dispatcher;
                this.deduplicationStorage = deduplicationStorage;
                this.databus = databus;
                this.endpointRouter = endpointRouter;
                manageReceiveChannels = channelManager;
                this.channelReceiverFactory = channelReceiverFactory;
                this.replyToAddress = replyToAddress;
                this.transportTransactionMode = transportTransactionMode;
            }

            protected override Task OnStart(IMessageSession context)
            {
                bool useTransactionScope;
                if (deduplicationStorage is LegacyDeduplicationWrapper)
                {
                    // the legacy deduplication storage requires the storage to support TransactionScope.
                    // This is critical when using the gateway with a non-transactional transport as a dispatch failure must undo any potential persistence changes.
                    useTransactionScope = true;
                    Logger.Debug("Using TransactionScope for legacy storage compatibility.");
                }
                else
                {
                    // only use transaction scope if both transport and persistence are able to enlist with the transaction scope.
                    // If one of them cannot enlist, use no transaction scope as partial rollbacks of the deduplication process can cause incorrect side effects.
                    useTransactionScope = deduplicationStorage.SupportsDistributedTransactions && transportTransactionMode == TransportTransactionMode.TransactionScope;
                    Logger.DebugFormat("Using TransactionScope: {0} (based on storage TransactionScope support: {1} and transport transaction mode: {2}).", useTransactionScope, deduplicationStorage.SupportsDistributedTransactions, transportTransactionMode);
                }

                foreach (var receiveChannel in manageReceiveChannels.GetReceiveChannels())
                {
                    var receiver = new SingleCallChannelReceiver(channelReceiverFactory, deduplicationStorage, databus, useTransactionScope);

                    receiver.Start(receiveChannel, receiveChannel.MaxConcurrency, MessageReceivedOnChannel);
                    activeReceivers.Add(receiver);

                    Logger.InfoFormat("Receive channel started: {0}", receiveChannel);
                }

                return Task.FromResult(0);
            }


            protected override async Task OnStop(IMessageSession context)
            {
                Logger.Info("Receiver is shutting down");

                var stopTasks = activeReceivers.Select(channelReceiver => channelReceiver.Stop());

                await Task.WhenAll(stopTasks).ConfigureAwait(false);

                activeReceivers.Clear();

                Logger.Info("Receiver shutdown complete");
            }

            Task MessageReceivedOnChannel(MessageReceivedOnChannelArgs e)
            {
                var body = e.Body;
                var headers = e.Headers;
                var id = e.Id;
                var recoverable = e.Recoverable;
                var timeToBeReceived = e.TimeToBeReceived;

                var destination = endpointRouter.GetDestinationFor(headers);

                Logger.Info("Sending message to " + destination);

                var outgoingMessage = new OutgoingMessage(id, headers, body);
                outgoingMessage.Headers[Headers.ReplyToAddress] = replyToAddress;

                var deliveryConstraints = new List<DeliveryConstraint>
                {
                    new DiscardIfNotReceivedBefore(timeToBeReceived)
                };

                if (!recoverable)
                {
                    deliveryConstraints.Add(new NonDurableDelivery());
                }

                var transportOperations = new TransportOperations(new TransportOperation(outgoingMessage, new UnicastAddressTag(destination), DispatchConsistency.Default, deliveryConstraints));
                return dispatchMessages.Dispatch(transportOperations, new TransportTransaction(), new ContextBag());
            }

            readonly TransportTransactionMode transportTransactionMode;

            ICollection<SingleCallChannelReceiver> activeReceivers = new List<SingleCallChannelReceiver>();
            IManageReceiveChannels manageReceiveChannels;
            Func<string, IChannelReceiver> channelReceiverFactory;
            EndpointRouter endpointRouter;
            IDispatchMessages dispatchMessages;
            IGatewayDeduplicationStorage deduplicationStorage;
            IDataBus databus;
            string replyToAddress;

            static ILog Logger = LogManager.GetLogger<GatewayReceiverStartupTask>();
        }
    }
}