namespace NServiceBus.AcceptanceTests.Gateway
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Features;
    using NServiceBus.Gateway;
    using NUnit.Framework;
    using static System.Int32;

    public class When_sending_fails_with_retries : NServiceBusAcceptanceTest
    {
        [Test, Explicit]
        public async Task Should_have_been_retried_using_defaults()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<Headquarters>(b =>
                {
                    b.CustomConfig((c, ctx) =>
                    {
                        c.Gateway().ChannelFactories(s => new FaultyChannelSender(ctx), s => new FakeChannelReceiver());
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
                .Run(TimeSpan.FromMinutes(11));

            Assert.IsTrue(context.MessageMovedToErrorQueue, "Message was not sent to error queue");
            Assert.AreEqual(4, context.NumberOfRetries, "Incorrect number of retries");
        }

        [Test]
        public async Task Should_have_been_retried_number_of_times_specified()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<Headquarters>(b =>
                {
                    b.CustomConfig((c, ctx) =>
                    {
                        c.Gateway().Retries(2, TimeSpan.FromSeconds(1));
                        c.Gateway().ChannelFactories(s => new FaultyChannelSender(ctx), s => new FakeChannelReceiver());
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

        const string ErrorSpyQueueName = "gw_error_spy_queue";

        class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public bool MessageMovedToErrorQueue { get; set; }
            public int NumberOfRetries { get; set; }
            public bool CustomRetryPolicyWasCalled { get; set; }
        }

        class Headquarters : EndpointConfigurationBuilder
        {
            public Headquarters()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnableFeature<Gateway>();
                    c.EnableFeature<TimeoutManager>();
                    c.SendFailedMessagesTo(ErrorSpyQueueName);
                })
                    .WithConfig<GatewayConfig>(c =>
                    {
                        c.Sites = new SiteCollection
                        {
                            new SiteConfig
                            {
                                Key = "SiteA",
                                Address = "http://localhost:25999/SiteA/",
                                ChannelType = "http"
                            }
                        };

                        c.Channels = new ChannelCollection
                        {
                            new ChannelConfig
                            {
                                Address = "http://localhost:25999/Headquarters/",
                                ChannelType = "http"
                            }
                        };
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
                EndpointSetup<ErrorQueueSpyServer>()
                    .CustomEndpointName(ErrorSpyQueueName);
            }

            class ErrorMessageHandler : IHandleMessages<AnyMessage>
            {
                public ErrorMessageHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(AnyMessage errorMessage, IMessageHandlerContext context)
                {
                    if (errorMessage.Id == testContext.Id)
                    {
                        testContext.MessageMovedToErrorQueue = true;
                    }

                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        class FaultyChannelSender : IChannelSender
        {
            public FaultyChannelSender(Context testContext)
            {
                this.testContext = testContext;
            }

            public Task Send(string remoteAddress, IDictionary<string, string> headers, Stream data)
            {
                if (headers.ContainsKey(FullRetriesHeaderKey))
                {
                    testContext.NumberOfRetries = Parse(headers[FullRetriesHeaderKey]);
                }
                throw new SimulatedException($"Simulated error when sending to site at {remoteAddress}");
            }

            Context testContext;

            static readonly string FullRetriesHeaderKey = $"NServiceBus.Header.{Headers.Retries}";
        }

        class FakeChannelReceiver : IChannelReceiver
        {
            public void Start(string address, int maxConcurrency, Func<DataReceivedOnChannelArgs, Task> dataReceivedOnChannel)
            {
            }

            public Task Stop()
            {
                return Task.FromResult(0);
            }
        }
    }
}