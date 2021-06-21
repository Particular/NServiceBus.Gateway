namespace NServiceBus.Gateway.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvancedExtensibility;
    using NUnit.Framework;

    public class When_doing_request_reply_with_proxy_address : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Callback_should_be_fired_when_SideAProxy_IsBehind_Proxy()
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
                EndpointSetup<GatewayEndpoint>(c =>
                    {
                        c.MakeInstanceUniquelyAddressable("1");
                        c.EnableCallbacks();

                        var gatewaySettings = c.GetSettings().Get<GatewaySettings>();
                        gatewaySettings.AddReceiveChannel("http://+:25698/SiteA/", "http", 10, true, "http://localhost:25698/SiteA/");
                        gatewaySettings.AddSite("SiteB", "http://localhost:25699/SiteB/");
                    });
            }
        }

        public class SiteB : EndpointConfigurationBuilder
        {
            public SiteB()
            {
                EndpointSetup<GatewayEndpoint>(c =>
                {
                    c.EnableCallbacks(makesRequests: false);
                    var gatewaySettings = c.GetSettings().Get<GatewaySettings>();
                    gatewaySettings.AddReceiveChannel("http://localhost:25699/SiteB/");
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