#if NET452
namespace NServiceBus.Gateway.Tests.Routing
{
    using System.Collections.Generic;
    using System.Linq;
    using Channels;
    using NUnit.Framework;
    using Receiving;
    using Settings;

    [TestFixture]
    public class When_using_the_configuration_based_channel_manager
    {
        [SetUp]
        public void SetUp()
        {
            channelManager = new ConfigurationBasedChannelManager(GatewaySettings.GetConfiguredChannels(new SettingsHolder()));
            activeChannels = channelManager.GetReceiveChannels();
            defaultChannel = channelManager.GetDefaultChannel();
        }

        [Test]
        public void Should_read_the_channels_from_app_config()
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

        IManageReceiveChannels channelManager;
        IEnumerable<ReceiveChannel> activeChannels;
        Channel defaultChannel;
    }
}
#endif