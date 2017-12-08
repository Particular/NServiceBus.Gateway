namespace NServiceBus.Gateway.Routing
{
    using Channels;

    /// <summary>
    /// The site class.
    /// </summary>
    public class Site
    {
        /// <summary>
        /// The channel used for the site.
        /// </summary>
        public Channel Channel { get; set; }

        /// <summary>
        /// The site key to use, this goes hand in hand with Bus.SendToSites(key, message).
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// <code>true</code> to set the forwarding mode for this site to use legacy mode.
        /// </summary>
        public bool LegacyMode { get; set; }

        /// <summary>
        /// <code>true</code> to use reverse proxy friendly headers.
        /// </summary>
        public bool UsesReverseProxy { get; set; }
    }
}