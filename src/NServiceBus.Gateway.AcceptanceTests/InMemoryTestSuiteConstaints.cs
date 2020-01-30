namespace NServiceBus.Gateway.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;

    //we don't ship this class in the source package since downstreams should provide their own storage config.
    public partial class GatewayTestSuiteConstraints
    {
        public Task<GatewayDeduplicationConfiguration> ConfigureDeduplicationStorage(string endpointName, EndpointConfiguration configuration, RunSettings settings)
        {
            return Task.FromResult<GatewayDeduplicationConfiguration>(new InMemoryDeduplicationConfiguration());
        }

        public Task Cleanup()
        {
            return Task.FromResult(0);
        }
    }
}