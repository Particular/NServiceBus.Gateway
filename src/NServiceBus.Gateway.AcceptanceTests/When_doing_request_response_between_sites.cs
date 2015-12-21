namespace NServiceBus.AcceptanceTests.Gateway
{
    using System;
    using System.Threading.Tasks;
    using Config;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_doing_request_response_between_sites : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Callback_should_be_fired()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SiteA>(
                    b => b.When(async (bus, c) =>
                    {
                        var options = new SendOptions();
                        options.RouteToSites("SiteB");
                        c.Response = await bus.Request<MyResponse>(new MyRequest(), options);
                        c.GotCallback = true;
                    }))
                .WithEndpoint<SiteB>()
                .Done(c => c.GotCallback)
                .Run();

            Assert.IsTrue(context.GotCallback);
            Assert.NotNull(context.Response);
        }

        public class Context : ScenarioContext
        {
            public bool GotCallback { get; set; }
            public MyResponse Response { get; set; }
        }

        public class SiteA : EndpointConfigurationBuilder
        {
            public SiteA()
            {
                EndpointSetup<DefaultServer>(c => c.EnableFeature<Features.Gateway>())
                    .WithConfig<GatewayConfig>(c =>
                    {
                        c.Sites = new SiteCollection
                        {
                            new SiteConfig
                            {
                                Key = "SiteB",
                                Address = "http://localhost:25799/SiteB/",
                                ChannelType = "http"
                            }
                        };

                        c.Channels = new ChannelCollection
                        {
                            new ChannelConfig
                            {
                                Address = "http://localhost:25799/SiteA/",
                                ChannelType = "http",
                                Default = true
                            }
                        };
                    });
            }
        }

        public class SiteB : EndpointConfigurationBuilder
        {
            public SiteB()
            {
                EndpointSetup<DefaultServer>(c => c.EnableFeature<Features.Gateway>())
                    .WithConfig<GatewayConfig>(c =>
                    {
                        c.Channels = new ChannelCollection
                        {
                            new ChannelConfig
                            {
                                Address = "http://localhost:25799/SiteB/",
                                ChannelType = "http",
                                Default = true
                            }
                        };
                    });

            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public async Task Handle(MyRequest request, IMessageHandlerContext context)
                {
                    await context.Reply(new MyResponse());
                }
            }
        }

        [Serializable]
        public class MyRequest : ICommand
        {
        }
        [Serializable]
        public class MyResponse : IMessage
        {
        }
    }
}
