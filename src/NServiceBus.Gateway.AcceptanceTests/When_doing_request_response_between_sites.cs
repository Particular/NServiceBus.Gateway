﻿namespace NServiceBus.Gateway.AcceptanceTests
{
    using System.Threading.Tasks;
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
                EndpointSetup<GatewayEndpoint>(c =>
                {
                    c.MakeInstanceUniquelyAddressable("1");
                    c.EnableCallbacks();

                    var gatewaySettings = c.Gateway();

                    gatewaySettings.AddReceiveChannel("http://localhost:25799/SiteA/");
                    gatewaySettings.AddSite("SiteB", "http://localhost:25799/SiteB/");
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
                    c.Gateway().AddReceiveChannel("http://localhost:25799/SiteB/");
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

        public class MyRequest : ICommand
        {
        }

        public class MyResponse : IMessage
        {
        }
    }
}