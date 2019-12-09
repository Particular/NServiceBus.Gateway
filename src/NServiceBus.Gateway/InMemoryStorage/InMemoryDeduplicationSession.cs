﻿namespace NServiceBus.Gateway
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    class InMemoryDeduplicationSession : IDuplicationCheckSession
    {
        public InMemoryDeduplicationSession(string messageId, Dictionary<string, LinkedListNode<string>> clientIdSet, LinkedList<string> clientIdList, object lockObj, int cacheSize)
        {
            this.messageId = messageId;
            this.clientIdSet = clientIdSet;
            this.clientIdList = clientIdList;
            this.lockObj = lockObj;
            this.cacheSize = cacheSize;
        }

        public bool IsDuplicate => clientIdSet.ContainsKey(messageId);

        public Task MarkAsDispatched()
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

        public void Dispose()
        {
            Monitor.Exit(lockObj);
            GC.SuppressFinalize(this);
        }

        readonly string messageId;
        readonly Dictionary<string, LinkedListNode<string>> clientIdSet;
        readonly LinkedList<string> clientIdList;
        readonly object lockObj;
        readonly int cacheSize;
    }
}