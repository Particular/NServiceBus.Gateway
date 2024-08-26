namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Gateway.HeaderManagement;
    using Gateway.Tests;
    using NUnit.Framework;

    [TestFixture]
    public class GatewayOutgoingBehaviorTests
    {
        const string addressOfOriginatingEndpoint = "EndpointLocatedInSiteA";
        const string originatingSite = "SiteA";

        [Test]
        public async Task Should_set_correct_headers_for_response_messages()
        {
            var returnState = new GatewayIncomingBehavior.ReturnState
            {
                ReplyToAddress = addressOfOriginatingEndpoint,
                OriginatingSite = originatingSite,
                LegacyMode = false
            };

            var context = new OutgoingPhysicalMessageContextFake(returnState);

            var behavior = new GatewayOutgoingBehavior();
            await behavior.Invoke(context, () => Task.FromResult(0));


            Assert.AreEqual(originatingSite, context.Headers[Headers.OriginatingSite]);
            Assert.AreEqual(addressOfOriginatingEndpoint, context.Headers[Headers.RouteTo]);
            Assert.AreEqual("False", context.Headers[GatewayHeaders.LegacyMode]);
        }

        [Test]
        public async Task Should_set_correct_httpTo_for_legacy_response_message()
        {
            var returnState = new GatewayIncomingBehavior.ReturnState
            {
                HttpFrom = originatingSite,
            };

            var context = new OutgoingPhysicalMessageContextFake(returnState);

            var behavior = new GatewayOutgoingBehavior();
            await behavior.Invoke(context, () => Task.FromResult(0));


            Assert.AreEqual(originatingSite, context.Headers[Headers.HttpTo]);
        }

        [Test]
        public async Task Should_not_override_existing_routeto_for_response_messages()
        {
            const string existingRouteTo = "existing";
            var returnState = new GatewayIncomingBehavior.ReturnState
            {
                ReplyToAddress = addressOfOriginatingEndpoint,
                OriginatingSite = originatingSite,
                LegacyMode = false
            };

            var headers = new Dictionary<string, string>
            {
                [Headers.RouteTo] = existingRouteTo,
                [Headers.CorrelationId] = Guid.NewGuid().ToString()
            };

            var context = new OutgoingPhysicalMessageContextFake(returnState, headers);

            var behavior = new GatewayOutgoingBehavior();
            await behavior.Invoke(context, () => Task.FromResult(0));


            Assert.AreEqual(existingRouteTo, context.Headers[Headers.RouteTo]);
        }

        [Test]
        public async Task Should_not_set_response_headers_if_incoming_return_headers_dont_exists()
        {
            var context = new OutgoingPhysicalMessageContextFake();

            var behavior = new GatewayOutgoingBehavior();
            await behavior.Invoke(context, () => Task.FromResult(0));

            Assert.That(context.Headers.ContainsKey(Headers.OriginatingSite), Is.False);
        }

        [Test]
        public async Task Should_not_set_response_headers_if_missing_correlation_id()
        {
            var headers = new Dictionary<string, string>();//no correlation id

            var context = new OutgoingPhysicalMessageContextFake(new GatewayIncomingBehavior.ReturnState(), headers);

            var behavior = new GatewayOutgoingBehavior();
            await behavior.Invoke(context, () => Task.FromResult(0));

            Assert.That(context.Headers.ContainsKey(Headers.OriginatingSite), Is.False);
        }

        [Test]
        public async Task Should_not_set_response_headers_if_doing_normal_send()
        {
            var headers = new Dictionary<string, string>
            {
                [Headers.DestinationSites] = "something",
                [Headers.CorrelationId] = Guid.NewGuid().ToString()
            };

            var context = new OutgoingPhysicalMessageContextFake(new GatewayIncomingBehavior.ReturnState(), headers);

            var behavior = new GatewayOutgoingBehavior();
            await behavior.Invoke(context, () => Task.FromResult(0));

            Assert.That(context.Headers.ContainsKey(Headers.OriginatingSite), Is.False);
        }

        [Test]
        public async Task Should_not_set_response_headers_if_doing_legacy_send()
        {
            var headers = new Dictionary<string, string>
            {
                [Headers.HttpTo] = "something",
                [Headers.CorrelationId] = Guid.NewGuid().ToString()
            };

            var context = new OutgoingPhysicalMessageContextFake(new GatewayIncomingBehavior.ReturnState(), headers);

            var behavior = new GatewayOutgoingBehavior();
            await behavior.Invoke(context, () => Task.FromResult(0));

            Assert.That(context.Headers.ContainsKey(Headers.OriginatingSite), Is.False);
        }
    }
}
