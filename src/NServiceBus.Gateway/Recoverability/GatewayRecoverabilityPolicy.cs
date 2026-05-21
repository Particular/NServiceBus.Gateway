namespace NServiceBus.Gateway
{
    using System;
    using Transport;

    static class GatewayRecoverabilityPolicy
    {
        public static RecoverabilityAction Invoke(ErrorContext errorContext, Func<IncomingMessage, Exception, int, TimeSpan> retryPolicy, RecoverabilityConfig config)
        {
            var currentRetry = errorContext.DelayedDeliveriesPerformed + 1;
            // Don't want to change the public Gateway API - just reconstitute the IncomingMessage from the errorContext
            var recreatedMessage = new IncomingMessage(errorContext.NativeMessageId, errorContext.Headers, errorContext.Body, errorContext.ReceiveProperties);
            var timeIncrease = retryPolicy(recreatedMessage, errorContext.Exception, currentRetry);
            if (timeIncrease <= TimeSpan.MinValue)
            {
                return RecoverabilityAction.MoveToError(config.Failed.ErrorQueue);
            }
            return RecoverabilityAction.DelayedRetry(timeIncrease);
        }
    }
}