namespace NServiceBus
{
    using System;
    using Configuration.AdvanceExtensibility;
    using Gateway;
    using Transports;

    /// <summary>
    /// Placeholder for the various settings and extension points related to gateway.
    /// </summary>
    public class GatewaySettings
    {
        EndpointConfiguration config;

        internal GatewaySettings(EndpointConfiguration config)
        {
            this.config = config;
        }

        /// <summary>
        /// Register custom factories for creating channel receivers and channel senders. This allows for overriding the default Http implementation.
        /// </summary>
        /// <param name="senderFactory">The sender factory to use. The factory takes a string with the channel type as parameter.</param>
        /// <param name="receiverFactory">The receiver factory to use. The factory takes a string with the channel type as parameter.</param>
        public void ChannelFactories(Func<string, IChannelSender> senderFactory, Func<string, IChannelReceiver> receiverFactory)
        {
            if (senderFactory == null)
            {
                throw new ArgumentNullException(nameof(senderFactory));
            }

            if (receiverFactory == null)
            {
                throw new ArgumentNullException(nameof(receiverFactory));
            }

            config.GetSettings().Set("GatewayChannelSenderFactory", senderFactory);
            config.GetSettings().Set("GatewayChannelReceiverFactory", receiverFactory);
        }


        /// <summary>
        /// Set the number of retries and time increase between them for messages failing to be sent through the gateway.
        /// </summary>
        /// <param name="numberOfRetries">The total number of retries to do. 0 means no retry.</param>
        /// <param name="timeIncrease">The time to wait between each retry.</param>
        public void Retries(int numberOfRetries, TimeSpan timeIncrease)
        {
            if (numberOfRetries < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numberOfRetries), numberOfRetries, $"{nameof(numberOfRetries)} must be non-negative");
            }

            if (timeIncrease < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timeIncrease), timeIncrease, $"{nameof(timeIncrease)} must be non-negative");
            }

            SetDefaultRetryPolicySettings(numberOfRetries, timeIncrease);
        }

        /// <summary>
        /// Set a retry policy that returns a TimeSpan to delay between attempts based on the number of retries attempted. Return <see cref="TimeSpan.Zero" /> to abort retries.
        /// </summary>
        /// <param name="customRetryPolicy">The custom retry policy to use.</param>
        public void CustomRetryPolicy(Func<IncomingMessage, Exception, int, TimeSpan> customRetryPolicy)
        {
            config.GetSettings().Set("Gateway.Retries.RetryPolicy", customRetryPolicy);
        }

        /// <summary>
        /// Failed messages will not be retried and will be sent directly to the configured error queue.
        /// </summary>
        public void DisableRetries()
        {
            SetDefaultRetryPolicySettings(0, TimeSpan.MinValue);
        }

        void SetDefaultRetryPolicySettings(int numberOfRetries, TimeSpan timeIncrease)
        {
            config.GetSettings().Set("Gateway.Retries.RetryPolicy", DefaultRetryPolicy.Build(numberOfRetries, timeIncrease));
        }
    }
}