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
                var deduplicationConfiguration = constraints.ConfigureDeduplicationStorage(
                    endpointName,
                    configuration,
                    runDescriptor.Settings)
                    .GetAwaiter().GetResult();

                var gatewaySettings = configuration.Gateway(deduplicationConfiguration);

                configuration.GetSettings().Set(gatewaySettings);

                runDescriptor.OnTestCompleted(_ => constraints.Cleanup());

                await configurationBuilderCustomization(configuration);
            });
        }
    }
}