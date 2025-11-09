namespace NServiceBus.Gateway.AcceptanceTests;

using System.Threading.Tasks;
using AcceptanceTesting.Support;

public interface IGatewayTestSuiteConstraints
{
    GatewaySettings ConfigureGateway(string endpointName, EndpointConfiguration configuration, RunSettings settings);

    Task Cleanup();
}