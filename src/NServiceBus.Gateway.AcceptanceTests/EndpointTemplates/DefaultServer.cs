namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting.Support;

    public class DefaultServer : IEndpointSetupTemplate
    {
        public Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointCustomizationConfiguration, Action<EndpointConfiguration> configurationBuilderCustomization)
        {
            var types = endpointCustomizationConfiguration.GetTypesScopedByTestClass();

            var endpointConfiguration = new EndpointConfiguration(endpointCustomizationConfiguration.EndpointName);

            endpointConfiguration.TypesToIncludeInScan(types);
           
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
    }
}