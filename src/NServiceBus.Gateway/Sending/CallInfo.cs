namespace NServiceBus.Gateway.V2.Sending
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class CallInfo
    {
        public string ClientId { get; set; }
        public CallType Type { get; set; }
        public IDictionary<string, string> Headers { get; set; }
        public Stream Data { get; set; }
        public TimeSpan TimeToBeReceived { get; set; }
        public string Md5 { get; set; }
    }
}