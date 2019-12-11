namespace NServiceBus.Gateway.AcceptanceTests
{
    using System;
    using AcceptanceTesting;
    using Configuration.AdvancedExtensibility;
    using NUnit.Framework;

    public class When_sending_to_unknown_site : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw_on_send()
        {
            Assert.That(async () =>
            {
                await Scenario.Define<ScenarioContext>()
                    .WithEndpoint<GatewayEndpoint>(b => b.When(async (bus, c) => await bus.SendToSites(new[]
                    {
                        "NonConfiguredSite1","SiteA", "NonConfiguredSite2",
                    }, new MyMessage())))
                    .Done(c => false)
                    .Run(TimeSpan.FromSeconds(10));
            }, Throws.Exception.With.Message.Contains("The following sites have not been configured: NonConfiguredSite1, NonConfiguredSite2"));
        }

        class GatewayEndpoint : EndpointConfigurationBuilder
        {
            public GatewayEndpoint()
            {
                EndpointSetup<AcceptanceTests.GatewayEndpoint>(c =>
                {
                    var gatewaySettings = c.GetSettings().Get<GatewaySettings>();
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