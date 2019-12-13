namespace NServiceBus.Gateway.PersistenceTests
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    public class When_checking_unknown_message : NServiceBusGatewayDeduplicationStorageTest
    {
        [Test]
        public async Task IsDuplicate_returns_false()
        {
            using (var session = await storage.CheckForDuplicate(Guid.NewGuid().ToString(), new ContextBag()))
            {
                Assert.IsFalse(session.IsDuplicate);
            }
        }

        [Test]
        public async Task IsDuplicate_returns_false_when_checking_multiple_times()
        {
            var messageId = Guid.NewGuid().ToString();

            using (var session = await storage.CheckForDuplicate(messageId, new ContextBag()))
            {
                Assert.IsFalse(session.IsDuplicate);
            }

            using (var session = await storage.CheckForDuplicate(messageId, new ContextBag()))
            {
                Assert.IsFalse(session.IsDuplicate);
            }
        }
    }
}