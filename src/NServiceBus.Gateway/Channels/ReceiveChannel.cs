namespace NServiceBus.Gateway
{
    using Channels;

    /// <summary>
    /// Represent a channel that the Gateway is listening on.
    /// </summary>
    public class ReceiveChannel : Channel
    {
        /// <summary>
        /// Maximum concurrency for this channel.
        /// </summary>
        public int MaxConcurrency { get; set; } = 1;

        /// <summary>
        /// Defines if the this channel should be the default if not specified when sending messages.
        /// </summary>
        public bool Default { get; set; }

        /// <summary>
        /// <inheritdoc cref="ToString"/>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{base.ToString()}MaxConcurrency={MaxConcurrency}Default={Default}";
        }
    }
}