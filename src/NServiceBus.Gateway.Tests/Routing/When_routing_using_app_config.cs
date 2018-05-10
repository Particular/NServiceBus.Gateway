#if NET452
namespace NServiceBus.Gateway.Tests.Routing
{
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using Channels;
    using Config;
    using Gateway.Routing;
    using Gateway.Routing.Sites;
    using NUnit.Framework;

    [TestFixture]
    public class When_routing_using_app_config
    {

        [Test]
        public void Should_read_sites_and_their_keys_from_the_configSource()
        {
            var section = ConfigurationManager.GetSection(typeof(GatewayConfig).Name) as GatewayConfig;

            var router = new ConfigurationBasedSiteRouter(section.Sites.Cast<SiteConfig>().Select(site => new Site
            {
                Key = site.Key,
                Channel = new Channel
                {
                    Type = site.ChannelType,
                    Address = site.Address
                },
                LegacyMode = site.LegacyMode
            }).ToList());

            var headers = new Dictionary<string, string>{{Headers.DestinationSites, "SiteA"}};

            var sites = router.GetDestinationSitesFor(headers);

            Assert.AreEqual(new Channel{ Address = "http://sitea.com",Type = "http"},sites.First().Channel);
        }
    }
}
#endif
