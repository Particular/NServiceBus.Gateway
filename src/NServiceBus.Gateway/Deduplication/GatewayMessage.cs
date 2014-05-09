namespace NServiceBus.Connect.Deduplication
{
    using System;

    internal class GatewayMessage
    {
        public string Id { get; set; }
        public DateTime TimeReceived { get; set; }
    }
}