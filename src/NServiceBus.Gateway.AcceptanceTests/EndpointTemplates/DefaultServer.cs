namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting.Support;
    using Features;

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


        public Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointCustomizationConfiguration, Action<EndpointConfiguration> configurationBuilderCustomization)
        {
            var types = endpointCustomizationConfiguration.GetTypesScopedByTestClass();

            types = types.Where(t => !AssembliesToExclude.Contains(t.Assembly.GetName().Name));

            typesToInclude.AddRange(types);

            var endpointConfiguration = new EndpointConfiguration(endpointCustomizationConfiguration.EndpointName);

            endpointConfiguration.TypesToIncludeInScan(typesToInclude);
            endpointConfiguration.EnableInstallers();

            endpointConfiguration.DisableFeature<TimeoutManager>();
            endpointConfiguration.Recoverability()
                .Delayed(delayed => delayed.NumberOfRetries(0))
                .Immediate(immediate => immediate.NumberOfRetries(0));


            var storageDir = Path.Combine(NServiceBusAcceptanceTest.StorageRootDir, NUnit.Framework.TestContext.CurrentContext.Test.ID);

            endpointConfiguration.UseTransport<LearningTransport>()
                .StorageDirectory(storageDir);

            endpointConfiguration.RegisterComponentsAndInheritanceHierarchy(runDescriptor);


            configurationBuilderCustomization(endpointConfiguration);

            return Task.FromResult(endpointConfiguration);
        }

        List<Type> typesToInclude;
    }
}