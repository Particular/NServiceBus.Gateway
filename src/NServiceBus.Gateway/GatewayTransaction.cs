namespace NServiceBus.Gateway
{
    using System;
    using System.Transactions;

    class GatewayTransaction
    {
        static TimeSpan Timeout(TimeSpan defaultTimeout)
        {
            if (ConfiguredTimeout.HasValue && ConfiguredTimeout > defaultTimeout)
            {
                return ConfiguredTimeout.Value;
            }

            return defaultTimeout;
        }

        public static TimeSpan? ConfiguredTimeout { private get; set; }

        public static TransactionScope Scope()
        {
            return new TransactionScope(TransactionScopeOption.RequiresNew,
                new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.ReadCommitted,
                    Timeout = Timeout(TimeSpan.FromSeconds(30)),
                }, TransactionScopeAsyncFlowOption.Enabled
                );
        }
    }
}
