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
            var messageId = Guid.NewGuid().ToString();

            using (var session = await storage.CheckForDuplicate(messageId, new ContextBag()))
            {
                await session.MarkAsDispatched();
            }

            using (var session = await storage.CheckForDuplicate(messageId, new ContextBag()))
            {
                Assert.IsTrue(session.IsDuplicate);
            }
        }

        [Test]
        public async Task MarkAsDispatched_can_be_called_multiple_times()
        {
            var messageId = Guid.NewGuid().ToString();

            using (var session = await storage.CheckForDuplicate(messageId, new ContextBag()))
            {
                await session.MarkAsDispatched();
            }

            using (var session = await storage.CheckForDuplicate(messageId, new ContextBag()))
            {
                await session.MarkAsDispatched();
            }
        }
    }
}