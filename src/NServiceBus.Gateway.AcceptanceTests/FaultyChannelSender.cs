namespace NServiceBus.AcceptanceTests.Gateway
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.Gateway;

    class FaultyChannelSender<TContext> : IChannelSender where TContext : ICountNumberOfRetries
    {
        public FaultyChannelSender(TContext testContext)
        {
            this.testContext = testContext;
        }

        public Task Send(string remoteAddress, IDictionary<string, string> headers, Stream data)
        {
            if (headers.ContainsKey(FullRetriesHeaderKey))
            {
                testContext.NumberOfRetries = Int32.Parse(headers[FullRetriesHeaderKey]);
            }
            throw new SimulatedException($"Simulated error when sending to site at {remoteAddress}");
        }

        TContext testContext;

        static readonly string FullRetriesHeaderKey = "NServiceBus.Header.NServiceBus.Retries";
    }
}