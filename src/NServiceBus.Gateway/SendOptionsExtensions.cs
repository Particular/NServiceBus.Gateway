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
            // TODO: Is this even correct? bus.Send(unicast.Settings.Get<Address>("MasterNode.Address").SubScope("gateway"), message);
            // TODO: options.RouteToLocalSattelite("gateway");
            options.SetHeader(Headers.DestinationSites, string.Join(",", siteKeys));
            string localAddress = "something";
            options.SetDestination(localAddress + ".gateway"); // TODO JS: fix options.RouteToLocalSattelite("gateway");
        }
    }
}