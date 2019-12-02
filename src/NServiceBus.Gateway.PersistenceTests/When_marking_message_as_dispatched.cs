namespace NServiceBus.Gateway.PersistenceTests
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    [TestFixture]
    class When_marking_message_as_dispatched : NServiceBusGatewayDeduplicationStorageTest
    {
        [Test]
        public async Task IsDuplicate_returns_true()
        {
            var messageId = Guid.NewGuid().ToString("N");

            await storage.MarkAsDispatched(messageId, new ContextBag());
            var isDuplicate = await storage.IsDuplicate(messageId, new ContextBag());

            Assert.IsTrue(isDuplicate);
        }

        [Test]
        public async Task MarkAsDispatched_can_be_called_multiple_times()
        {
            var messageId = Guid.NewGuid().ToString("N");

            await storage.MarkAsDispatched(messageId, new ContextBag());
            await storage.MarkAsDispatched(messageId, new ContextBag());

            Assert.IsTrue(await storage.IsDuplicate(messageId, new ContextBag()));
        }
    }
}