namespace NServiceBus.Gateway.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using Transport;
    using NUnit.Framework;
    using Configuration.AdvancedExtensibility;

    [TestFixture]
    class When_configuring_the_default_retry_policy_with_a_single_retry
    {
        [SetUp]
        public void Setup()
        {
            FailingMessage = new IncomingMessage("failing-id", new Dictionary<string, string>(), new byte[0]);
            Exception = new Exception("exception-message");
            TimeIncrease = TimeSpan.FromSeconds(1);
            NumberOfRetries = 1;

            var config = new EndpointConfiguration("fake-endpoint");
            config.Gateway(new NonDurableDeduplicationConfiguration()).Retries(NumberOfRetries, TimeIncrease);

            RetryPolicy = config.GetSettings().Get<Func<IncomingMessage, Exception, int, TimeSpan>>("Gateway.Retries.RetryPolicy");
        }

        [Test]
        public void Should_schedule_first_retry()
        {
            var delay = RetryPolicy.Invoke(FailingMessage, Exception, 1);

            Assert.AreEqual(TimeIncrease, delay, "First retry should be attempted after a single timeIncrease interval");
        }

        [Test]
        public void Should_prevent_a_second_retry()
        {
            var delay = RetryPolicy.Invoke(FailingMessage, Exception, 2);

            Assert.AreEqual(TimeSpan.MinValue, delay, "Second Retry should be prevented");
        }

        IncomingMessage FailingMessage;
        Exception Exception;
        TimeSpan TimeIncrease;
        int NumberOfRetries;
        Func<IncomingMessage, Exception, int, TimeSpan> RetryPolicy;
    }
}
