namespace NServiceBus.Gateway.V2.Routing
{
    using System.Collections.Generic;

    public interface IRouteMessagesToSites

    {
        IEnumerable<Site> GetDestinationSitesFor(TransportMessage messageToDispatch);
    }
}