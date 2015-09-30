namespace NServiceBus.Gateway.HeaderManagement
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Pipeline.Contexts;
    using TransportDispatch;

    class GatewayOutgoingBehavior : Behavior<OutgoingContext>
    {
        public override Task Invoke(OutgoingContext context, Func<Task> next)
        {
            GatewayIncomingBehavior.State state;
            if (!context.TryGet(out state))
            {
                return Task.FromResult(0);
            }

            // TODO: really required?
            //var headers = context.OutgoingHeaders;
            //if (string.IsNullOrEmpty(headers[Headers.CorrelationId]))
            //{
            //    return Task.FromResult(0);
            //}

            //if (headers.ContainsKey(Headers.HttpTo) ||
            //    headers.ContainsKey(Headers.DestinationSites))
            //{
            //    return Task.FromResult(0);
            //}

            context.SetHeader(Headers.HttpTo, state.HttpFrom);
            context.SetHeader(Headers.OriginatingSite, state.OriginatingSite);

            // TODO: really required?
            //if (!headers.ContainsKey(Headers.RouteTo))
            //{
            //    headers[Headers.RouteTo] = state.ReplyToAddress;
            //}

            //// send to be backwards compatible with Gateway 3.X
            //headers[GatewayHeaders.LegacyMode] = state.LegacyMode.ToString();

            return Task.FromResult(0);
        }

        public class Registration : RegisterStep
        {
            public Registration() : base("GatewayOutgoingBehavior", typeof(GatewayOutgoingBehavior), "Puts gateway related information on the headers of outgoing messages")
            {
            }
        }
    }
}