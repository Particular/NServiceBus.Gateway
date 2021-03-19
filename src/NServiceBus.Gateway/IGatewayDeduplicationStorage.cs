namespace NServiceBus.Gateway
{
    using System.Threading;
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
        /// Returns a session that provides duplicate detection for the given message id.
        /// </summary>
        /// <returns>
        /// A <see cref="IDeduplicationSession"/>
        /// </returns>
        Task<IDeduplicationSession> CheckForDuplicate(string messageId, ContextBag context, CancellationToken cancellationToken = default);
    }
}
