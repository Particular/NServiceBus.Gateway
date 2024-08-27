namespace NServiceBus.Gateway.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Configuration.AdvancedExtensibility;
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
                            var gatewaySettings = c.GetSettings().Get<GatewaySettings>();
                            gatewaySettings.Retries(2, TimeSpan.FromSeconds(1));
                            gatewaySettings.ChannelFactories(s => new FaultyChannelSender<Context>(ctx), s => new FakeChannelReceiver());
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

            Assert.Multiple(() =>
            {
                Assert.That(context.MessageMovedToErrorQueue, Is.True, "Message was not sent to error queue");
                Assert.That(context.NumberOfRetries, Is.EqualTo(2), "Incorrect number of retries");
            });
        }

        [Test]
        public async Task Should_use_custom_retry_policy()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<Headquarters>(b =>
                {
                    b.CustomConfig((cfg, ctx) =>
                        {
                            var gatewaySettings = cfg.GetSettings().Get<GatewaySettings>();
                            gatewaySettings.CustomRetryPolicy((msg, ex, currentRetry) =>
                            {
                                ctx.CustomRetryPolicyWasCalled = true;
                                return TimeSpan.MinValue;
                            });
                        })
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

            Assert.That(context.CustomRetryPolicyWasCalled, Is.True, "Custom retry policy was not called");
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
                EndpointSetup<GatewayEndpoint>(c =>
                {
                    var gatewaySettings = c.GetSettings().Get<GatewaySettings>();
                    gatewaySettings.AddReceiveChannel("http://localhost:25999/Headquarters/");
                    gatewaySettings.AddSite("SiteA", "http://localhost:25999/SiteA/");

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
                EndpointSetup<GatewayEndpointWithNoStorage>();
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