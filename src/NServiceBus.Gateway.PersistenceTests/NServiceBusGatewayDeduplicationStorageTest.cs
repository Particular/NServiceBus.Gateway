namespace NServiceBus.Gateway.PersistenceTests
{
    using NUnit.Framework;

    [TestFixture]
    abstract class NServiceBusGatewayDeduplicationStorageTest
    {
        protected IGatewayDeduplicationStorage storage;

        [SetUp]
        public void SetUp()
        {
            storage = GatewayPersistenceTestsConfiguration.Current.CreateStorage();
        }
    }
}