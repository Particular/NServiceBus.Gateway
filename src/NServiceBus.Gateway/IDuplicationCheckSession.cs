namespace NServiceBus.Gateway
{
    using System;
    using System.Threading.Tasks;

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
        /// Marks the message as successfully dispatched. Marking a message as dispatched will consider it a duplicate when invoking <see cref="IGatewayDeduplicationStorage.CheckForDuplicate"/>.
        /// </summary>
        Task MarkAsDispatched();
    }
}