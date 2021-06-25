namespace NServiceBus
{
    class GatewayReplyUri
    {
        public string Type { get; }
        public string Address { get; }

        internal GatewayReplyUri(string type, string address)
        {
            Type = type;
            Address = address;
        }

        public override string ToString()
        {
            return $"{Type},{Address}";
        }
    }
}
