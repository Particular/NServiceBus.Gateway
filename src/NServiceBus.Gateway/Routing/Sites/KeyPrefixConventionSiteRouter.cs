namespace NServiceBus.Gateway.Routing.Sites
{
    using System.Collections.Generic;
    using Channels;

    class KeyPrefixConventionSiteRouter 
    {
        public static IEnumerable<Site> GetDestinationSitesFor(Dictionary<string, string> headers)
        {
            string sites;
            if (headers.TryGetValue(Headers.DestinationSites, out sites))
            {
                var siteKeys = sites.Split(',');

                foreach (var siteKey in siteKeys)
                {
                    var parts = siteKey.Split(':');

                    if (parts.Length >= 2)
                    {
                        yield return new Site
                        {
                            Channel = new Channel
                                {
                                    Address = siteKey, 
                                    Type = parts[0]
                                },
                            Key = siteKey
                        };
                    }
                }
            }
        }
    }
}