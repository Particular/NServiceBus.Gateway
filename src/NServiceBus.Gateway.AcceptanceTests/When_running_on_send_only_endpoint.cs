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

            StringAssert.Contains("Gateway is not support for send only endpoints.", ex.Message);
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