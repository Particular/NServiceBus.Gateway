namespace NServiceBus.Gateway.HeaderManagement
{
    using System;
    using System.Threading.Tasks;
    using OutgoingPipeline;
    using Pipeline;

    class GatewayOutgoingBehavior : Behavior<IOutgoingPhysicalMessageContext>
    {
        public override async Task Invoke(IOutgoingPhysicalMessageContext context, Func<Task> next)
        {
            GatewayIncomingBehavior.State state;
            if (!context.Extensions.TryGet(out state))
            {
                await next().ConfigureAwait(false);
            }

            context.Headers[Headers.HttpTo] = state.HttpFrom;
            context.Headers[Headers.OriginatingSite] = state.OriginatingSite;
            // TODO: Discuss, is it safe to always set this?
            context.Headers[Headers.RouteTo] = state.ReplyToAddress;
            // send to be backwards compatible with Gateway 3.X
            context.Headers[GatewayHeaders.LegacyMode] = state.LegacyMode.ToString();

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