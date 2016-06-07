namespace NServiceBus.Gateway
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    class RetryFailedGatewayMessagesBehavior : ForkConnector<ITransportReceiveContext, IRoutingContext>
    {
        public RetryFailedGatewayMessagesBehavior(string localAddress, Func<IncomingMessage, Exception, int, TimeSpan> retryPolicy)
        {
            this.localAddress = localAddress;
            this.retryPolicy = retryPolicy;
        }

        public override async Task Invoke(PushContext context, Func<Task> next, Func<IRoutingContext, Task> fork)
        {
            try
            {
                await next().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                var sentForRetry = await SendForRetry(context, exception, fork).ConfigureAwait(false);
                if (!sentForRetry)
                {
                    throw;
                }
            }
        }

        async Task<bool> SendForRetry(PushContext context, Exception exception, Func<IRoutingContext, Task> fork)
        {
            var message = new IncomingMessage(context.MessageId, context.Headers, context.BodyStream);

            var retryCount = GetRetryCount(message.Headers);

            var currentRetry = retryCount + 1;

            var timeIncrease = retryPolicy(message, exception, currentRetry);

            if (timeIncrease <= TimeSpan.MinValue)
            {
                return false;
            }

            Logger.Warn($"Gateway message '{message.MessageId}' failed. Will reschedule message after {timeIncrease}:", exception);

            var messageToRetry = new OutgoingMessage(message.MessageId, message.Headers, message.Body);

            messageToRetry.Headers[Headers.Retries] = currentRetry.ToString();
            messageToRetry.Headers[Headers.RetriesTimestamp] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);

            var dispatchContext = this.CreateRoutingContext(messageToRetry, localAddress, context);

            dispatchContext.Extensions.Set(new List<DeliveryConstraint>
            {
                new DelayDeliveryWith(timeIncrease)
            });

            await fork(dispatchContext).ConfigureAwait(false);

            return true;
        }

        static int GetRetryCount(Dictionary<string,string> headers)
        {
            string value;
            if (headers.TryGetValue(Headers.Retries, out value))
            {
                int i;
                if (int.TryParse(value, out i))
                {
                    return i;
                }
            }
            return 0;
        }

        string localAddress;
        Func<IncomingMessage, Exception, int, TimeSpan> retryPolicy;

        static ILog Logger = LogManager.GetLogger<RetryFailedGatewayMessagesBehavior>();

        public class Registration : RegisterStep
        {
            public Registration(string localAddress, Func<IncomingMessage, Exception, int, TimeSpan> retryPolicy)
                : base("RetryFailedGatewayMessages", typeof(RetryFailedGatewayMessagesBehavior), "Retries failed gateway messages by forwarding them to the timeout manager",
                    builder => new RetryFailedGatewayMessagesBehavior(localAddress, retryPolicy))
            {
                InsertAfter(ForwardFailedGatewayMessagesToErrorQueueBehavior.StepId);
            }
        }
    }
}