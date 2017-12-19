namespace NServiceBus.Gateway.AcceptanceTests
{
    //we don't ship this class in the source package since downstreams should provide their own storage config.
    public partial class GatewayTestSuiteConstraints
    {
        public IConfigureGatewayPersitenceExecution CreatePersistenceConfiguration()
        {
            return new InMemoryPersistenceConfiguration();
        }
    }
}