namespace NServiceBus.AcceptanceTests.Gateway
{
    using System;
    using System.Threading.Tasks;
    using Config;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_doing_request_response_with_databus_between_sites : NServiceBusAcceptanceTest
    {
        static byte[] PayloadToSend = new byte[1024 * 1024 * 10];

        [Test]
        public Task Should_be_able_to_reply_to_the_message_using_databus()
        {
            return Scenario.Define<Context>()
                .WithEndpoint<SiteA>(
                    b => b.When(async (bus, context) =>
                    {
                        var options = new SendOptions();
                        options.RouteToSites("SiteB");
                        context.Response = await bus.Request<MyResponse>(new MyRequest { Payload = new DataBusProperty<byte[]>(PayloadToSend) }, options);
                        context.GotCallback = true;
                    }))
                .WithEndpoint<SiteB>()
                .Done(c => c.GotResponseBack && c.GotCallback)
                .Repeat(r => r.For(Transports.Default))
                .Should(c =>
                {
                    Assert.AreEqual(PayloadToSend, c.SiteBReceivedPayload,
                        "The large payload should be marshalled correctly using the databus");
                    Assert.AreEqual(PayloadToSend, c.SiteAReceivedPayloadInResponse,
                        "The large payload should be marshalled correctly using the databus");
                    Assert.AreEqual("http,http://localhost:25899/SiteA/", c.OriginatingSiteForRequest);
                    Assert.AreEqual("http,http://localhost:25899/SiteB/", c.OriginatingSiteForResponse);
                    Assert.NotNull(c.Response);
                })
                .Run();
        }

        public class Context : ScenarioContext
        {
            public bool GotResponseBack { get; set; }
            public bool GotCallback { get; set; }
            public MyResponse Response { get; set; }
            public byte[] SiteBReceivedPayload { get; set; }
            public byte[] SiteAReceivedPayloadInResponse { get; set; }
            public string OriginatingSiteForRequest { get; set; }
            public string OriginatingSiteForResponse { get; set; }
        }

        public class SiteA : EndpointConfigurationBuilder
        {
            public SiteA()
            {
                EndpointSetup<DefaultServerWithCallbacks>(c =>
                {
                    c.ScaleOut().InstanceDiscriminator("1");
                    c.EnableFeature<Features.Gateway>();
                        c.UseDataBus<FileShareDataBus>().BasePath(@".\databus\siteA");
                    }).WithConfig<GatewayConfig>(c =>
                     {
                         c.Sites = new SiteCollection
                        {
                            new SiteConfig
                            {
                                Key = "SiteB",
                                Address = "http://localhost:25899/SiteB/",
                                ChannelType = "http"
                            }
                        };

                         c.Channels = new ChannelCollection
                        {
                            new ChannelConfig
                            {
                                Address = "http://localhost:25899/SiteA/",
                                ChannelType = "http",
                                Default = true
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
                    Context.SiteAReceivedPayloadInResponse = response.OriginalPayload.Value;

                    // Inspect the headers to find the originating site address
                    Context.OriginatingSiteForResponse = context.MessageHeaders[Headers.OriginatingSite];

                    return Task.FromResult(0);
                }
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
                    c.UseDataBus<FileShareDataBus>().BasePath(@".\databus\siteB");
                })
                   .WithConfig<GatewayConfig>(c =>
                   {
                       c.Channels = new ChannelCollection
                        {
                            new ChannelConfig
                            {
                                Address = "http://localhost:25899/SiteB/",
                                ChannelType = "http",
                                Default = true
                            }
                        };
                   });

            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public Context Context { get; set; }

                public async Task Handle(MyRequest request, IMessageHandlerContext context)
                {
                    Context.SiteBReceivedPayload = request.Payload.Value;
                    await context.Reply(new MyResponse { OriginalPayload = request.Payload });

                    // Inspect the headers to find the originating site address
                    Context.OriginatingSiteForRequest = context.MessageHeaders[Headers.OriginatingSite];
                }
            }
        }

        [Serializable]
        public class MyRequest : ICommand
        {
            public DataBusProperty<byte[]> Payload { get; set; }
        }

        [Serializable]
        public class MyResponse : IMessage
        {
            public DataBusProperty<byte[]> OriginalPayload { get; set; }
        }
    }
}
