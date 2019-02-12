namespace NServiceBus.Gateway.Tests
{
    using NServiceBus;
    using NUnit.Framework;
    using Particular.Approvals;
    using PublicApiGenerator;

    [TestFixture]
    public class APIApprovals
    {

        [Test]
        public void Approve()
        {
#if NETFRAMEWORK
            var targetFramework = "netframework";
#else
            var targetFramework = "netstandard";
#endif
            var publicApi = ApiGenerator.GeneratePublicApi(typeof(GatewaySettings).Assembly);
            Approver.Verify(publicApi, scenario: targetFramework);
        }
    }
}