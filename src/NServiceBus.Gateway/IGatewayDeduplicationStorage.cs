namespace NServiceBus.Gateway
{
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
        Task<IDeduplicationSession> CheckForDuplicate(string messageId, ContextBag context);
    }
}
