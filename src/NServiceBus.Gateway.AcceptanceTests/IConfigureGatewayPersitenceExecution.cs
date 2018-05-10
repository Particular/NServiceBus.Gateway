namespace NServiceBus.Gateway.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;

    public interface IConfigureGatewayPersitenceExecution
    {
        Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings);

        Task Cleanup();
    }
}