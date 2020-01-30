namespace NServiceBus.Gateway.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting.Support;

    public interface IGatewayTestSuiteConstraints
    {
        /// <summary>
        /// Return the GatewayDeduplicationConfiguration only. The test infrastructure will call endpoingConfig.Gateway(…)
        /// </summary>
        Task<GatewayDeduplicationConfiguration> ConfigureDeduplicationStorage(string endpointName, EndpointConfiguration configuration, RunSettings settings);

        Task Cleanup();
    }
}
