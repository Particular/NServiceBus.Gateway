namespace NServiceBus.AcceptanceTests.Gateway
{
    using System;
    using System.Threading.Tasks;
    using Config;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_doing_request_reply : NServiceBusAcceptanceTest
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
                        c.Response = await bus.Request<int>(new MyRequest(), options);
                        c.GotCallback = true;
                    }))
                .WithEndpoint<SiteB>()
                .Done(c => c.GotCallback)
                .Run();

            Assert.IsTrue(context.GotCallback);
            Assert.AreEqual(1, context.Response);
        }

        public class Context : ScenarioContext
        {
            public bool GotCallback { get; set; }
            public int Response { get; set; }
        }

        public class SiteA : EndpointConfigurationBuilder
        {
            public SiteA()
            {
                EndpointSetup<DefaultServerWithCallbacks>(c =>
                {
                    c.ScaleOut().InstanceDiscriminator("1");
                    c.EnableFeature<Features.Gateway>();
                })
                    .WithConfig<GatewayConfig>(c =>
                    {
                        c.Sites = new SiteCollection
                        {
                            new SiteConfig
                            {
                                Key = "SiteB",
                                Address = "http://localhost:25699/SiteB/",
                                ChannelType = "http"
                            }
                        };

                        c.Channels = new ChannelCollection
                        {
                            new ChannelConfig
                            {
                                Address = "http://localhost:25699/SiteA/",
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
                EndpointSetup<DefaultServerWithCallbacks>(c =>
                {
                    c.ScaleOut().InstanceDiscriminator("1");
                    c.EnableFeature<Features.Gateway>();
                })
                    .WithConfig<GatewayConfig>(c =>
                    {
                        c.Channels = new ChannelCollection
                        {
                            new ChannelConfig
                            {
                                Address = "http://localhost:25699/SiteB/",
                                ChannelType = "http",
                                Default = true
                            }
                        };
                    });

            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public Task Handle(MyRequest request, IMessageHandlerContext context)
                {
                    return context.Reply(1);
                }
            }
        }

        [Serializable]
        public class MyRequest : ICommand
        {
        }
    }
}
