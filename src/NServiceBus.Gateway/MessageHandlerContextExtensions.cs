namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Gateways IBus extensions.
    /// </summary>
    public static class MessageHandlerContextExtensions
    {
        /// <summary>
        /// Sends the message to all sites with matching site keys registered with the gateway.
        /// </summary>
        /// <remarks>To use callbacks with SendToSites then use <see cref="SendOptions"/> with <see cref="SendOptionsExtensions.RouteToSites"/></remarks>
        public static Task SendToSites(this IMessageHandlerContext context, IEnumerable<string> siteKeys, object message)
        {
            var options = new SendOptions();
            options.RouteToSites(siteKeys.ToArray());
            return context.Send(message, options);
        }

        /// <summary>
        /// Sends the message to all sites with matching site keys registered with the gateway.
        /// </summary>
        /// <remarks>To use callbacks with SendToSites then use <see cref="SendOptions"/> with <see cref="SendOptionsExtensions.RouteToSites"/></remarks>
        public static Task SendToSites(this IMessageSession context, IEnumerable<string> siteKeys, object message)
        {
            var options = new SendOptions();
            options.RouteToSites(siteKeys.ToArray());
            return context.Send(message, options);
        }
    }
}