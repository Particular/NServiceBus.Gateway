namespace NServiceBus.Gateway.HeaderManagement
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Pipeline.Contexts;
    using TransportDispatch;

    class GatewayOutgoingBehavior : Behavior<OutgoingContext>
    {
        public override async Task Invoke(OutgoingContext context, Func<Task> next)
        {
            GatewayIncomingBehavior.State state;
            if (!context.TryGet(out state))
            {
                await next().ConfigureAwait(false);
            }

            context.SetHeader(Headers.HttpTo, state.HttpFrom);
            context.SetHeader(Headers.OriginatingSite, state.OriginatingSite);
            // TODO: Discuss, is it safe to always set this?
            context.SetHeader(Headers.RouteTo, state.ReplyToAddress);
            // send to be backwards compatible with Gateway 3.X
            context.SetHeader(GatewayHeaders.LegacyMode, state.LegacyMode.ToString());

            await next().ConfigureAwait(false);
        }

        public class Registration : RegisterStep
        {
            public Registration() : base("GatewayOutgoingBehavior", typeof(GatewayOutgoingBehavior), "Puts gateway related information on the headers of outgoing messages")
            {
            }
        }
    }
}