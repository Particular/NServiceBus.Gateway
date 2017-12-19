﻿namespace NServiceBus.Gateway.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_listening_on_wildcard_uri : NServiceBusAcceptanceTest
    {
        [Test]
        public void should_throw_exception_if_wildcard_channel_is_used_for_replies()
        {
            Assert.That(async () =>
            {
                await Scenario.Define<ScenarioContext>()
                    .WithEndpoint<EndpointWithWildCardUriAsDefault>(e => e.When(b => Task.FromResult(0)))
                    .Run();
            }, Throws.Exception.With.Message.Contains("Please add an extra channel with a fully qualified non-wildcard uri in order for replies to be transmitted properly."));
        }

        class EndpointWithWildCardUriAsDefault : EndpointConfigurationBuilder
        {
            public EndpointWithWildCardUriAsDefault()
            {
                EndpointSetup<GatewayEndpoint>(c =>
                {
                    var gatewaySettings = c.Gateway();

                    gatewaySettings.AddReceiveChannel("http://+:25699/");
                });
            }
        }
    }
}
