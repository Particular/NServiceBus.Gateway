namespace NServiceBus.Gateway.Routing.Endpoints
{
    using System.Collections.Generic;

    class DefaultEndpointRouter : IRouteMessagesToEndpoints
    {
        public string MainInputAddress { get; set; }

        public string GetDestinationFor(Dictionary<string, string> headers)
        {
            return MainInputAddress;
        }
    }
}