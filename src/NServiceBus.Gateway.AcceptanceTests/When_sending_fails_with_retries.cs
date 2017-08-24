namespace NServiceBus.AcceptanceTests.Gateway
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Config;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_sending_fails_with_retries : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_have_been_retried_number_of_times_specified()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<Headquarters>(b =>
                {
                    b.CustomConfig((c, ctx) =>
                        {
                            c.Gateway().Retries(2, TimeSpan.FromSeconds(1));
                            c.Gateway().ChannelFactories(s => new FaultyChannelSender<Context>(ctx), s => new FakeChannelReceiver());
                        })
                        .When((bus, c) => bus.SendToSites(new[]
                        {
                            "SiteA"
                        }, new AnyMessage
                        {
                            Id = c.Id
                        }));
                })
                .WithEndpoint<ErrorSpy>()
                .Done(c => c.MessageMovedToErrorQueue)
                .Run();

            Assert.IsTrue(context.MessageMovedToErrorQueue, "Message was not sent to error queue");
            Assert.AreEqual(2, context.NumberOfRetries, "Incorrect number of retries");
        }

        [Test]
        public async Task Should_use_custom_retry_policy()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<Headquarters>(b =>
                {
                    b.CustomConfig((cfg, ctx) => cfg.Gateway().CustomRetryPolicy((msg, ex, currentRetry) =>
                        {
                            ctx.CustomRetryPolicyWasCalled = true;
                            return TimeSpan.MinValue;
                        }))
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

            Assert.True(context.CustomRetryPolicyWasCalled, "Custom retry policy was not called");
        }

        class Context : ScenarioContext, ICountNumberOfRetries
        {
            public Guid Id { get; set; }
            public bool MessageMovedToErrorQueue { get; set; }
            public bool CustomRetryPolicyWasCalled { get; set; }
            public int NumberOfRetries { get; set; }
        }

        class Headquarters : EndpointConfigurationBuilder
        {
            public Headquarters()
            {
                EndpointSetup<DefaultServer>(c =>
                {
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
                    });

                    c.SendFailedMessagesTo(Conventions.EndpointNamingConvention(typeof(ErrorSpy)));
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
                EndpointSetup<ErrorQueueSpy>();
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