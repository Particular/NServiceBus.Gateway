namespace NServiceBus.Gateway.AcceptanceTests
{
    //we don't ship this class in the package
    public partial class GatewayTestSuiteConstraints
    {
        //we don't ship this class in the package
        public IConfigureGatewayPersitenceExecution CreatePersistenceConfiguration()
        {
            return new InMemoryPersistenceConfiguration();
        }
    }
}