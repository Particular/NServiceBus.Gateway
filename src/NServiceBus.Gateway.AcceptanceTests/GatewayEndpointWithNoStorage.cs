﻿namespace NServiceBus.Gateway.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting.Support;
    using NUnit.Framework;

    public class GatewayEndpointWithNoStorage : IEndpointSetupTemplate
    {
        public virtual Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointCustomizationConfiguration, Action<EndpointConfiguration> configurationBuilderCustomization)
        {
            var endpointConfiguration = new EndpointConfiguration(endpointCustomizationConfiguration.EndpointName);

            endpointConfiguration.TypesToIncludeInScan(endpointCustomizationConfiguration.GetTypesScopedByTestClass());

            endpointConfiguration.Recoverability()
                .Delayed(delayed => delayed.NumberOfRetries(0))
                .Immediate(immediate => immediate.NumberOfRetries(0));

            var storageDir = Path.Combine(NServiceBusAcceptanceTest.StorageRootDir, TestContext.CurrentContext.Test.ID);

            endpointConfiguration.EnableInstallers();

            endpointConfiguration.UseTransport<LearningTransport>()
                .StorageDirectory(storageDir);

            endpointConfiguration.RegisterComponentsAndInheritanceHierarchy(runDescriptor);

            configurationBuilderCustomization(endpointConfiguration);

            return Task.FromResult(endpointConfiguration);
        }
    }
}