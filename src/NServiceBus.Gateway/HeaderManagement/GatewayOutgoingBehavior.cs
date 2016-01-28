namespace NServiceBus.Gateway.HeaderManagement
{
    using System;
    using System.Threading.Tasks;
    using OutgoingPipeline;
    using Pipeline;

    class GatewayOutgoingBehavior : Behavior<IOutgoingPhysicalMessageContext>
    {
        public override Task Invoke(IOutgoingPhysicalMessageContext context, Func<Task> next)
        {
            GatewayIncomingBehavior.ReturnState returnState;
            var noReturnInfo = !context.Extensions.TryGet(out returnState);

            var correlationIdMissing = !context.Headers.ContainsKey(Headers.CorrelationId);
            var normalSend = context.Headers.ContainsKey(Headers.DestinationSites);
            var legacySend = context.Headers.ContainsKey(Headers.HttpTo);

            if (noReturnInfo || correlationIdMissing || legacySend || normalSend)
            {
                return next();
            }
            
            //handle response message
            context.Headers[Headers.HttpTo] = returnState.HttpFrom;
            context.Headers[Headers.OriginatingSite] = returnState.OriginatingSite;
            if (!context.Headers.ContainsKey(Headers.RouteTo))
            {
                context.Headers[Headers.RouteTo] = returnState.ReplyToAddress;
            }
            // send to be backwards compatible with Gateway 3.X
            context.Headers[GatewayHeaders.LegacyMode] = returnState.LegacyMode.ToString();

            return next();
        }

        public class Registration : RegisterStep
        {
            public Registration() : base("GatewayOutgoingBehavior", typeof(GatewayOutgoingBehavior), "Puts gateway related information on the headers of outgoing messages")
            {
            }
        }
    }
}