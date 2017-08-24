namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using AcceptanceTesting.Support;
    using Config;
    using Configuration.AdvancedExtensibility;
    using ObjectBuilder;
    using Persistence;

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

        public static GatewaySettings EnableGateway(this EndpointConfiguration config, GatewayConfig gatewayConfig, bool configureInMemoryPersistence = true)
        {
            config.GetSettings().Set<GatewayConfig>(gatewayConfig);

            if (configureInMemoryPersistence)
            {
                config.UsePersistence<InMemoryPersistence, StorageType.GatewayDeduplication>();
            }
            return config.Gateway();
        }
    }
}