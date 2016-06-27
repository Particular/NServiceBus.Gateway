﻿namespace NServiceBus.AcceptanceTests.Gateway
{
    using System;
    using System.Threading.Tasks;
    using Config;
    using EndpointTemplates;
    using AcceptanceTesting;
    using Features;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_sending_a_message_to_another_site : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_be_able_to_reply_to_the_message()
        {
            return Scenario.Define<Context>()
                    .WithEndpoint<Headquarters>(b => b.When(async (bus,c) => await bus.SendToSites(new[] { "SiteA" }, new MyRequest())))
                    .WithEndpoint<SiteA>()
                    .Done(c => c.GotResponseBack)
                    .Repeat(r => r.For(Transports.Default)
                    )
                    .Should(c => Assert.IsTrue(c.GotResponseBack))
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool GotResponseBack { get; set; }
        }

        public class Headquarters : EndpointConfigurationBuilder
        {
            public Headquarters()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnableFeature<Gateway>();
                })
                    .WithConfig<GatewayConfig>(c =>
                        {
                            c.Sites = new SiteCollection
                                {
                                    new SiteConfig
                                        {
                                            Key = "SiteA",
                                            Address = "http://localhost:25999/SiteA/",
                                            ChannelType = "http"
                                        }
                                };

                            c.Channels = new ChannelCollection
                                {
                                    new ChannelConfig
                                        {
                                             Address = "http://localhost:25999/Headquarters/",
                                            ChannelType = "http"
                                        }
                                };


                        });
            }

            public class MyResponseHandler : IHandleMessages<MyResponse>
            {
                public Context Context { get; set; }

                public Task Handle(MyResponse response, IMessageHandlerContext context)
                {
                    Context.GotResponseBack = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class SiteA : EndpointConfigurationBuilder
        {
            public SiteA()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.ScaleOut().InstanceDiscriminator("1");
                    c.EnableFeature<Gateway>();
                })
                        .WithConfig<GatewayConfig>(c =>
                        {
                            c.Channels = new ChannelCollection
                                {
                                    new ChannelConfig
                                        {
                                             Address = "http://localhost:25999/SiteA/",
                                            ChannelType = "http"
                                        }
                                };
                        });
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {

                public Task Handle(MyRequest request, IMessageHandlerContext context)
                {
                    return context.Reply(new MyResponse());
                }
            }
        }

        [Serializable]
        public class MyRequest : IMessage
        {
        }

        [Serializable]
        public class MyResponse : IMessage
        {
        }
    }
}
