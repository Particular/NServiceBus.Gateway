namespace NServiceBus.Gateway.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting.Support;
    using NUnit.Framework;

    public class DefaultServerWithNoStorage : IEndpointSetupTemplate
    {
        public async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointCustomizationConfiguration, Action<EndpointConfiguration> configurationBuilderCustomization)
        {
            var endpointConfiguration = new EndpointConfiguration(endpointCustomizationConfiguration.EndpointName);

            endpointConfiguration.TypesToIncludeInScan(endpointCustomizationConfiguration.GetTypesScopedByTestClass());

            endpointConfiguration.Recoverability()
                .Delayed(delayed => delayed.NumberOfRetries(0))
                .Immediate(immediate => immediate.NumberOfRetries(0));

            var storageDir = Path.Combine(NServiceBusAcceptanceTest.StorageRootDir, TestContext.CurrentContext.Test.ID);

            endpointConfiguration.UseTransport<LearningTransport>()
                .StorageDirectory(storageDir);

            if (ConfigureStorage)
            {
                var persistenceConfiguration = GatewayTestSuiteConstraints.Current.CreatePersistenceConfiguration();

                await persistenceConfiguration.Configure(endpointCustomizationConfiguration.EndpointName, endpointConfiguration, runDescriptor.Settings).ConfigureAwait(false);

                runDescriptor.OnTestCompleted(_ => persistenceConfiguration.Cleanup());
            }

            endpointConfiguration.RegisterComponentsAndInheritanceHierarchy(runDescriptor);

            configurationBuilderCustomization(endpointConfiguration);

            return endpointConfiguration;
        }

        protected bool ConfigureStorage;
    }
}