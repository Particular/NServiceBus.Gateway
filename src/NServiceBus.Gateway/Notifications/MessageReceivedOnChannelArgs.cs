namespace NServiceBus.Gateway.V2.Notifications
{
    using System;

    public class MessageReceivedOnChannelArgs : EventArgs
    {
        public TransportMessage Message { get; set; }
        public string FromChannel { get; set; }
        public string ToChannel { get; set; }
    }
}