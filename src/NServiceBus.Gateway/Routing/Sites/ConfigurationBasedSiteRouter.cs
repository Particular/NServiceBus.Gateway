namespace NServiceBus.Gateway.Routing.Sites
{
    using System.Collections.Generic;

    class ConfigurationBasedSiteRouter
    {
        public ConfigurationBasedSiteRouter(IList<Site> configuredSites)
        {
            foreach (var site in configuredSites)
            {
                sites[site.Key] = site;
            }
        }

        public IEnumerable<Site> GetDestinationSitesFor(Dictionary<string, string> headers)
        {
            if (headers.TryGetValue(Headers.DestinationSites, out string destinationSites))
            {
                var siteKeys = destinationSites.Split(',');

                foreach (var siteKey in siteKeys)
                {
                    if (sites.TryGetValue(siteKey, out Site site))
                    {
                        yield return site;
                    }
                }
            }
        }

        IDictionary<string, Site> sites = new Dictionary<string, Site>();
    }
}