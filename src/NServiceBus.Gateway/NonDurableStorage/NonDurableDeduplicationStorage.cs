﻿namespace NServiceBus.Gateway
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;

    class NonDurableDeduplicationStorage : IGatewayDeduplicationStorage
    {
        public NonDurableDeduplicationStorage(int cacheSize)
        {
            this.cacheSize = cacheSize;
        }

        public bool SupportsDistributedTransactions { get; } = false;

        public Task<IDeduplicationSession> CheckForDuplicate(string messageId, ContextBag context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IDeduplicationSession>(
                new NonDurableDeduplicationSession(messageId, clientIdSet, clientIdList, cacheSize));
        }

        readonly int cacheSize;
        readonly LinkedList<string> clientIdList = new LinkedList<string>();
        readonly Dictionary<string, LinkedListNode<string>> clientIdSet = [];
    }
}