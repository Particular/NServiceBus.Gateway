namespace NServiceBus.Gateway.Routing.Endpoints
{
    class EndpointRouter
    {
        public string MainInputAddress { get; set; }

        public string GetDestinationFor()
        {
            return MainInputAddress;
        }
    }
}