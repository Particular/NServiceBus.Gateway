namespace NServiceBus.Gateway.Tests.Routing
{
    using System.Collections.Generic;
    using System.Linq;
    using Channels;
    using Gateway.Routing.Sites;
    using NUnit.Framework;

    [TestFixture]
    public class When_routing_a_reply_message
    {
        [Test]
        public void Should_return_the_correct_site_based_on_the_originating_site_header()
        {
            var defaultChannel = new Channel
            {
                Type = "http",
                Address = "http://x.y"
            };

            var headers = new Dictionary<string, string> { { Headers.OriginatingSite, defaultChannel.ToString() } };

            Assert.That(OriginatingSiteHeaderRouter.GetDestinationSitesFor(headers).First().Channel, Is.EqualTo(defaultChannel));
        }


        [Test]
        public void Should_return_empty_list_if_header_is_missing()
        {
            Assert.That(OriginatingSiteHeaderRouter.GetDestinationSitesFor([]).Count(), Is.EqualTo(0));
        }
    }
}