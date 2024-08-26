namespace NServiceBus.Gateway.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class SendOptionsExtensionsTests
    {
        [Test]
        public void GetSitesRoutingTo_WhenNoSitesConfigured_ShouldReturnEmptyArray()
        {
            var sendOptions = new SendOptions();

            var sites = sendOptions.GetSitesRoutingTo();

            Assert.That(sites, Is.Empty);
        }

        [Test]
        public void GetSitesRoutingTo_WhenRoutingToSites_ShouldReturnConfiguredSites()
        {
            var sendOptions = new SendOptions();
            var expectedSites = new[]
            {
                "site A",
                "",
                "site B"
            };

            sendOptions.RouteToSites(expectedSites);
            var sites = sendOptions.GetSitesRoutingTo();

            CollectionAssert.AreEqual(expectedSites, sites);
        }
    }
}