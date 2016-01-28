namespace NServiceBus.Gateway.HeaderManagement
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class GatewayIncomingBehavior : Behavior<IIncomingPhysicalMessageContext>
    {
        public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            var message = context.Message;

            var headers = message.Headers;
            
            var legacyMessageWithNoReturnCapability = !headers.ContainsKey(Headers.HttpFrom) && !headers.ContainsKey(Headers.OriginatingSite);
            if (legacyMessageWithNoReturnCapability)
            {
                return next();
            }

            string originatingSite;
            headers.TryGetValue(Headers.OriginatingSite, out originatingSite);
            string httpFrom;
            headers.TryGetValue(Headers.HttpFrom, out httpFrom);

            var state = context.Extensions.GetOrCreate<ReturnState>();
            //we preserve the httpFrom to be backwards compatible with NServiceBus 2.X 
            state.HttpFrom = httpFrom;
            state.OriginatingSite = originatingSite;
            state.ReplyToAddress = headers[Headers.ReplyToAddress];
            state.LegacyMode = headers.IsLegacyGatewayMessage();

            return next();
        }

        public class ReturnState
        {
            public string HttpFrom { get; set; }
            public string OriginatingSite { get; set; }
            public string ReplyToAddress { get; set; }
            public bool LegacyMode { get; set; }
        }

        public class Registration : RegisterStep
        {
            public Registration() : base("GatewayIncomingBehavior", typeof(GatewayIncomingBehavior), "Extracts gateway related information from the incoming message")
            {
            }
        }
    }
}