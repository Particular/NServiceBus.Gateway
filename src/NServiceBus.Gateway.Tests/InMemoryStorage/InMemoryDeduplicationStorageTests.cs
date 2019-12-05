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

            using (var s1 = await storage.IsDuplicate("A", new ContextBag()))
            {
                await s1.MarkAsDispatched();
            }
            using (var s1 = await storage.IsDuplicate("A", new ContextBag()))
            {
                Assert.IsTrue(s1.IsDuplicate);
            }

            using (var s1 = await storage.IsDuplicate("B", new ContextBag()))
            {
                await s1.MarkAsDispatched();
            }
            using (var s1 = await storage.IsDuplicate("B", new ContextBag()))
            {
                Assert.IsTrue(s1.IsDuplicate);
            }

            using (var s1 = await storage.IsDuplicate("C", new ContextBag()))
            {
                await s1.MarkAsDispatched();
            }
            using (var s1 = await storage.IsDuplicate("C", new ContextBag()))
            {
                Assert.IsTrue(s1.IsDuplicate);
            }

            using (var s1 = await storage.IsDuplicate("A", new ContextBag()))
            {
                Assert.IsFalse(s1.IsDuplicate);
            }
        }
    }
}