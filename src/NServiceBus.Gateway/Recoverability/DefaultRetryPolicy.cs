namespace NServiceBus.Gateway
{
    using NServiceBus.Transports;
    using System;

    class DefaultRetryPolicy
    {
        public static Func<IncomingMessage, Exception, int, TimeSpan> BuildWithDefaults()
        {
            return Build(4, TimeSpan.FromSeconds(60));
        }

        public static Func<IncomingMessage, Exception, int, TimeSpan> Build(int numberOfRetries, TimeSpan timeIncrease)
        {
            return (message, exception, currentRetry) => currentRetry > numberOfRetries
                ? TimeSpan.MinValue
                : TimeSpan.FromTicks(currentRetry * timeIncrease.Ticks);
        }
    }
}
