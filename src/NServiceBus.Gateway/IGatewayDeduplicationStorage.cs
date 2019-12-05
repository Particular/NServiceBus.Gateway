namespace NServiceBus.Gateway
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using Extensibility;

    /// <summary>
    /// Defines the api to provide storage for the gateway deduplication mechanism.
    /// </summary>
    public interface IGatewayDeduplicationStorage
    {
        /// <summary>
        /// Returns whether the storage can enlist in a distributed transaction via ambient <see cref="TransactionScope"/> to support exactly-once dispatching.
        /// </summary>
        bool SupportsDistributedTransactions { get; }

        /// <summary>
        /// Returns if the message is a duplicate.
        /// </summary>
        /// <returns>
        /// <code>true</code> if the message has been received successfully before and is considered a duplicate. <code>false</code> otherwise.
        /// </returns>
        Task<IDuplicationCheckSession> IsDuplicate(string messageId, ContextBag context);
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IDuplicationCheckSession : IDisposable
    {
        /// <summary>
        /// Returns if the message is a duplicate.
        /// </summary>
        bool IsDuplicate { get; }

        /// <summary>
        /// Marks the message as successfully dispatched. Marking a message as dispatched will consider it a duplicate when invoking <see cref="IGatewayDeduplicationStorage.IsDuplicate"/>.
        /// </summary>
        Task MarkAsDispatched();
    }
}
