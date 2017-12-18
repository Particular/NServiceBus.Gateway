namespace NServiceBus.Gateway.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;

    public class InMemoryPersistenceConfiguration : IConfigureGatewayPersitenceExecution
    {
        public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings)
        {
            configuration.UsePersistence<InMemoryPersistence, StorageType.GatewayDeduplication>();

            return Task.FromResult(0);
        }

        public Task Cleanup()
        {
            //no-op
            return Task.FromResult(0);
        }
    }
}