namespace NServiceBus.Gateway
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;

    class InMemoryDeduplicationStorage : IGatewayDeduplicationStorage
    {
        readonly int cacheSize;
        readonly LinkedList<string> clientIdList = new LinkedList<string>();
        readonly Dictionary<string, LinkedListNode<string>> clientIdSet = new Dictionary<string, LinkedListNode<string>>();

        public InMemoryDeduplicationStorage(int cacheSize)
        {
            this.cacheSize = cacheSize;
        }

        public bool SupportsDistributedTransactions { get; } = false;

        public Task<bool> IsDuplicate(string messageId, ContextBag context)
        {
            lock (clientIdSet)
            {
                return Task.FromResult(clientIdSet.ContainsKey(messageId));
            }
        }

        public Task MarkAsDispatched(string messageId, ContextBag context)
        {
            lock (clientIdSet)
            {
                if (clientIdSet.TryGetValue(messageId, out var existingNode)) // O(1)
                {
                    // "refresh" lifetime by moving id to the back of the linked list
                    clientIdList.Remove(existingNode); // O(1) operation, because we got the node reference
                    clientIdList.AddLast(existingNode); // O(1) operation
                }
                else
                {
                    if (clientIdSet.Count == cacheSize)
                    {
                        var id = clientIdList.First.Value;
                        clientIdSet.Remove(id); // O(1)
                        clientIdList.RemoveFirst(); // O(1)
                    }

                    var node = clientIdList.AddLast(messageId); // O(1)
                    clientIdSet.Add(messageId, node); // O(1)
                }

                return Task.FromResult(0);
            }
        }
    }   
}