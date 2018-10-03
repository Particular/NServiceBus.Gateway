namespace NServiceBus.Gateway.AcceptanceTests
{
    using System;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_sending_to_invalid_site : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw()
        {
            Assert.That(async () =>
            {
                await Scenario.Define<ScenarioContext>()
                    .WithEndpoint<GatewayEndpoint>(b => b.When(async (bus, c) => await bus.SendToSites(new[]
                    {
                        "SiteA","NonConfiguredSite"
                    }, new MyMessage())))
                    .Done(c => false)
                    .Run(TimeSpan.FromSeconds(10));
            }, Throws.Exception.With.Message.Contains("Make sure that you have configured the site `NonConfiguredSite` properly"));
        }

        class GatewayEndpoint : EndpointConfigurationBuilder
        {
            public GatewayEndpoint()
            {
                EndpointSetup<AcceptanceTests.GatewayEndpoint>(c =>
                {
                    var gatewaySettings = c.Gateway();

                    gatewaySettings.AddReceiveChannel("http://localhost:25605/Gateway/");
                    gatewaySettings.AddSite("SiteA", "http://sitea.com");
                });
            }
        }

        class MyMessage : IMessage
        {
        }
    }
}
