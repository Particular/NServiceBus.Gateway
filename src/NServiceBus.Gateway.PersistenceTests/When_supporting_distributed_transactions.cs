namespace NServiceBus.Gateway.PersistenceTests
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using Extensibility;
    using NUnit.Framework;

    public class When_supporting_distributed_transactions : NServiceBusGatewayDeduplicationStorageTest
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
                using (var session = await storage.CheckForDuplicate(messageId, new ContextBag()))
                {
                    Assert.IsFalse(session.IsDuplicate);
                    await session.MarkAsDispatched();
                }

                using (var concurrentScope = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
                {
                    using (var session = await storage.CheckForDuplicate(messageId, new ContextBag()))
                    {
                        Assert.IsFalse(session.IsDuplicate, "concurrent readers should not see uncommitted data");
                        await session.MarkAsDispatched();
                    }

                    concurrentScope.Complete();
                }

                scope.Complete();
            }

            using (var session = await storage.CheckForDuplicate(messageId, new ContextBag()))
            {
                Assert.IsTrue(session.IsDuplicate);
            }
        }

        [Test]
        public async Task Does_not_mark_messages_as_dispatched_when_rolled_back()
        {
            var messageId = Guid.NewGuid().ToString("D");

            using (var _ = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
            {
                using (var session = await storage.CheckForDuplicate(messageId, new ContextBag()))
                {
                    Assert.IsFalse(session.IsDuplicate);
                    await session.MarkAsDispatched();
                }

                // no commit
            }

            using (var session = await storage.CheckForDuplicate(messageId, new ContextBag()))
            {
                Assert.IsFalse(session.IsDuplicate);
            }
        }

        [Test]
        public async Task Throws_exception_when_message_concurrently_marked_as_dispatched()
        {
            var messageId = Guid.NewGuid().ToString("D");
            Exception exception = null;

            using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
            {
                using (var session = await storage.CheckForDuplicate(messageId, new ContextBag()))
                {
                    Assert.IsFalse(session.IsDuplicate);
                    await session.MarkAsDispatched();
                }

                using (var concurrentScope = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
                {
                    using (var session = await storage.CheckForDuplicate(messageId, new ContextBag()))
                    {
                        Assert.IsFalse(session.IsDuplicate);
                        await session.MarkAsDispatched();
                    }
                    concurrentScope.Complete();
                }

                try
                {
                    scope.Complete();
                }
                catch (Exception e)
                {
                    exception = e;
                }
            }

            Assert.NotNull(exception);
        }
    }
}