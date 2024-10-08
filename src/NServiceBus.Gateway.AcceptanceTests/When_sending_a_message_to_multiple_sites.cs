﻿namespace NServiceBus.Gateway.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvancedExtensibility;
    using NUnit.Framework;

    public class When_sending_a_message_to_multiple_sites : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_able_to_reply_to_messages_from_all_sites()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Headquarters>(b => b.When(async (bus, c) => await bus.SendToSites(new[]
                {
                    "SiteA",
                    "SiteB"
                }, new MyRequest())))
                .WithEndpoint<SiteA>()
                .WithEndpoint<SiteB>()
                .Done(c => c.GotResponseBackFromSiteA && c.GotResponseBackFromSiteB)
                .Run();

            Assert.Multiple(() =>
            {
                Assert.That(context.GotResponseBackFromSiteA, Is.True);
                Assert.That(context.GotResponseBackFromSiteB, Is.True);
            });
        }

        public class Context : ScenarioContext
        {
            public bool GotResponseBackFromSiteA { get; set; }
            public bool GotResponseBackFromSiteB { get; set; }
        }

        public class Headquarters : EndpointConfigurationBuilder
        {
            public Headquarters()
            {
                EndpointSetup<GatewayEndpoint>(c =>
                {
                    var gatewaySettings = c.GetSettings().Get<GatewaySettings>();
                    gatewaySettings.AddReceiveChannel("http://localhost:26000/Headquarters/");
                    gatewaySettings.AddSite("SiteA", "http://localhost:26000/SiteA/");
                    gatewaySettings.AddSite("SiteB", "http://localhost:26000/SiteB/");
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
                    switch (response.Message)
                    {
                        case "SiteA":
                            testContext.GotResponseBackFromSiteA = true;
                            break;
                        case "SiteB":
                            testContext.GotResponseBackFromSiteB = true;
                            break;
                        default:
                            throw new Exception($"Got a response from an unknown site, {response.Message}");
                    }
                    return Task.FromResult(0);
                }
            }
        }

        public class SiteA : EndpointConfigurationBuilder
        {
            public SiteA()
            {
                EndpointSetup<GatewayEndpoint>(c =>
                {
                    var gatewaySettings = c.GetSettings().Get<GatewaySettings>();
                    gatewaySettings.AddReceiveChannel("http://localhost:26000/SiteA/");
                });
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public Task Handle(MyRequest request, IMessageHandlerContext context)
                {
                    return context.Reply(new MyResponse { Message = "SiteA" });
                }
            }
        }

        public class SiteB : EndpointConfigurationBuilder
        {
            public SiteB()
            {
                EndpointSetup<GatewayEndpoint>(c =>
                {
                    var gatewaySettings = c.GetSettings().Get<GatewaySettings>();
                    gatewaySettings.AddReceiveChannel("http://localhost:26000/SiteB/");
                });
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                public Task Handle(MyRequest request, IMessageHandlerContext context)
                {
                    return context.Reply(new MyResponse { Message = "SiteB" });
                }
            }
        }

        public class MyRequest : IMessage
        {
        }

        public class MyResponse : IMessage
        {
            public string Message { get; set; }
        }
    }
}