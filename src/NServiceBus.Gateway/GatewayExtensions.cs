namespace NServiceBus.Gateway
{
    using System.Collections.Generic;
    using HeaderManagement;

    /// <summary>
    ///     extensions internal to the gateway
    /// </summary>
    static class HeaderExtensions
    {
        /// <summary>
        ///     legacy mode support
        /// </summary>
        /// <returns>
        ///     true when message received from gateway other than v4
        ///     or v4 site is configured to forward messages using legacy mode,
        ///     false otherwise
        /// </returns>
        public static bool IsLegacyGatewayMessage(this IDictionary<string, string> headers)
        {
            var legacyMode = true;

            // Gateway v3 would never have sent this header
            string legacyModeString;
            if (headers.TryGetValue(GatewayHeaders.LegacyMode, out legacyModeString))
            {
                bool.TryParse(legacyModeString, out legacyMode);
            }

            return legacyMode;
        }

        /// <summary>
        ///     encode headers for reverse proxy
        /// </summary>
        public static IDictionary<string, string> EncodeHeadersForReverseProxy(this IDictionary<string, string> headers)
        {
            var encodedHeaders = new Dictionary<string, string>();
            foreach (var header in headers)
            {
                encodedHeaders.Add(header.Key.Replace(".","-"),header.Value);
            }

            return encodedHeaders;
        }
    }
}