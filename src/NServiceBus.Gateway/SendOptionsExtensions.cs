namespace NServiceBus
{
    using Extensibility;
    using Gateway.Sending;

    /// <summary>
    /// 
    /// </summary>
    public static class SendOptionsExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="siteKeys"></param>
        public static void RouteToSites(this SendOptions options, params string[] siteKeys)
        {
            options.SetHeader(Headers.DestinationSites, string.Join(",", siteKeys));
            options.RouteToSatellite("gateway");
        }
    }
}