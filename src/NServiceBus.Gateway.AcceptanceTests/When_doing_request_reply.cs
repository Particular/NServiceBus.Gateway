﻿namespace NServiceBus.AcceptanceTests.Gateway
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Config;
    using EndpointTemplates;
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
            Assert.AreEqual(1,context.Response);
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
                EndpointSetup<DefaultServer>(c =>
                {
                    c.MakeInstanceUniquelyAddressable("1");
                    c.EnableCallbacks();
                    c.EnableGateway(new GatewayConfig
                    {
                        Sites = new SiteCollection
                        {
                            new SiteConfig
                            {
                                Key = "SiteB",
                                Address = "http://localhost:25699/SiteB/",
                                ChannelType = "http"
                            }
                        },

                        Channels = new ChannelCollection
                        {
                            new ChannelConfig
                            {
                                Address = "http://localhost:25699/SiteA/",
                                ChannelType = "http",
                                Default = true
                            }
                        }
                    });
                });
            }
        }

        public class SiteB : EndpointConfigurationBuilder
        {
            public SiteB()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnableCallbacks(makesRequests: false);
                    c.EnableGateway(new GatewayConfig
                    {
                        Channels = new ChannelCollection
                        {
                            new ChannelConfig
                            {
                                Address = "http://localhost:25699/SiteB/",
                                ChannelType = "http",
                                Default = true
                            }
                        }
                    });
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


        public class MyRequest : ICommand
        {
        }
    }
}