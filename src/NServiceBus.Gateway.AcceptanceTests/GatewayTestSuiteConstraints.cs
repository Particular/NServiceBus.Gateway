namespace NServiceBus.Gateway.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;

    public partial class GatewayTestSuiteConstraints : IGatewayTestSuiteConstraints
    {
        public static GatewayTestSuiteConstraints Current = new GatewayTestSuiteConstraints();
    }

    public interface IGatewayTestSuiteConstraints
    {
        IConfigureGatewayPersitenceExecution CreatePersistenceConfiguration();
    }

    public interface IConfigureGatewayPersitenceExecution
    {
        Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings);

        Task Cleanup();
    }
}