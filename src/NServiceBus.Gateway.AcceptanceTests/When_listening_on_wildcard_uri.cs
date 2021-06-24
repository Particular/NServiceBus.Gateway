namespace NServiceBus.Gateway.AcceptanceTests
{
    using AcceptanceTesting;
    using Configuration.AdvancedExtensibility;
    using NUnit.Framework;

    public class When_listening_on_wildcard_uri : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw_exception_if_wildcard_channel_is_used_for_replies()
        {
            Assert.That(async () =>
            {
                await Scenario.Define<ScenarioContext>()
                    .WithEndpoint<EndpointWithWildCardUriAsDefault>()
                    .Done(c => c.EndpointsStarted)
                    .Run();
            }, Throws.Exception.With.Message.Contains("contains a wildcard in the URI"));
        }

        class EndpointWithWildCardUriAsDefault : EndpointConfigurationBuilder
        {
            public EndpointWithWildCardUriAsDefault()
            {
                EndpointSetup<GatewayEndpoint>(c =>
                {
                    var gatewaySettings = c.GetSettings().Get<GatewaySettings>();
                    gatewaySettings.AddReceiveChannel("http://+:25601/WildcardA/");
                });
            }
        }

        [Test]
        public void Should_not_throw_exception_if_wildcard_channel_is_not_default()
        {
            Assert.DoesNotThrowAsync(async () =>
                await Scenario.Define<ScenarioContext>()
                    .WithEndpoint<EndpointWithWildCardUriAndFullyQualifiedDefault>()
                    .Done(c => c.EndpointsStarted)
                    .Run());
        }

        class EndpointWithWildCardUriAndFullyQualifiedDefault : EndpointConfigurationBuilder
        {
            public EndpointWithWildCardUriAndFullyQualifiedDefault()
            {
                EndpointSetup<GatewayEndpoint>(c =>
                {
                    var gatewaySettings = c.GetSettings().Get<GatewaySettings>();
                    gatewaySettings.AddReceiveChannel("http://+:25701/WildcardB/");
                    gatewaySettings.AddReceiveChannel("http://localhost:25700/WildcardB/", isDefault: true);
                });
            }
        }
    }
}
