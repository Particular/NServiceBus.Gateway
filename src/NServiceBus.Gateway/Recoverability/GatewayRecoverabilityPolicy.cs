namespace NServiceBus.Gateway
{
    using System;
    using NServiceBus.Transport;

    static class GatewayRecoverabilityPolicy
    {
        public static RecoverabilityAction Invoke(ErrorContext errorContext, Func<IncomingMessage, Exception, int, TimeSpan> retryPolicy, RecoverabilityConfig config)
        {
            var currentRetry = errorContext.DelayedDeliveriesPerformed + 1;
            var timeIncrease = retryPolicy(errorContext.Message, errorContext.Exception, currentRetry);
            if (timeIncrease <= TimeSpan.MinValue)
            {
                return RecoverabilityAction.MoveToError(config.Failed.ErrorQueue);
            }
            return RecoverabilityAction.DelayedRetry(timeIncrease);
        }
    }
}