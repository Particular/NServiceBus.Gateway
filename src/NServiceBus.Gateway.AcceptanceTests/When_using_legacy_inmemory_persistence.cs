namespace NServiceBus.Gateway.AcceptanceTests
{
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Logging;
    using NUnit.Framework;

    public class When_using_legacy_inmemory_persistence : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_log_warning()
        {
            var context = await Scenario.Define<ScenarioContext>()
                .WithEndpoint<EndpointWithLegacyConfiguration>()
                .Done(c => c.EndpointsStarted)
                .Run();

            var warnings = context.Logs
                .Where(l => l.Level == LogLevel.Warn)
                .Select(l => l.Message);
            Assert.That(warnings.Any(l => l.Contains("Endpoint is configured to use the legacy in-memory gateway deduplication storage")));
        }

        class EndpointWithLegacyConfiguration : EndpointConfigurationBuilder
        {
            public EndpointWithLegacyConfiguration()
            {
                EndpointSetup<GatewayEndpointWithNoStorage>(c =>
                {
                    c.UsePersistence<InMemoryPersistence, StorageType.GatewayDeduplication>();
                    
#pragma warning disable 618
                    // legacy configuration API (not passing a storage as parameter):
                    var gatewayConfiguration = c.Gateway();
#pragma warning restore 618
                    gatewayConfiguration.AddReceiveChannel("http://localhost:25999/SiteA/");
                });
            }
        }
    }
}