namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Gateway.Routing;
    using Pipeline;
    using Routing;

    class RouteToGatewayBehavior : Behavior<IRoutingContext>
    {
        public RouteToGatewayBehavior(string gatewayAddress)
        {
            this.gatewayAddress = gatewayAddress;
        }

        public override Task Invoke(IRoutingContext context, Func<Task> next)
        {

            if (context.Extensions.TryGet(out RouteThroughGateway _))
            {
                //Hack 133
                context.RoutingStrategies = new[]
                {
                    new UnicastRoutingStrategy(gatewayAddress)
                };

                context.Extensions.Remove<RouteThroughGateway>();
            }

            return next();
        }

        string gatewayAddress;
    }
}