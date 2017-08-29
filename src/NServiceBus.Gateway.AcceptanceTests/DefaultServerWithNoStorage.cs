namespace NServiceBus.Gateway.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting.Support;
    using NUnit.Framework;
    using Persistence;

    public class DefaultServerWithNoStorage : IEndpointSetupTemplate
    {
        protected bool ConfigureStorage;

        public Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointCustomizationConfiguration, Action<EndpointConfiguration> configurationBuilderCustomization)
        {
            var types = endpointCustomizationConfiguration.GetTypesScopedByTestClass();

            var endpointConfiguration = new EndpointConfiguration(endpointCustomizationConfiguration.EndpointName);

            endpointConfiguration.TypesToIncludeInScan(types);

            endpointConfiguration.Recoverability()
                .Delayed(delayed => delayed.NumberOfRetries(0))
                .Immediate(immediate => immediate.NumberOfRetries(0));

            var storageDir = Path.Combine(NServiceBusAcceptanceTest.StorageRootDir, TestContext.CurrentContext.Test.ID);

            endpointConfiguration.UseTransport<LearningTransport>()
                .StorageDirectory(storageDir);

            if (ConfigureStorage)
                endpointConfiguration.UsePersistence<InMemoryPersistence, StorageType.GatewayDeduplication>();

            endpointConfiguration.RegisterComponentsAndInheritanceHierarchy(runDescriptor);

            configurationBuilderCustomization(endpointConfiguration);

            return Task.FromResult(endpointConfiguration);
        }
    }
}