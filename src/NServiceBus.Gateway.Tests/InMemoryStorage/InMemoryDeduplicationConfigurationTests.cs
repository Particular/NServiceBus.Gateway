namespace NServiceBus.Gateway.Tests.InMemoryStorage
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    class InMemoryDeduplicationConfigurationTests
    {
        [Test]
        public void Default_LRU_cache_size_is_10000()
        {
            var configuration = new InMemoryDeduplicationConfiguration();
            
            Assert.AreEqual(10000, configuration.CacheSize);
        }

        [Test]
        public void Can_configure_custom_LRU_cache_size()
        {
            var configuration = new InMemoryDeduplicationConfiguration();

            configuration.CacheSize = int.MaxValue;
            Assert.AreEqual(int.MaxValue, configuration.CacheSize);
            configuration.CacheSize = 42;
            Assert.AreEqual(42, configuration.CacheSize);
        }

        [Test]
        public void LRU_cache_size_needs_to_be_greater_than_zero()
        {
            var configuration = new InMemoryDeduplicationConfiguration();

            Assert.Throws<ArgumentOutOfRangeException>(() => configuration.CacheSize = -1);
            Assert.Throws<ArgumentOutOfRangeException>(() => configuration.CacheSize = int.MinValue);
            Assert.Throws<ArgumentOutOfRangeException>(() => configuration.CacheSize = 0);
        }
    }
}