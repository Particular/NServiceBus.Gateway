namespace NServiceBus.Gateway.Tests.NonDurableStorage
{
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    [TestFixture]
    class NonDurableDeduplicationStorageTests
    {
        [Test]
        public async Task Should_remove_oldest_entries_when_LRU_reaches_limit()
        {
            var storage = new NonDurableDeduplicationStorage(2);

            using (var s1 = await storage.CheckForDuplicate("A", new ContextBag()))
            {
                await s1.MarkAsDispatched();
            }
            using (var s1 = await storage.CheckForDuplicate("A", new ContextBag()))
            {
                Assert.IsTrue(s1.IsDuplicate);
            }

            using (var s1 = await storage.CheckForDuplicate("B", new ContextBag()))
            {
                await s1.MarkAsDispatched();
            }
            using (var s1 = await storage.CheckForDuplicate("B", new ContextBag()))
            {
                Assert.IsTrue(s1.IsDuplicate);
            }

            using (var s1 = await storage.CheckForDuplicate("C", new ContextBag()))
            {
                await s1.MarkAsDispatched();
            }
            using (var s1 = await storage.CheckForDuplicate("C", new ContextBag()))
            {
                Assert.IsTrue(s1.IsDuplicate);
            }

            using (var s1 = await storage.CheckForDuplicate("A", new ContextBag()))
            {
                Assert.IsFalse(s1.IsDuplicate);
            }
        }
    }
}