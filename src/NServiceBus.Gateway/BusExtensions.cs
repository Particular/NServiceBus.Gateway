namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Gateways IBus extensions.
    /// </summary>
    public static class BusExtensions
    {
        /// <summary>
        /// Sends the message to all sites with matching site keys registered with the gateway.
        /// The gateway is assumed to be located at the master node. 
        /// </summary>
        /// <param name="bus"></param>
        /// <param name="siteKeys"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static ICallback SendToSites(this IBus bus, IEnumerable<string> siteKeys, object message)
        {
            bus.SetMessageHeader(message, Headers.DestinationSites, string.Join(",", siteKeys.ToArray()));
            
            return bus.Send(Settings.Get<Address>("MasterNode.Address").SubScope("gateway"), message);
        }
    }
}