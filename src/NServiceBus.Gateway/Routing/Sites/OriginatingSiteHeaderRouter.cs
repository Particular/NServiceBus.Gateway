namespace NServiceBus.Gateway.Routing.Sites
{
    using System.Collections.Generic;
    using Channels;

    class OriginatingSiteHeaderRouter
    {
        public static IEnumerable<Site> GetDestinationSitesFor(Dictionary<string, string> headers)
        {
            if (headers.TryGetValue(Headers.OriginatingSite, out string originatingSite))
            {
                yield return new Site
                {
                    Channel = Channel.Parse(originatingSite),
                    Key = "Default reply channel",
                    LegacyMode = headers.IsLegacyGatewayMessage()
                };
            }
        }
    }
}