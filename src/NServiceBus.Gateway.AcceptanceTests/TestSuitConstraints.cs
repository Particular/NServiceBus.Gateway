namespace NServiceBus.Gateway.AcceptanceTests
{
    using System.Runtime.CompilerServices;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Support;

    public partial class TestSuiteConstraints : ITestSuiteConstraints
    {
        public bool SupportsDtc => false;
        public bool SupportsCrossQueueTransactions => false;
        public bool SupportsNativePubSub => true;
        public bool SupportsDelayedDelivery => false;
        public bool SupportsOutbox => false;
        public bool SupportsPurgeOnStartup => false;
        public IConfigureEndpointTestExecution CreateTransportConfiguration() => new ConfigureEndpointAcceptanceTestingTransport(SupportsNativePubSub, SupportsDelayedDelivery);
        public IConfigureEndpointTestExecution CreatePersistenceConfiguration() => new ConfigureEndpointAcceptanceTestingPersistence();

        [ModuleInitializer]
        public static void Initialize() => ITestSuiteConstraints.Current = new TestSuiteConstraints();
    }
}
