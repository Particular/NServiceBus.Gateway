﻿namespace NServiceBus.Features
{
    using System;
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
    using NServiceBus.Gateway.Channels.Http;
    using NServiceBus.Gateway.Deduplication;
    using NServiceBus.Gateway.HeaderManagement;
    using NServiceBus.Gateway.Notifications;
    using NServiceBus.Gateway.Receiving;
    using NServiceBus.Gateway.Routing;
    using NServiceBus.Gateway.Routing.Endpoints;
    using NServiceBus.Gateway.Routing.Sites;
    using NServiceBus.Gateway.Sending;
    using NServiceBus.Persistence;
    using NServiceBus.Routing;
    using Performance.TimeToBeReceived;
    using Transports;


    /// <summary>
    /// Used to configure the gateway.
    /// </summary>
    public class Gateway : Feature
    {
        internal Gateway()
        {
            DependsOn("NServiceBus.Features.DelayedDeliveryFeature");
        }

        /// <summary>
        ///     Called when the features is activated
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            if (!context.Settings.Get<List<Type>>("ResultingSupportedStorages").Contains(typeof(StorageType.GatewayDeduplication)))
            {
                throw new Exception("The selected persistence doesn't have support for gateway deduplication storage. Select another persistence or disable the gateway feature using endpointConfiguration.DisableFeature<Gateway>()");
            }

            ConfigureTransaction(context);

            string gatewayInputAddress;

            var requiredTransactionSupport = context.Settings.GetRequiredTransactionModeForReceives();

            var gatewayPipeline = context.AddSatellitePipeline("Gateway", requiredTransactionSupport, PushRuntimeSettings.Default, "gateway", out gatewayInputAddress);

            var channelManager = CreateChannelManager(context);

            Func<string, IChannelSender> channelSenderFactory;
            Func<string, IChannelReceiver> channelReceiverFactory;
            RegisterChannels(context, channelManager, out channelSenderFactory, out channelReceiverFactory);

            gatewayPipeline.Register(ForwardFailedGatewayMessagesToErrorQueueBehavior.StepId, b => new ForwardFailedGatewayMessagesToErrorQueueBehavior(gatewayInputAddress, b.Build<CriticalError>()), "Handles failures when sending messages through the gateway");

            var retryPolicy = context.Settings.HasSetting("Gateway.Retries.RetryPolicy") ? context.Settings.Get<Func<IncomingMessage, Exception, int ,TimeSpan>>("Gateway.Retries.RetryPolicy") : DefaultRetryPolicy.BuildWithDefaults();

            gatewayPipeline.Register(new RetryFailedGatewayMessagesBehavior.Registration(gatewayInputAddress, retryPolicy));
            gatewayPipeline.Register("GatewaySendProcessor", b => new GatewaySendBehavior(gatewayInputAddress, channelManager, new MessageNotifier(), b.Build<IDispatchMessages>(), context.Settings, CreateForwarder(channelSenderFactory, b.BuildAll<IDataBus>()?.FirstOrDefault()), GetConfigurationBasedSiteRouter(context)), "Processes messages to be sent to the gateway");
            context.Pipeline.Register("RouteToGateway", b => new RouteToGatewayBehavior(gatewayInputAddress), "Reroutes gateway messages to the gateway");

            context.Pipeline.Register("GatewayIncomingBehavior", typeof(GatewayIncomingBehavior), "Extracts gateway related information from the incoming message");
            context.Pipeline.Register("GatewayOutgoingBehavior", typeof(GatewayOutgoingBehavior), "Puts gateway related information on the headers of outgoing messages");
            context.RegisterStartupTask(b => new GatewayReceiverStartupTask(channelManager, channelReceiverFactory, GetEndpointRouter(context), b.Build<IDispatchMessages>(), b.Build<IDeduplicateMessages>(), b.BuildAll<IDataBus>()?.FirstOrDefault(), gatewayInputAddress));
        }

        static void RegisterChannels(FeatureConfigurationContext context, IManageReceiveChannels channelManager, out Func<string, IChannelSender> channelSenderFactory, out Func<string, IChannelReceiver> channelReceiverFactory)
        {
            var usingCustomChannelProviders = context.Settings.HasSetting("GatewayChannelReceiverFactory") || context.Settings.HasSetting("GatewayChannelSenderFactory");

            if (usingCustomChannelProviders)
            {
                channelReceiverFactory = context.Settings.Get<Func<string, IChannelReceiver>>("GatewayChannelReceiverFactory");
                channelSenderFactory = context.Settings.Get<Func<string, IChannelSender>>("GatewayChannelSenderFactory");
            }
            else
            {
                channelReceiverFactory = s => new ChannelReceiverFactory(typeof(HttpChannelReceiver)).GetReceiver(s);
                channelSenderFactory = s => new ChannelSenderFactory(typeof(HttpChannelSender)).GetSender(s);
                RegisterHttpListenerInstaller(context, channelManager);
            }
        }

        static SingleCallChannelForwarder CreateForwarder(Func<string, IChannelSender> channelSenderFactory, IDataBus databus)
        {
            return new SingleCallChannelForwarder(channelSenderFactory, databus);
        }

        static EndpointRouter GetEndpointRouter(FeatureConfigurationContext context)
        {
            return new EndpointRouter { MainInputAddress = context.Settings.EndpointName().ToString() };
        }

        static void ConfigureTransaction(FeatureConfigurationContext context)
        {
            var configSection = context.Settings.GetConfigSection<GatewayConfig>();

            if (configSection != null)
            {
                GatewayTransaction.ConfiguredTimeout = configSection.TransactionTimeout;
            }
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


        static ConfigurationBasedSiteRouter GetConfigurationBasedSiteRouter(FeatureConfigurationContext context)
        {
           var sites = new Dictionary<string, Site>();

            var section = context.Settings.GetConfigSection<GatewayConfig>();
            if (section != null)
            {
                sites = section.SitesAsDictionary();
            }

            return new ConfigurationBasedSiteRouter(sites);
        }

        static void RegisterHttpListenerInstaller(FeatureConfigurationContext context, IManageReceiveChannels channelManager)
        {
            context.Container.ConfigureComponent(() => new GatewayHttpListenerInstaller(channelManager, true), DependencyLifecycle.SingleInstance);
        }

        class GatewayReceiverStartupTask : FeatureStartupTask
        {
            public GatewayReceiverStartupTask(IManageReceiveChannels channelManager, Func<string, IChannelReceiver> channelReceiverFactory, EndpointRouter endpointRouter, IDispatchMessages dispatcher, IDeduplicateMessages deduplicator, IDataBus databus, string replyToAddress)
            {
                dispatchMessages = dispatcher;
                this.deduplicator = deduplicator;
                this.databus = databus;
                this.endpointRouter = endpointRouter;
                manageReceiveChannels = channelManager;
                this.channelReceiverFactory = channelReceiverFactory;
                this.replyToAddress = replyToAddress;
            }

            protected override Task OnStart(IMessageSession context)
            {
                foreach (var receiveChannel in manageReceiveChannels.GetReceiveChannels())
                {
                    var receiver = new SingleCallChannelReceiver(channelReceiverFactory, deduplicator, databus);

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
                return dispatchMessages.Dispatch(transportOperations, new ContextBag());
            }

            static ILog Logger = LogManager.GetLogger<GatewayReceiverStartupTask>();
            ICollection<SingleCallChannelReceiver> activeReceivers = new List<SingleCallChannelReceiver>();
            IManageReceiveChannels manageReceiveChannels;
            Func<string, IChannelReceiver> channelReceiverFactory;
            EndpointRouter endpointRouter;
            IDispatchMessages dispatchMessages;
            IDeduplicateMessages deduplicator;
            IDataBus databus;
            string replyToAddress;
        }
    }
}