namespace NServiceBus.AcceptanceTests.Gateway
{
    using System;
    using System.Threading.Tasks;
    using Config;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_doing_request_response_with_databus_between_sites : NServiceBusAcceptanceTest
    {
        static byte[] PayloadToSend = new byte[1024 * 1024 * 10];

        [Test]
        public async Task Should_be_able_to_reply_to_the_message_using_databus()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SiteA>(
                    b => b.When(async (bus, c) =>
                    {
                        var options = new SendOptions();
                        options.RouteToSites("SiteB");
                        c.Response = await bus.Request<MyResponse>(new MyRequest { Payload = new DataBusProperty<byte[]>(PayloadToSend) }, options);
                        c.GotCallback = true;
                    }))
                .WithEndpoint<SiteB>()
                .Done(c => c.GotResponseBack && c.GotCallback)
                 .Run();

            Assert.AreEqual(PayloadToSend, context.SiteBReceivedPayload,
                "The large payload should be marshalled correctly using the databus");
            Assert.AreEqual(PayloadToSend, context.SiteAReceivedPayloadInResponse,
                "The large payload should be marshalled correctly using the databus");
            Assert.AreEqual("http,http://localhost:25899/SiteA/", context.OriginatingSiteForRequest);
            Assert.AreEqual("http,http://localhost:25899/SiteB/", context.OriginatingSiteForResponse);
            Assert.NotNull(context.Response);
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
                    c.MakeInstanceUniquelyAddressable("1");
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
                    c.MakeInstanceUniquelyAddressable("1");
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
