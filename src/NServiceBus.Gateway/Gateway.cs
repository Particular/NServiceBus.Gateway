namespace NServiceBus.Connect.Features
{
    using System;
    using System.Linq;
    using NServiceBus.Config;
    using NServiceBus.Features;
    using Channels;
    using Deduplication;
    using HeaderManagement;
    using Notifications;
    using Receiving;
    using Routing.Endpoints;
    using Routing.Sites;
    using Sending;

    public class Gateway : Feature
    {
        public override void Initialize()
        {
            ConfigureChannels();

            ConfigureReceiver();

            ConfigureSender();

            //Temp until we can let the channel turn dedupe off
            Configure.Instance.Configurer.ConfigureComponent<InMemoryDeduplication>(DependencyLifecycle.SingleInstance);

            InfrastructureServices.Enable<IDeduplicateMessages>();
        }

        static void ConfigureChannels()
        {
            var registry = new ChannelTypeRegistry();

            FillChannelTypeRegistryAndContainer(registry);

            Configure.Instance.Configurer.RegisterSingleton<IChannelTypeRegistry>(registry);
            Configure.Instance.Configurer.ConfigureComponent<ChannelFactory>(DependencyLifecycle.SingleInstance);
        }

        static void FillChannelTypeRegistryAndContainer(IChannelTypeRegistry registry)
        {
            foreach (var type in Configure.TypesToScan.Where(t => typeof(IChannelReceiver).IsAssignableFrom(t) && !t.IsInterface))
            {
                var channelTypes = type.GetCustomAttributes(true).OfType<ChannelTypeAttribute>().ToList();
                if (channelTypes.Any())
                {
                    channelTypes.ForEach(t =>
                    {
                        registry.AddReceiver(t.Type, type);
                        AddToContainerIfNecessary(type);
                    });
                }
                else
                {
                    registry.AddReceiver(type.Name.Substring(0, type.Name.IndexOf("Channel")), type);
                    AddToContainerIfNecessary(type);
                }
            }

            foreach (var type in Configure.TypesToScan.Where(t => typeof(IChannelSender).IsAssignableFrom(t) && !t.IsInterface))
            {
                var channelTypes = type.GetCustomAttributes(true).OfType<ChannelTypeAttribute>().ToList();
                if (channelTypes.Any())
                {
                    channelTypes.ForEach(t =>
                    {
                        registry.AddSender(t.Type, type);
                        AddToContainerIfNecessary(type);
                    });
                }
                else
                {
                    registry.AddSender(type.Name.Substring(0, type.Name.IndexOf("Channel")), type);
                    AddToContainerIfNecessary(type);
                }
            }
        }

        static void AddToContainerIfNecessary(Type type)
        {
            if (!Configure.HasComponent(type))
            {
                Configure.Component(type, DependencyLifecycle.InstancePerCall);
            }
        }

        static void ConfigureSender()
        {
            if (!Configure.Instance.Configurer.HasComponent<IForwardMessagesToSites>())
            {
                Configure.Component<SingleCallChannelForwarder>(DependencyLifecycle.InstancePerCall);
            }

            Configure.Component<MessageNotifier>(DependencyLifecycle.SingleInstance);

            var configSection = Configure.GetConfigSection<Config.GatewayConfig>();

            if (configSection != null && configSection.GetChannels().Any())
            {
                Configure.Component<ConfigurationBasedChannelManager>(DependencyLifecycle.SingleInstance);
            }
            else
            {
                Configure.Component<ConventionBasedChannelManager>(DependencyLifecycle.SingleInstance);
            }

            ConfigureSiteRouters();
        }

        static void ConfigureSiteRouters()
        {
            Configure.Component<OriginatingSiteHeaderRouter>(DependencyLifecycle.SingleInstance);
            Configure.Component<KeyPrefixConventionSiteRouter>(DependencyLifecycle.SingleInstance);
            Configure.Component<ConfigurationBasedSiteRouter>(DependencyLifecycle.SingleInstance);
        }

        static void ConfigureReceiver()
        {
            if (!Configure.Instance.Configurer.HasComponent<IReceiveMessagesFromSites>())
            {
                Configure.Component<SingleCallChannelReceiver>(DependencyLifecycle.InstancePerCall);
            }

            Configure.Component<DataBusHeaderManager>(DependencyLifecycle.InstancePerCall);

            Configure.Component<DefaultEndpointRouter>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.MainInputAddress, Address.Parse(Configure.EndpointName));
        }
    }

    public class SetDefaultInMemoryDeduplication : IWantToRunBeforeConfigurationIsFinalized
    {
        
        public void Run()
        {
            InfrastructureServices.SetDefaultFor<IDeduplicateMessages>(() => Configure.Instance.UseInMemoryGatewayDeduplication());
        }
    }
}