namespace NServiceBus.Gateway.Routing.Endpoints
{
    using System.Collections.Generic;

    class EndpointRouter
    {
        public string MainInputAddress { get; set; }

        public string GetDestinationFor(Dictionary<string, string> headers)
        {
            return MainInputAddress;
        }
    }
}