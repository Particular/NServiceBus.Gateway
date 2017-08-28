namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System.Linq;
    using AcceptanceTesting.Support;
    using Config;
    using Configuration.AdvancedExtensibility;
    using NServiceBus.Gateway;
    using NServiceBus.Gateway.Channels;
    using NServiceBus.Gateway.Routing;
    using ObjectBuilder;

    public static class ConfigureExtensions
    {
        public static void RegisterComponentsAndInheritanceHierarchy(this EndpointConfiguration builder, RunDescriptor runDescriptor)
        {
            builder.RegisterComponents(r => { RegisterInheritanceHierarchyOfContextOnContainer(runDescriptor, r); });
        }


        static void RegisterInheritanceHierarchyOfContextOnContainer(RunDescriptor runDescriptor, IConfigureComponents r)
        {
            var type = runDescriptor.ScenarioContext.GetType();
            while (type != typeof(object))
            {
                r.RegisterSingleton(type, runDescriptor.ScenarioContext);
                type = type.BaseType;
            }
        }

        public static GatewaySettings EnableGateway(this EndpointConfiguration config, GatewayConfig gatewayConfig)
        {
            config.GetSettings().Set<GatewayConfig>(gatewayConfig);

            var settings = config.Gateway();

            if (gatewayConfig.Sites.Count > 0)
            {
                var sites = gatewayConfig.Sites.Cast<SiteConfig>().Select(site => new Site
                {
                    Key = site.Key,
                    Channel = new Channel
                    {
                        Type = site.ChannelType,
                        Address = site.Address
                    },
                    LegacyMode = site.LegacyMode
                }).ToList();

                settings.Sites(sites);
            }

            if (gatewayConfig.Channels.Count > 0)
            {
                var channels = (from ChannelConfig channel in gatewayConfig.Channels
                    select new ReceiveChannel
                    {
                        Address = channel.Address,
                        Type = channel.ChannelType,
                        MaxConcurrency = channel.MaxConcurrency,
                        Default = channel.Default
                    }).ToList();

                settings.Channels(channels);
            }

            return settings;
        }
    }
}