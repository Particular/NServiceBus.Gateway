namespace NServiceBus.Gateway.HeaderManagement
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class GatewayIncomingBehavior : PhysicalMessageProcessingStageBehavior
    {
        public override Task Invoke(Context context, Func<Task> next)
        {
            var message = context.GetPhysicalMessage();

            var headers = message.Headers;
            if (!headers.ContainsKey(Headers.HttpFrom) &&
                !headers.ContainsKey(Headers.OriginatingSite))
            {
                return Task.FromResult(0);
            }

            string originatingSite;
            headers.TryGetValue(Headers.OriginatingSite, out originatingSite);
            string httpFrom;
            headers.TryGetValue(Headers.HttpFrom, out httpFrom);

            var state = context.GetOrCreate<State>();
            //we preserve the httpFrom to be backwards compatible with NServiceBus 2.X 
            state.HttpFrom = httpFrom;
            state.OriginatingSite = originatingSite;
            state.ReplyToAddress = headers[Headers.ReplyToAddress];
            state.LegacyMode = headers.IsLegacyGatewayMessage();

            return next();
        }

        public class State
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