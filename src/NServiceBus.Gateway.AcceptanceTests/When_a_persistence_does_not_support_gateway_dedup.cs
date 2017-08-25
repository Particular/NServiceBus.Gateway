namespace NServiceBus.AcceptanceTests.Gateway
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Config;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_a_persistence_does_not_support_gateway_dedup : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw_exception()
        {
            Assert.That(async () =>
            {
                await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(e => e.When(b => Task.FromResult(0)))
                    .Run();
            }, Throws.Exception.With.Message.Contains("please configure one"));
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServerWithNoStorage>(c =>
                {
                    c.EnableGateway(new GatewayConfig
                    {
                        Channels = new ChannelCollection
                        {
                            new ChannelConfig
                            {
                                Address = "http://localhost:25898/SomeSite/",
                                ChannelType = "http"
                            }
                        }
                    });
                });
            }
        }

        public class Context : ScenarioContext
        {
        }
    }
}
