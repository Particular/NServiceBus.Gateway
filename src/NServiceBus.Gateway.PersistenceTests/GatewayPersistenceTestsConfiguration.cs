using NServiceBus.Gateway;

interface IGatewayPersistenceTestsConfiguration
{
    IGatewayDeduplicationStorage CreateStorage();
}

partial class GatewayPersistenceTestsConfiguration : IGatewayPersistenceTestsConfiguration
{
    public static GatewayPersistenceTestsConfiguration Current { get; } = new GatewayPersistenceTestsConfiguration();

    //TODO remove
    public IGatewayDeduplicationStorage CreateStorage()
    {
        throw new System.NotImplementedException();
    }
}