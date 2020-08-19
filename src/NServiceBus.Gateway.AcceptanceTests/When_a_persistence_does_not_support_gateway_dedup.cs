namespace NServiceBus.Gateway.AcceptanceTests
{
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_a_persistence_does_not_support_gateway_dedup : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw_exception()
        {
            Assert.That(async () =>
            {
                await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>()
                    .Done(c => c.EndpointsStarted)
                    .Run();
            }, Throws.Exception.With.Message.Contains("please configure one"));
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<GatewayEndpointWithNoStorage>(c =>
                {
                    c.Gateway(new InMemoryDeduplicationConfiguration()).AddReceiveChannel("http://localhost:25898/SomeSite/");
                });
            }
        }

        public class Context : ScenarioContext
        {
        }
    }
}
