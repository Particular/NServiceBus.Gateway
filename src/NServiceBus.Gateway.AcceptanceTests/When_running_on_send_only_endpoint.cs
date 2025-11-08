namespace NServiceBus.Gateway.AcceptanceTests
{
    using System;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_running_on_send_only_endpoint : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw_on_startup()
        {
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await Scenario.Define<ScenarioContext>()
                    .WithEndpoint<GatewayEndpoint>()
                    .Done(c => c.EndpointsStarted)
                    .Run());

            Assert.That(ex.Message, Does.Contain("Gateway is not supported for send only endpoints."));
        }

        class GatewayEndpoint : EndpointConfigurationBuilder
        {
            public GatewayEndpoint()
            {
                EndpointSetup<AcceptanceTests.GatewayEndpoint>(c => c.SendOnly());
            }
        }
    }
}