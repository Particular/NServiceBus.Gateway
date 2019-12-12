namespace NServiceBus.Gateway.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;

    public class GatewayEndpoint : GatewayEndpointWithNoStorage
    {
        public override Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointCustomizationConfiguration, Action<EndpointConfiguration> configurationBuilderCustomization)
        {
            return base.GetConfiguration(runDescriptor, endpointCustomizationConfiguration, configuration =>
            {
                GatewayTestSuiteConstraints.Current.ConfigureDeduplicationStorage(
                    endpointCustomizationConfiguration.CustomEndpointName, 
                    configuration, 
                    runDescriptor.Settings)
                    .GetAwaiter().GetResult();

                runDescriptor.OnTestCompleted(_ => GatewayTestSuiteConstraints.Current.Cleanup());

                configurationBuilderCustomization(configuration);
            });
        }
    }
}