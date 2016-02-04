namespace NServiceBus.Gateway.Tests.Routing
{
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using Channels;
    using Config;
    using NUnit.Framework;
    using Receiving;

    [TestFixture]
    public class When_using_the_configuration_bases_channel_manager
    {
        IManageReceiveChannels config;
        IEnumerable<ReceiveChannel> activeChannels;
        Channel defaultChannel;

        [SetUp]
        public void SetUp()
        {

            var section = ConfigurationManager.GetSection(typeof(GatewayConfig).Name) as GatewayConfig;


            config = new ConfigurationBasedChannelManager
            {
                ReceiveChannels = section.GetChannels().ToList()
            };

            activeChannels = config.GetReceiveChannels();
            defaultChannel = config.GetDefaultChannel();

        }

        [Test]
        public void Should_read_the_channels_from_the_configSource()
        {
            Assert.AreEqual(activeChannels.Count(), 3);
        }

        [Test]
        public void Should_default_the_max_concurrency_to_1()
        {
            Assert.AreEqual(activeChannels.First().MaxConcurrency, 1);
        }

        [Test]
        public void Should_allow_max_concurrency_to_be_specified()
        {
            Assert.AreEqual(activeChannels.Last().MaxConcurrency, 3);
        }

        [Test]
        public void Should_default_to_the_first_channel_if_no_default_is_set()
        {
            Assert.AreEqual(activeChannels.First(), defaultChannel);
        }
    }
}