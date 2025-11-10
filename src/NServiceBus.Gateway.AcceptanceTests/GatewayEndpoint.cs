namespace NServiceBus.Gateway.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;
    using Configuration.AdvancedExtensibility;

    public class GatewayEndpoint : GatewayEndpointWithNoStorage
    {
        public override Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointCustomizationConfiguration, Func<EndpointConfiguration, Task> configurationBuilderCustomization)
        {
            return base.GetConfiguration(runDescriptor, endpointCustomizationConfiguration, async configuration =>
            {
                var endpointName = endpointCustomizationConfiguration.CustomEndpointName ?? configuration.GetSettings().EndpointName();

                var constraints = new GatewayTestSuiteConstraints();

                var gatewaySettings = constraints.ConfigureGateway(
                    endpointName,
                    configuration,
                    runDescriptor.Settings);

                // This is needed to enable the tests to access and modify gateway settings
                configuration.GetSettings().Set(gatewaySettings);

                runDescriptor.OnTestCompleted(_ => constraints.Cleanup());

                await configurationBuilderCustomization(configuration);
            });
        }
    }
}