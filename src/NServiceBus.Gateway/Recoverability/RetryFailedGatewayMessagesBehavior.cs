namespace NServiceBus.Gateway
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Logging;
    using NServiceBus.Transports;

    class RetryFailedGatewayMessagesBehavior
    {
        public RetryFailedGatewayMessagesBehavior(Func<IncomingMessage, Exception, int, TimeSpan> retryPolicy)
        {
            this.retryPolicy = retryPolicy;
        }

        public async Task Invoke(PushContext context, Func<Task> next)
        {
            try
            {
                await next().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                var sentForRetry = await SendForRetry(context, exception).ConfigureAwait(false);
                if (!sentForRetry)
                {
                    throw;
                }
            }
        }

        Task<bool> SendForRetry(PushContext context, Exception exception)
        {
            var message = new IncomingMessage(context.MessageId, context.Headers, context.BodyStream);

            var retryCount = GetRetryCount(message.Headers);

            var currentRetry = retryCount + 1;

            var timeIncrease = retryPolicy(message, exception, currentRetry);

            if (timeIncrease <= TimeSpan.MinValue)
            {
                return Task.FromResult(false);
            }

            Logger.Warn($"Gateway message '{message.MessageId}' failed. Will reschedule message after {timeIncrease}:", exception);

            var messageToRetry = new OutgoingMessage(message.MessageId, message.Headers, message.Body);

            messageToRetry.Headers[Headers.Retries] = currentRetry.ToString();
            messageToRetry.Headers[Headers.RetriesTimestamp] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);

            //var dispatchContext = this.CreateRoutingContext(messageToRetry, localAddress, context);

            //dispatchContext.Extensions.Set(new List<DeliveryConstraint>
            //{
            //    new DelayDeliveryWith(timeIncrease)
            //});

            //await fork(dispatchContext).ConfigureAwait(false);

            //todo
            return Task.FromResult(true);
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

        Func<IncomingMessage, Exception, int, TimeSpan> retryPolicy;

        static ILog Logger = LogManager.GetLogger<RetryFailedGatewayMessagesBehavior>();
    }
}