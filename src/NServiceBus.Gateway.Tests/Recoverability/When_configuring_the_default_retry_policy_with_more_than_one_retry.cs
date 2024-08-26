namespace NServiceBus.Gateway.Tests.Recoverability
{
    using System;
    using Configuration.AdvancedExtensibility;
    using Transport;
    using NUnit.Framework;

    [TestFixture]
    class When_configuring_the_default_retry_policy_with_more_than_one_retry
    {
        [SetUp]
        public void Setup()
        {
            FailingMessage = new IncomingMessage("failing-id", [], new byte[0]);
            Exception = new Exception("exception-message");
            TimeIncrease = TimeSpan.FromSeconds(1);
            NumberOfRetries = 2;

            var config = new EndpointConfiguration("fake-endpoint");
            config.Gateway(new NonDurableDeduplicationConfiguration()).Retries(NumberOfRetries, TimeIncrease);

            RetryPolicy = config.GetSettings().Get<Func<IncomingMessage, Exception, int, TimeSpan>>("Gateway.Retries.RetryPolicy");
        }

        [Test]
        public void Should_allow_at_least_that_many_retries()
        {
            var delay = RetryPolicy.Invoke(FailingMessage, Exception, NumberOfRetries);

            Assert.AreNotEqual(TimeSpan.MinValue, delay, $"{NumberOfRetries} retries should be allowed");
        }

        [Test]
        public void Should_prevent_subsequent_retries()
        {
            var delay = RetryPolicy.Invoke(FailingMessage, Exception, NumberOfRetries + 1);

            Assert.That(delay, Is.EqualTo(TimeSpan.MinValue), $"{NumberOfRetries + 1} should not be allowed");
        }

        IncomingMessage FailingMessage;
        Exception Exception;
        TimeSpan TimeIncrease;
        int NumberOfRetries;
        Func<IncomingMessage, Exception, int, TimeSpan> RetryPolicy;
    }
}