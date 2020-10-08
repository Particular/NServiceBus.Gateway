using NServiceBus.Gateway;

partial class GatewayPersistenceTestsConfiguration
{
    public IGatewayDeduplicationStorage CreateStorage()
    {
        return new NonDurableDeduplicationStorage(100);
    }
}