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

                    string unknownSites = null;

                    foreach (var siteKey in siteKeys)
                    {
                        if (!configuredSiteKeys.Contains(siteKey))
                        {
                            if (unknownSites == null)
                            {
                                unknownSites = siteKey;
                            }
                            else
                            {
                                unknownSites += "," + siteKey;
                            }
                        }
                    }

                    if (unknownSites != null)
                    {
                        throw new Exception($"Sites with keys `{unknownSites}` was not found in the list of configured sites. Please make sure to configure it or remove it from the call to `{nameof(SendOptionsExtensions.RouteToSites)}`");
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