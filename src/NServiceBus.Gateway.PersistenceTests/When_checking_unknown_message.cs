namespace NServiceBus.Gateway.PersistenceTests
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    class When_checking_unknown_message : NServiceBusGatewayDeduplicationStorageTest
    {
        [Test]
        public async Task IsDuplicate_returns_false()
        {
            var isDuplicate = await storage.IsDuplicate(Guid.NewGuid().ToString("N"), new ContextBag());

            Assert.IsFalse(isDuplicate);
        }

        [Test]
        public async Task IsDuplicate_returns_false_when_checking_multiple_times()
        {
            var messageId = Guid.NewGuid().ToString("N");

            Assert.IsFalse(await storage.IsDuplicate(messageId, new ContextBag()));
            Assert.IsFalse(await storage.IsDuplicate(messageId, new ContextBag()));
            Assert.IsFalse(await storage.IsDuplicate(messageId, new ContextBag()));
        }
    }
}