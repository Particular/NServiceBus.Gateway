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

    public class When_sending_fails_without_retries : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_move_to_error_queue()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<Headquarters>(b =>
                {
                    b.CustomConfig((c, ctx) =>
                    {
                        c.Gateway().ChannelFactories(s => new FaultyChannelSender(ctx), s => new FakeChannelReceiver());
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

            Assert.IsTrue(context.MessageMovedToErrorQueue, "Message was not sent to error queue");
            Assert.AreEqual(0, context.NumberOfRetries, "Message was retried");
        }

        const string ErrorSpyQueueName = "gw_error_spy_queue";

        class Context : ScenarioContext
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
                    c.EnableFeature<Gateway>();
                    c.SendFailedMessagesTo(ErrorSpyQueueName);
                    c.Gateway().DisableRetries();
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
                Context testContext;

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
