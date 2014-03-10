namespace NServiceBus.Gateway.V2.Deduplication
{
    using System;

    public class GatewayMessage
    {
        public string Id { get; set; }
        public DateTime TimeReceived { get; set; }
    }
}