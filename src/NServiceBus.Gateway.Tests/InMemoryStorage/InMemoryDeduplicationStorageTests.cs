namespace NServiceBus.Gateway.Tests.InMemoryStorage
{
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    [TestFixture]
    class InMemoryDeduplicationStorageTests
    {
        [Test]
        public async Task Should_remove_oldest_entries_when_LRU_reaches_limit()
        {
            var storage = new InMemoryDeduplicationStorage(2);

            await storage.MarkAsDispatched("A", new ContextBag());
            Assert.True(await storage.IsDuplicate("A", new ContextBag()));

            await storage.MarkAsDispatched("B", new ContextBag());
            Assert.True(await storage.IsDuplicate("A", new ContextBag()));

            await storage.MarkAsDispatched("C", new ContextBag());
            Assert.True(await storage.IsDuplicate("B", new ContextBag()));
            Assert.True(await storage.IsDuplicate("C", new ContextBag()));
            Assert.False(await storage.IsDuplicate("A", new ContextBag()));
        }
    }
}