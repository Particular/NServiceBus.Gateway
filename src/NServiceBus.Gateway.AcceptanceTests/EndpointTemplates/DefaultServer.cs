namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting.Support;
    using Config.ConfigurationSource;
    using Configuration.AdvanceExtensibility;
    using Features;
    using Serialization;

    public class DefaultServer : IEndpointSetupTemplate
    {
        public DefaultServer()
        {
            typesToInclude = new List<Type>();
        }

        public DefaultServer(List<Type> typesToInclude)
        {
            this.typesToInclude = typesToInclude;
        }

        protected virtual List<string> AssembliesToExclude { get; } = new List<string>
                {
                    "NServiceBus.Callbacks"
                };


        public async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, IConfigurationSource configSource, Action<EndpointConfiguration> configurationBuilderCustomization)
        {
            var settings = runDescriptor.Settings;

            var types = endpointConfiguration.GetTypesScopedByTestClass();

            types = types.Where(t => !AssembliesToExclude.Contains(t.Assembly.GetName().Name));

            typesToInclude.AddRange(types);

            var builder = new EndpointConfiguration(endpointConfiguration.EndpointName);

            builder.TypesToIncludeInScan(typesToInclude);
            builder.CustomConfigurationSource(configSource);
            builder.EnableInstallers();

            builder.DisableFeature<TimeoutManager>();
            builder.Recoverability()
                .Delayed(delayed => delayed.NumberOfRetries(0))
                .Immediate(immediate => immediate.NumberOfRetries(0));

            await builder.DefineTransport(settings, endpointConfiguration.EndpointName).ConfigureAwait(false);

            builder.DefineBuilder(settings);
            builder.RegisterComponentsAndInheritanceHierarchy(runDescriptor);

            Type serializerType;
            if (settings.TryGet("Serializer", out serializerType))
            {
                builder.UseSerialization((SerializationDefinition) Activator.CreateInstance(serializerType));
            }
            await builder.DefinePersistence(settings, endpointConfiguration.EndpointName).ConfigureAwait(false);

            builder.GetSettings().SetDefault("ScaleOut.UseSingleBrokerQueue", true);
            configurationBuilderCustomization(builder);

            return builder;
        }

        List<Type> typesToInclude;
    }
}