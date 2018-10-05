﻿namespace NServiceBus.AcceptanceTests.Gateway
{
    using AcceptanceTesting;
    using Config;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_listening_on_wildcard_uri : NServiceBusAcceptanceTest
    {
        [Test]
        public void should_throw_exception_if_wildcard_channel_is_used_for_replies()
        {
            Assert.That(async () =>
            {
                await Scenario.Define<ScenarioContext>()
                    .WithEndpoint<EndpointWithWildCardUriAsDefault>()
                    .Done(c => c.EndpointsStarted)
                    .Run();
            }, Throws.Exception.InnerException.InnerException.With.Message.Contains("Please add an extra channel with a fully qualified non-wildcard uri in order for replies to be transmitted properly."));
        }

        class EndpointWithWildCardUriAsDefault : EndpointConfigurationBuilder
        {
            public EndpointWithWildCardUriAsDefault()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnableFeature<Gateway>();
                })
                    .WithConfig<GatewayConfig>(c =>
                    {
                        c.Channels = new ChannelCollection
                        {
                            new ChannelConfig
                            {
                                Address = "http://+:25699/",
                                ChannelType = "http",
                                Default = true
                            }
                        };
                    });
            }
        }
    }
}
