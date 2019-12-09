namespace NServiceBus.Gateway
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;

    class InMemoryDeduplicationStorage : IGatewayDeduplicationStorage
    {
        public InMemoryDeduplicationStorage(int cacheSize)
        {
            this.cacheSize = cacheSize;
        }

        public bool SupportsDistributedTransactions { get; } = false;

        public Task<IDuplicationCheckSession> CheckForDuplicate(string messageId, ContextBag context)
        {
            Monitor.Enter(lockObj);
            return Task.FromResult<IDuplicationCheckSession>(new InMemoryDeduplicationSession(messageId, clientIdSet, clientIdList, lockObj, cacheSize));
        }

        readonly int cacheSize;
        readonly LinkedList<string> clientIdList = new LinkedList<string>();
        readonly Dictionary<string, LinkedListNode<string>> clientIdSet = new Dictionary<string, LinkedListNode<string>>();
        readonly object lockObj = new object();
    }
}