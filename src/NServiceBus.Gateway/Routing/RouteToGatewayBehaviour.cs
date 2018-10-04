namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Gateway.Routing;
    using Pipeline;
    using Routing;

    class RouteToGatewayBehavior : Behavior<IRoutingContext>
    {
        public RouteToGatewayBehavior(string gatewayAddress, List<string> configuredSiteKeys)
        {
            this.gatewayAddress = gatewayAddress;
            this.configuredSiteKeys = configuredSiteKeys;
        }

        public override Task Invoke(IRoutingContext context, Func<Task> next)
        {
            if (context.Extensions.TryGet(out RouteThroughGateway _))
            {
                if (context.Message.Headers.TryGetValue(Headers.DestinationSites, out var siteKeyHeader))
                {
                    var siteKeys = siteKeyHeader.Split(',');

                    var unknownSites = new List<string>();

                    foreach (var siteKey in siteKeys)
                    {
                        if (!configuredSiteKeys.Contains(siteKey))
                        {
                            unknownSites.Add(siteKey);
                        }
                    }

                    if (unknownSites.Count > 0)
                    {
                        throw new Exception($"Sites with keys `{string.Join(",", unknownSites)}` was not found in the list of configured sites. Please make sure to configure it or remove it from the call to `{nameof(SendOptionsExtensions.RouteToSites)}`");
                    }
                }

                //Hack 133
                context.RoutingStrategies = new[]
                {
                    new UnicastRoutingStrategy(gatewayAddress)
                };

                context.Extensions.Remove<RouteThroughGateway>();
            }

            return next();
        }

        List<string> configuredSiteKeys;
        string gatewayAddress;
    }
}