namespace NServiceBus.Gateway.Tests.NonDurableStorage
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    class NonDurableDeduplicationConfigurationTests
    {
        [Test]
        public void Default_LRU_cache_size_is_10000()
        {
            var configuration = new NonDurableDeduplicationConfiguration();

            Assert.That(configuration.CacheSize, Is.EqualTo(10000));
        }

        [Test]
        public void Can_configure_custom_LRU_cache_size()
        {
            var configuration = new NonDurableDeduplicationConfiguration
            {
                CacheSize = int.MaxValue
            };
            Assert.That(configuration.CacheSize, Is.EqualTo(int.MaxValue));
            configuration.CacheSize = 42;
            Assert.That(configuration.CacheSize, Is.EqualTo(42));
        }

        [Test]
        public void LRU_cache_size_needs_to_be_greater_than_zero()
        {
            var configuration = new NonDurableDeduplicationConfiguration();

            Assert.Throws<ArgumentOutOfRangeException>(() => configuration.CacheSize = -1);
            Assert.Throws<ArgumentOutOfRangeException>(() => configuration.CacheSize = int.MinValue);
            Assert.Throws<ArgumentOutOfRangeException>(() => configuration.CacheSize = 0);
        }
    }
}