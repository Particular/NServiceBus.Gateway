﻿namespace NServiceBus.AcceptanceTests.Gateway
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Config;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_sending_fails_without_retries : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_move_to_error_queue()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<Headquarters>(b =>
                {
                    b.CustomConfig((c, ctx) => { c.Gateway().ChannelFactories(s => new FaultyChannelSender<Context>(ctx), s => new FakeChannelReceiver()); })
                        .When((bus, ctx) => bus.SendToSites(new[]
                        {
                            "SiteA"
                        }, new AnyMessage
                        {
                            Id = ctx.Id
                        }));
                })
                .WithEndpoint<ErrorSpy>()
                .Done(c => c.MessageMovedToErrorQueue)
                .Run();

            Assert.IsTrue(context.MessageMovedToErrorQueue, "Message was not sent to error queue");
            Assert.AreEqual(0, context.NumberOfRetries, "Message was retried");
        }

        class Context : ScenarioContext, ICountNumberOfRetries
        {
            public Guid Id { get; set; }
            public bool MessageMovedToErrorQueue { get; set; }
            public int NumberOfRetries { get; set; }
        }

        class Headquarters : EndpointConfigurationBuilder
        {
            public Headquarters()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.SendFailedMessagesTo(Conventions.EndpointNamingConvention(typeof(ErrorSpy)));
                  
                    c.EnableGateway(new GatewayConfig
                    {
                        Sites = new SiteCollection
                        {
                            new SiteConfig
                            {
                                Key = "SiteA",
                                Address = "http://localhost:25999/SiteA/",
                                ChannelType = "http"
                            }
                        },
                        Channels = new ChannelCollection
                        {
                            new ChannelConfig
                            {
                                Address = "http://localhost:25999/Headquarters/",
                                ChannelType = "http"
                            }
                        }
                    })
                    .DisableRetries();
                });
            }
        }

        public class AnyMessage : IMessage
        {
            public Guid Id { get; set; }
        }

        class ErrorSpy : EndpointConfigurationBuilder
        {
            public ErrorSpy()
            {
                EndpointSetup<DefaultServer>();
            }

            class ErrorMessageHandler : IHandleMessages<AnyMessage>
            {
                public ErrorMessageHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(AnyMessage errorMessage, IMessageHandlerContext context)
                {
                    testContext.MessageMovedToErrorQueue = true;

                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }
    }
}