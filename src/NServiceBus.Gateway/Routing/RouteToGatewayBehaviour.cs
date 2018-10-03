namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Gateway.Routing;
    using Pipeline;
    using Routing;

    class RouteToGatewayBehavior : Behavior<IRoutingContext>
    {
        public RouteToGatewayBehavior(string gatewayAddress, IList<string> configuredSiteKeys)
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

                    var unknownSites = siteKeys.Where(siteKey => !configuredSiteKeys.Contains(siteKey))
                        .ToList();

                    throw new Exception($"Sites with keys `{string.Join(",", unknownSites)}` was not found in the list of configured sites. Please make sure to configure it or remove it from the call to `{nameof(SendOptionsExtensions.RouteToSites)}`");

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

        string gatewayAddress;
        readonly IList<string> configuredSiteKeys;
    }
}