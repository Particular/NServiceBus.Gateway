namespace NServiceBus.Gateway.Routing.Sites
{
    using System.Collections.Generic;
    
    interface IRouteMessagesToSites
    {
        IEnumerable<Site> GetDestinationSitesFor(Dictionary<string, string> headers);
    }
}