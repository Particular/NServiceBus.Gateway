namespace NServiceBus.Gateway.AcceptanceTests
{
    public class GatewayEndpoint : GatewayEndpointWithNoStorage
    {
        public GatewayEndpoint()
        {
            ConfigureStorage = true;
        }
    }
}