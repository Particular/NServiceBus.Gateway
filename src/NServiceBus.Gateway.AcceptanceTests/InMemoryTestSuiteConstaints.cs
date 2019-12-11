namespace NServiceBus.Gateway.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;
    using Configuration.AdvancedExtensibility;

    //we don't ship this class in the source package since downstreams should provide their own storage config.
    public partial class GatewayTestSuiteConstraints
    {
        public Task ConfigureDeduplicationStorage(string endpointName, EndpointConfiguration configuration, RunSettings settings)
        {
            var inMemoryDeduplicationConfiguration = new InMemoryDeduplicationConfiguration();
            var gatewaySettings = configuration.Gateway(inMemoryDeduplicationConfiguration);
            configuration.GetSettings().Set(gatewaySettings);
            return Task.FromResult(0);
        }

        public Task Cleanup()
        {
            return Task.FromResult(0);
        }
    }
}