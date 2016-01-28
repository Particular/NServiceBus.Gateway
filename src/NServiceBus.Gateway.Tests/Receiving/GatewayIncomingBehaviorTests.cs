namespace NServiceBus.Core.Tests
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Gateway.HeaderManagement;
    using Gateway.Tests;
    using NUnit.Framework;

    [TestFixture]
    public class GatewayIncomingBehaviorTests
    {

        const string addressOfOriginatingEndpoint = "EndpointLocatedInSiteA";
        const string originatingSite = "SiteA";

        [Test]
        public async Task Should_store_returninfo_for_messages_with_httpfrom()
        {
            var headers = new Dictionary<string, string>
            {
                [Headers.HttpFrom] = originatingSite,
                [Headers.ReplyToAddress] = addressOfOriginatingEndpoint,
                [GatewayHeaders.LegacyMode] = "false"
            };

            var state = new GatewayIncomingBehavior.ReturnState();
            var context = new IncomingPhysicalMessageContextFake(state, headers);

            await new GatewayIncomingBehavior().Invoke(context, () => Task.FromResult(0));

            Assert.AreEqual(originatingSite, state.HttpFrom);
            Assert.AreEqual(addressOfOriginatingEndpoint, state.ReplyToAddress);
            Assert.AreEqual(false, state.LegacyMode);
        }

        [Test]
        public async Task Should_store_returninfo_for_messages_with_originatingsite()
        {
            var headers = new Dictionary<string, string>
            {
                [Headers.OriginatingSite] = originatingSite,
                [Headers.ReplyToAddress] = addressOfOriginatingEndpoint,
                [GatewayHeaders.LegacyMode] = "false"
            };

            var state = new GatewayIncomingBehavior.ReturnState();
            var context = new IncomingPhysicalMessageContextFake(state, headers);

            await new GatewayIncomingBehavior().Invoke(context, () => Task.FromResult(0));

            Assert.AreEqual(originatingSite, state.OriginatingSite);
            Assert.AreEqual(addressOfOriginatingEndpoint, state.ReplyToAddress);
            Assert.AreEqual(false, state.LegacyMode);
        }

        [Test]
        public async Task Should_not_store_returninfo_for_legacy_messages_missing_both_kinds_of_from_information()
        {
            var headersWithoutFrom = new Dictionary<string, string> ();
            
            var context = new IncomingPhysicalMessageContextFake(null, headersWithoutFrom);

            await new GatewayIncomingBehavior().Invoke(context, () => Task.FromResult(0));

            GatewayIncomingBehavior.ReturnState state;
            Assert.IsFalse(context.Extensions.TryGet(out state));
        }
    }
}
