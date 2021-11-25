namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Microsoft.Extensions.DependencyInjection;
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
    class Gateway : Feature
    {
        internal Gateway()
        {
            DependsOn("NServiceBus.Features.DelayedDeliveryFeature");
            Defaults(s => s.SetDefault("Gateway.Retries.RetryPolicy", DefaultRetryPolicy.BuildWithDefaults()));

            // since the installers are registered even if the feature isn't enabled we need to make
            // this a no-op if the installer runs without the feature enabled
            Defaults(c => c.Set(new InstallerSettings()));
        }

        /// <summary>
        /// Called when the features is activated
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            if (context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"))
            {
                throw new InvalidOperationException("Gateway is not support for send only endpoints.");
            }

            var storageConfiguration = context.Settings.Get<GatewayDeduplicationConfiguration>();
            storageConfiguration.Setup(context.Settings);

            var transportDefinition = context.Settings.Get<TransportDefinition>();

            ConfigureTransaction(context);

            var channelManager = CreateChannelManager(context.Settings);

            RegisterChannels(context, channelManager, out var channelSenderFactory, out var channelReceiverFactory);

            var retryPolicy = context.Settings.Get<Func<IncomingMessage, Exception, int, TimeSpan>>("Gateway.Retries.RetryPolicy");

            var logicalGatewayAddress = new QueueAddress(context.Settings.EndpointQueueName(), null, null, "gateway");
            var replyToAddress = GetReplyToAddress(context.Settings, channelManager);

            context.Services.AddSingleton(b => new GatewayMessageSender(
                b.GetRequiredService<ITransportAddressResolver>().ToTransportAddress(logicalGatewayAddress),
                new MessageNotifier(),
                b.GetRequiredService<ReceiveAddresses>().MainReceiveAddress,
                GetConfigurationBasedSiteRouter(context),
                replyToAddress,
                b.GetRequiredService<IMessageDispatcher>(),
                CreateForwarder(channelSenderFactory, b.GetServices<IDataBus>()?.FirstOrDefault())));

            context.AddSatelliteReceiver("Gateway", logicalGatewayAddress, PushRuntimeSettings.Default,
                (config, errorContext) => GatewayRecoverabilityPolicy.Invoke(errorContext, retryPolicy, config),
                (builder, messageContext, cancellationToken) => builder.GetRequiredService<GatewayMessageSender>().SendToDestination(messageContext, cancellationToken));
            ;

            var configuredSitesKeys = GatewaySettings.GetConfiguredSites(context.Settings)
                .Select(s => s.Key)
                .ToList();

            context.Pipeline.Register("RouteToGateway", b => new RouteToGatewayBehavior(b.GetRequiredService<ITransportAddressResolver>().ToTransportAddress(logicalGatewayAddress), configuredSitesKeys), "Reroutes gateway messages to the gateway");
            context.Pipeline.Register("GatewayIncomingBehavior", new GatewayIncomingBehavior(), "Extracts gateway related information from the incoming message");
            context.Pipeline.Register("GatewayOutgoingBehavior", new GatewayOutgoingBehavior(), "Puts gateway related information on the headers of outgoing messages");

            context.RegisterStartupTask(b => new GatewayReceiverStartupTask(
                channelManager,
                channelReceiverFactory,
                GetEndpointRouter(context),
                b.GetRequiredService<IMessageDispatcher>(),
                storageConfiguration.CreateStorage(b),
                b.GetServices<IDataBus>()?.FirstOrDefault(),
                b.GetRequiredService<ITransportAddressResolver>().ToTransportAddress(logicalGatewayAddress),
                transportDefinition.TransportTransactionMode));
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

            channelReceiverFactory = s => new ChannelReceiverFactory(typeof(HttpChannelReceiver)).GetReceiver(s);
            channelSenderFactory = s => new ChannelSenderFactory(typeof(HttpChannelSender)).GetSender(s);

            var installerSettings = context.Settings.Get<InstallerSettings>();
            installerSettings.ChannelManager = channelManager;
            installerSettings.Enabled = true;
        }

        static string GetReplyToAddress(IReadOnlySettings settings, IManageReceiveChannels channelManager)
        {
            var replyToUri = GatewaySettings.GetReplyToUri(settings);
            if (replyToUri.type == null || replyToUri.address == null)
            {
                var defaultChannel = channelManager.GetDefaultChannel();
                replyToUri = (defaultChannel.Type, defaultChannel.Address);
            }

            if (replyToUri.address.Contains("*") || replyToUri.address.Contains("+"))
            {
                throw new Exception($"The address {replyToUri.address} is configured as the reply-to URI, but contains a wildcard in the URI, which would not be addressable for a reply. Please use `gatewaySettings.SetReplyToAddress(address)` with a non-wildcard address in order for replies to be transmitted properly.");
            }

            if (!channelManager.GetReceiveChannels().Any(channel => channel.Type == replyToUri.type))
            {
                throw new Exception($"The ReplyToAddress is of type `{replyToUri.type}` but there are no channels of that type configured to listen for replies.");
            }

            return $"{replyToUri.type},{replyToUri.address}";
        }

        static SingleCallChannelForwarder CreateForwarder(Func<string, IChannelSender> channelSenderFactory, IDataBus databus)
        {
            return new SingleCallChannelForwarder(channelSenderFactory, databus);
        }

        static EndpointRouter GetEndpointRouter(FeatureConfigurationContext context)
        {
            return new EndpointRouter { MainInputAddress = context.Settings.EndpointName() };
        }

        static void ConfigureTransaction(FeatureConfigurationContext context)
        {
            GatewayTransaction.ConfiguredTimeout = GatewaySettings.GetTransactionTimeout(context.Settings);
        }

        internal static IManageReceiveChannels CreateChannelManager(IReadOnlySettings settings)
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
            public GatewayReceiverStartupTask(IManageReceiveChannels channelManager, Func<string, IChannelReceiver> channelReceiverFactory, EndpointRouter endpointRouter, IMessageDispatcher dispatcher, IGatewayDeduplicationStorage deduplicationStorage, IDataBus databus, string replyToAddress, TransportTransactionMode transportTransactionMode)
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

            protected override Task OnStart(IMessageSession context, CancellationToken cancellationToken = default)
            {
                // only use transaction scope if both transport and persistence are able to enlist with the transaction scope.
                // If one of them cannot enlist, use no transaction scope as partial rollbacks of the deduplication process can cause incorrect side effects.
                var useTransactionScope = deduplicationStorage.SupportsDistributedTransactions && transportTransactionMode == TransportTransactionMode.TransactionScope;
                Logger.DebugFormat("Using TransactionScope: {0} (based on storage TransactionScope support: {1} and transport transaction mode: {2}).", useTransactionScope, deduplicationStorage.SupportsDistributedTransactions, transportTransactionMode);

                foreach (var receiveChannel in manageReceiveChannels.GetReceiveChannels())
                {
                    var receiver = new SingleCallChannelReceiver(channelReceiverFactory, deduplicationStorage, databus, useTransactionScope);

                    receiver.Start(receiveChannel, receiveChannel.MaxConcurrency, MessageReceivedOnChannel);
                    activeReceivers.Add(receiver);

                    Logger.InfoFormat("Receive channel started: {0}", receiveChannel);
                }

                return Task.CompletedTask;
            }


            protected override async Task OnStop(IMessageSession context, CancellationToken cancellationToken = default)
            {
                Logger.Info("Receiver is shutting down");

                var stopTasks = activeReceivers.Select(channelReceiver => channelReceiver.Stop());

                await Task.WhenAll(stopTasks).ConfigureAwait(false);

                activeReceivers.Clear();

                Logger.Info("Receiver shutdown complete");
            }

            Task MessageReceivedOnChannel(MessageReceivedOnChannelArgs e, CancellationToken cancellationToken)
            {
                var body = e.Body;
                var headers = e.Headers;
                var id = e.Id;
                var timeToBeReceived = e.TimeToBeReceived;

                var destination = endpointRouter.GetDestinationFor();

                Logger.Info("Sending message to " + destination);

                var outgoingMessage = new OutgoingMessage(id, headers, body);
                outgoingMessage.Headers[Headers.ReplyToAddress] = replyToAddress;

                var dispatchProperties = new DispatchProperties { DiscardIfNotReceivedBefore = new DiscardIfNotReceivedBefore(timeToBeReceived) };

                var transportOperations = new TransportOperations(new TransportOperation(outgoingMessage, new UnicastAddressTag(destination), dispatchProperties, DispatchConsistency.Default));
                return dispatchMessages.Dispatch(transportOperations, new TransportTransaction(), cancellationToken);
            }

            readonly TransportTransactionMode transportTransactionMode;

            ICollection<SingleCallChannelReceiver> activeReceivers = new List<SingleCallChannelReceiver>();
            IManageReceiveChannels manageReceiveChannels;
            Func<string, IChannelReceiver> channelReceiverFactory;
            EndpointRouter endpointRouter;
            IMessageDispatcher dispatchMessages;
            IGatewayDeduplicationStorage deduplicationStorage;
            IDataBus databus;
            string replyToAddress;

            static ILog Logger = LogManager.GetLogger<GatewayReceiverStartupTask>();
        }
    }
}
