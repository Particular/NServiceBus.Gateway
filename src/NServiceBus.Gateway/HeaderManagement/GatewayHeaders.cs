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

        //https://github.com/Particular/NServiceBus/blob/689825ba80f1282f1ca39a17dad75d8ab0915dfd/src/NServiceBus.Core/obsoletes-v8.cs#L35
        public const string CoreLegacyHeaderName = "Header";
    }
}