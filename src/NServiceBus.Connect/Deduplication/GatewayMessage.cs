namespace NServiceBus.Connect.Deduplication
{
    using System;

    public class GatewayMessage
    {
        public string Id { get; set; }
        public DateTime TimeReceived { get; set; }
    }
}