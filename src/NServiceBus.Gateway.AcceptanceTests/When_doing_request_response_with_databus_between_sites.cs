namespace NServiceBus.Gateway.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvancedExtensibility;
    using NUnit.Framework;

    public class When_doing_request_response_with_databus_between_sites : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_able_to_reply_to_the_message_using_databus()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SiteA>(
                    b => b.When(async (bus, c) =>
                    {
                        var options = new SendOptions();
                        options.RouteToSites("SiteB");
                        c.Response = await bus.Request<MyResponse>(new MyRequest
                        {
                            Payload = new DataBusProperty<byte[]>(PayloadToSend)
                        }, options);
                        c.GotCallback = true;
                    }))
                .WithEndpoint<SiteB>()
                .Done(c => c.GotResponseBack && c.GotCallback)
                .Run();

            Assert.Multiple(() =>
            {
                Assert.That(context.SiteBReceivedPayload, Is.EqualTo(PayloadToSend),
                            "The large payload should be marshalled correctly using the databus");
                Assert.That(context.SiteAReceivedPayloadInResponse, Is.EqualTo(PayloadToSend),
                    "The large payload should be marshalled correctly using the databus");
            });
            Assert.Multiple(() =>
            {
                Assert.That(context.OriginatingSiteForRequest, Is.EqualTo("http,http://localhost:25899/SiteA/"));
                Assert.That(context.OriginatingSiteForResponse, Is.EqualTo("http,http://localhost:25899/SiteB/"));
                Assert.That(context.Response, Is.Not.Null);
            });
        }

        static byte[] PayloadToSend = new byte[1024 * 1024 * 10];

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
                EndpointSetup<GatewayEndpoint>(c =>
                {
                    c.UseDataBus<FileShareDataBus, SystemJsonDataBusSerializer>().BasePath(@".\databus\siteA");
                    c.MakeInstanceUniquelyAddressable("1");
                    c.EnableCallbacks();

                    var gatewaySettings = c.GetSettings().Get<GatewaySettings>();
                    gatewaySettings.AddReceiveChannel("http://localhost:25899/SiteA/");
                    gatewaySettings.AddSite("SiteB", "http://localhost:25899/SiteB/");
                });
            }

            public class MyResponseHandler : IHandleMessages<MyResponse>
            {
                Context testContext;

                public MyResponseHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MyResponse response, IMessageHandlerContext context)
                {
                    testContext.GotResponseBack = true;
                    testContext.SiteAReceivedPayloadInResponse = response.OriginalPayload.Value;

                    // Inspect the headers to find the originating site address
                    testContext.OriginatingSiteForResponse = context.MessageHeaders[Headers.OriginatingSite];

                    return Task.FromResult(0);
                }
            }
        }

        public class SiteB : EndpointConfigurationBuilder
        {
            public SiteB()
            {
                EndpointSetup<GatewayEndpoint>(c =>
                {
                    c.UseDataBus<FileShareDataBus, SystemJsonDataBusSerializer>().BasePath(@".\databus\siteB");
                    c.EnableCallbacks(makesRequests: false);
                    var gatewaySettings = c.GetSettings().Get<GatewaySettings>();
                    gatewaySettings.AddReceiveChannel("http://localhost:25899/SiteB/");
                });
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                Context testContext;

                public MyRequestHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public async Task Handle(MyRequest request, IMessageHandlerContext context)
                {
                    testContext.SiteBReceivedPayload = request.Payload.Value;
                    await context.Reply(new MyResponse
                    {
                        OriginalPayload = request.Payload
                    });

                    // Inspect the headers to find the originating site address
                    testContext.OriginatingSiteForRequest = context.MessageHeaders[Headers.OriginatingSite];
                }
            }
        }

        public class MyRequest : ICommand
        {
            public DataBusProperty<byte[]> Payload { get; set; }
        }

        public class MyResponse : IMessage
        {
            public DataBusProperty<byte[]> OriginalPayload { get; set; }
        }
    }
}
