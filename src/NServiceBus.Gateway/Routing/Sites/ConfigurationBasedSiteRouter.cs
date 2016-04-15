namespace NServiceBus.Gateway.Routing.Sites
{
    using System.Collections.Generic;

    class ConfigurationBasedSiteRouter : IRouteMessagesToSites
    {
        public ConfigurationBasedSiteRouter(IDictionary<string, Site> sites)
        {
            this.sites = sites;
        }

        public IEnumerable<Site> GetDestinationSitesFor(Dictionary<string, string> headers)
        {
            string destinationSites;
            if (headers.TryGetValue(Headers.DestinationSites, out destinationSites))
            {
                var siteKeys = destinationSites.Split(',');

                foreach (var siteKey in siteKeys)
                {
                    Site site;
                    if (sites.TryGetValue(siteKey, out site))
                    {
                        yield return site;
                    }
                }
            }
        }

        IDictionary<string, Site> sites;
    }
}