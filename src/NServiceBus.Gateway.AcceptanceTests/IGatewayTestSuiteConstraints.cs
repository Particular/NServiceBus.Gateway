namespace NServiceBus.Gateway.AcceptanceTests
{
    public interface IGatewayTestSuiteConstraints
    {
        IConfigureGatewayPersitenceExecution CreatePersistenceConfiguration();
    }
}