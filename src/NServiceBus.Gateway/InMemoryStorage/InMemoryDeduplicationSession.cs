namespace NServiceBus.Gateway
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    class InMemoryDeduplicationSession : IDuplicationCheckSession
    {
        public InMemoryDeduplicationSession(string messageId, Dictionary<string, LinkedListNode<string>> clientIdSet, LinkedList<string> clientIdList, int cacheSize)
        {
            this.messageId = messageId;
            this.clientIdSet = clientIdSet;
            this.clientIdList = clientIdList;
            this.cacheSize = cacheSize;
        }

        public bool IsDuplicate
        {
            get
            {
                lock (lockObj)
                {
                    return clientIdSet.ContainsKey(messageId);
                }
            }
        }

        public Task MarkAsDispatched()
        {
            lock (lockObj)
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
            }

            return Task.FromResult(0);
        }

        public void Dispose()
        {
        }

        readonly string messageId;
        readonly Dictionary<string, LinkedListNode<string>> clientIdSet;
        readonly LinkedList<string> clientIdList;
        readonly int cacheSize;

        static readonly object lockObj = new object();
    }
}