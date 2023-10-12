namespace NServiceBus.Gateway.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Extensibility;
    using NUnit.Framework;
    using Transport;

    class When_invoking_gateway_recoverability_policy
    {
        [SetUp]
        public void Setup()
        {
            context = new ErrorContext(
                new Exception(),
                [],
                Guid.NewGuid().ToString(),
                new byte[0],
                new TransportTransaction(),
                2,
                "my-queue",
                new ContextBag());

            config = new RecoverabilityConfig(
                new ImmediateConfig(0),
                new DelayedConfig(4, TimeSpan.FromSeconds(10)),
                new FailedConfig("error", []));
        }

        [Test]
        public void Should_call_retry_policy_with_current_retry_number()
        {
            var retryPolicyCalled = false;
            var currentRetry = 0;

            GatewayRecoverabilityPolicy.Invoke(context, (message, exception, retry) =>
            {
                retryPolicyCalled = true;
                currentRetry = retry;
                return TimeSpan.MinValue;
            }, config);

            Assert.IsTrue(retryPolicyCalled, "Retry policy was not called by the recoverability policy");
            Assert.AreEqual(context.DelayedDeliveriesPerformed + 1, currentRetry, "Retry policy was called with wrong retry number");
        }

        [Test]
        public void Should_move_to_error_queue_based_on_retry_policy()
        {
            var action = GatewayRecoverabilityPolicy.Invoke(context, (message, exception, retry) => TimeSpan.MinValue, config);

            Assert.IsInstanceOf<MoveToError>(action, "MoveToError recoverability action was expected");
            var moveToErrorAction = action as MoveToError;
            Assert.AreEqual(config.Failed.ErrorQueue, moveToErrorAction.ErrorQueue, "MoveToError recoverability action has wrong error queue");
        }

        [Test]
        public void Should_make_delayed_retry_based_on_retry_policy()
        {
            var requestedDelay = TimeSpan.FromSeconds(10);

            var action = GatewayRecoverabilityPolicy.Invoke(context, (message, exception, retry) => requestedDelay, config);

            Assert.IsInstanceOf<DelayedRetry>(action, "DelayedRetry recoverability action was expected");
            var delayedRetryAction = action as DelayedRetry;
            Assert.AreEqual(requestedDelay, delayedRetryAction.Delay, "DelayedRetry recoverability action has wrong delay");
        }

        ErrorContext context;
        RecoverabilityConfig config;
    }
}