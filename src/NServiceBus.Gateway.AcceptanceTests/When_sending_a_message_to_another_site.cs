namespace NServiceBus.AcceptanceTests.Gateway
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Config;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_sending_a_message_to_another_site : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_able_to_reply_to_the_message()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Headquarters>(b => b.When(async (bus, c) => await bus.SendToSites(new[]
                {
                    "SiteA"
                }, new MyRequest())))
                .WithEndpoint<SiteA>()
                .Done(c => c.GotResponseBack)
                .Run();

            Assert.IsTrue(context.GotResponseBack);
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
                    c.EnableGateway(new GatewayConfig
                    {
                        Sites = new SiteCollection
                        {
                            new SiteConfig
                            {
                                Key = "SiteA",
                                Address = "http://localhost:25999/SiteA/",
                                ChannelType = "http"
                            }
                        },
                        Channels = new ChannelCollection
                        {
                            new ChannelConfig
                            {
                                Address = "http://localhost:25999/Headquarters/",
                                ChannelType = "http"
                            }
                        }
                    });
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
                    c.EnableGateway(new GatewayConfig
                    {
                        Channels = new ChannelCollection
                        {
                            new ChannelConfig
                            {
                                Address = "http://localhost:25999/SiteA/",
                                ChannelType = "http"
                            }
                        }
                    });
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

        public class MyRequest : IMessage
        {
        }

        public class MyResponse : IMessage
        {
        }
    }
}