namespace NServiceBus.Gateway.PersistenceTests
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using Extensibility;
    using NUnit.Framework;

    class When_supporting_distributed_transactions : NServiceBusGatewayDeduplicationStorageTest
    {
        [SetUp]
        public void IgnoreNonDTCStorages()
        {
            if (!storage.SupportsDistributedTransactions)
            {
                Assert.Ignore();
            }
        }

        [Test]
        public async Task Only_marks_messages_as_dispatched_when_committed()
        {
            var messageId = Guid.NewGuid().ToString("D");

            using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
            {
                var context = new ContextBag();
                Assert.IsFalse(await storage.IsDuplicate(messageId, context));

                await storage.MarkAsDispatched(messageId, context);

                using (var concurrentScope = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
                {
                    Assert.IsFalse(await storage.IsDuplicate(messageId, context), "concurrent readers should not see uncommitted data");
                    concurrentScope.Complete();
                }

                scope.Complete();
            }

            Assert.IsTrue(await storage.IsDuplicate(messageId, new ContextBag()));
        }

        [Test]
        public async Task Does_not_mark_messages_as_dispatched_when_aborted()
        {
            var messageId = Guid.NewGuid().ToString("D");

            using (var _ = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
            {
                var context = new ContextBag();
                Assert.IsFalse(await storage.IsDuplicate(messageId, context));

                await storage.MarkAsDispatched(messageId, context);
            }

            Assert.IsFalse(await storage.IsDuplicate(messageId, new ContextBag()));
        }

        [Test]
        public async Task Throws_exception_when_message_concurrently_marked_as_dispatched()
        {
            var messageId = Guid.NewGuid().ToString("D");

            using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
            {
                var context = new ContextBag();
                Assert.IsFalse(await storage.IsDuplicate(messageId, context));

                await storage.MarkAsDispatched(messageId, context);

                using (var concurrentScope = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
                {
                    await storage.MarkAsDispatched(messageId, new ContextBag());
                    concurrentScope.Complete();
                }

                scope.Complete();
            }
        }
    }
}