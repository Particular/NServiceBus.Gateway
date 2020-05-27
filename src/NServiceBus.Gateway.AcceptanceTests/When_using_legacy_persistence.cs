namespace NServiceBus.Gateway.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using AcceptanceTesting;
    using Configuration.AdvancedExtensibility;
    using Deduplication;
    using Extensibility;
    using NUnit.Framework;
    using Persistence;

    public class When_using_legacy_storage
    {
        [Test]
        public async Task Should_always_create_transaction_scope()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(e => e
                    .When(s => s.SendToSites(new []{ "SiteA" }, new SomeMessage())))
                .WithEndpoint<EndpointWithLegacyStorage>()
                .Done(c => c.MessageReceived)
                .Run();

            Assert.IsTrue(context.HasAmbientTransaction);
        }

        class Context : ScenarioContext
        {
            public bool HasAmbientTransaction { get; set; }
            public bool MessageReceived { get; set; }
        }

        class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<GatewayEndpoint>(c =>
                {
                    var gatewaySettings = c.GetSettings().Get<GatewaySettings>();
                    gatewaySettings.AddSite("SiteA", "http://localhost:25999/SiteA/");
                    gatewaySettings.AddReceiveChannel("http://localhost:25999/SiteB/");
                });
            }
        }

        class EndpointWithLegacyStorage : EndpointConfigurationBuilder
        {
            public EndpointWithLegacyStorage()
            {
                EndpointSetup<GatewayEndpointWithNoStorage>((configuration, runDescriptor) =>
                {
                    var testContext = runDescriptor.ScenarioContext as Context;
#pragma warning disable CS0618 // Type or member is obsolete
                    configuration.RegisterComponents(r => r.RegisterSingleton(typeof(IDeduplicateMessages), new FakeDeduplicationStorage(testContext)));
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
                    configuration.UsePersistence<FakeDeduplicationPersistence, StorageType.GatewayDeduplication>();
#pragma warning restore CS0618 // Type or member is obsolete
                    var gatewaySettings = configuration.Gateway();
                    gatewaySettings.AddReceiveChannel("http://localhost:25999/SiteA/");
                });
            }

            class SomeMessageHandler : IHandleMessages<SomeMessage>
            {
                Context testContext;

                public SomeMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    testContext.MessageReceived = true;
                    return Task.FromResult(0);
                }
            }
        }

        class SomeMessage : IMessage
        {
        }

        class FakeDeduplicationPersistence : PersistenceDefinition
        {
            public FakeDeduplicationPersistence()
            {
#pragma warning disable CS0618 // Type or member is obsolete
                Supports<StorageType.GatewayDeduplication>(_ => {});
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }

#pragma warning disable CS0618 // Type or member is obsolete
        class FakeDeduplicationStorage : IDeduplicateMessages
#pragma warning restore CS0618 // Type or member is obsolete
        {
            Context testContext;

            public FakeDeduplicationStorage(Context testContext)
            {
                this.testContext = testContext;
            }

            public Task<bool> DeduplicateMessage(string clientId, DateTime timeReceived, ContextBag context)
            {
                testContext.HasAmbientTransaction = Transaction.Current != null;
                return Task.FromResult(true);
            }
        }
    }
}