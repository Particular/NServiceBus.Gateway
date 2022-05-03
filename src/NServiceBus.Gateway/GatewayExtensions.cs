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
            if (headers.TryGetValue(GatewayHeaders.LegacyMode, out string legacyModeString))
            {
                bool.TryParse(legacyModeString, out legacyMode);
            }

            return legacyMode;
        }
    }
}