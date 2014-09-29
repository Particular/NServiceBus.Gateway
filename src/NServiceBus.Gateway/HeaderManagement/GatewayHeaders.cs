namespace NServiceBus.Gateway.HeaderManagement
{
    class GatewayHeaders
    {
        public const string AutoAck = "NServiceBus.AutoAck";
        public const string DatabusKey = "NServiceBus.Gateway.DataBusKey";

        public const string IsGatewayMessage = "NServiceBus.Gateway";

        public const string CallTypeHeader = "NServiceBus.CallType";

        public const string ClientIdHeader = "NServiceBus.Id";

        public const string LegacyMode = "NServiceBus.Gateway.LegacyMode";
    }
}