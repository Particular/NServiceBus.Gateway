namespace NServiceBus.Gateway.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvancedExtensibility;
    using NUnit.Framework;

    public class When_specifying_replyto_address
    {
        [Test]
        public async Task Reply_should_be_received()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SiteA>(b => b.When(async (bus, c) =>
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
            Assert.AreEqual(42, context.Response);
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
                    gatewaySettings.AddReceiveChannel("http://+:25698/SiteA/");
                    //gatewaySettings.SetReplyToAddress("http://localhost:25698/SiteA/");
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
                    return context.Reply(42);
                }
            }
        }


        public class MyRequest : ICommand
        {
        }

    }
}
