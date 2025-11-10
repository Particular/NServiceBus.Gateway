namespace NServiceBus.Gateway.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;

    //we don't ship this class in the source package since downstreams should provide their own storage config.
    public partial class GatewayTestSuiteConstraints
    {
        public GatewaySettings ConfigureGateway(string endpointName, EndpointConfiguration configuration, RunSettings settings) => configuration.Gateway(new NonDurableDeduplicationConfiguration());

        public Task Cleanup() => Task.CompletedTask;
    }
}