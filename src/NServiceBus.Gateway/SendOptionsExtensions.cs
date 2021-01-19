namespace NServiceBus
{
    using System;
    using Extensibility;
    using Gateway.Routing;

    /// <summary>
    /// Extensions to <see cref="SendOptions"/> provided by the Gateway.
    /// </summary>
    public static class SendOptionsExtensions
    {
        /// <summary>
        /// Route the message through the Gateway to the specified sites.
        /// </summary>
        public static void RouteToSites(this SendOptions options, params string[] siteKeys)
        {
            options.SetHeader(Headers.DestinationSites, string.Join(",", siteKeys));
            options.GetExtensions().Set(new RouteThroughGateway());
            options.RouteToThisEndpoint();
        }

        /// <summary>
        /// Retrieves the sites configured by <see cref="RouteToSites"/>.
        /// </summary>
        public static string[] GetSitesRoutingTo(this SendOptions options)
        {
            if (options.GetHeaders().TryGetValue(Headers.DestinationSites, out string siteKeys))
            {
                return siteKeys.Split(new[]
                {
                    ","
                }, StringSplitOptions.None);
            }

            return new string[0];
        }
    }
}