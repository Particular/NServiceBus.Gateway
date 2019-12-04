using NServiceBus.Gateway;

interface IGatewayPersistenceTestsConfiguration
{
    IGatewayDeduplicationStorage CreateStorage();
}

/// <summary>
/// Consumers of this package have to implement the <see cref="IGatewayPersistenceTestsConfiguration"/> interface in the local partial class.
/// </summary>
partial class GatewayPersistenceTestsConfiguration : IGatewayPersistenceTestsConfiguration
{
    public static GatewayPersistenceTestsConfiguration Current { get; } = new GatewayPersistenceTestsConfiguration();
}