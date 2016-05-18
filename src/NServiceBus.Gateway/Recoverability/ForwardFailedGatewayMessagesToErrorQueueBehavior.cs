namespace NServiceBus.Gateway
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    class ForwardFailedGatewayMessagesToErrorQueueBehavior : ForkConnector<ITransportReceiveContext, IFaultContext>
    {
        public ForwardFailedGatewayMessagesToErrorQueueBehavior(string localAddress, CriticalError criticalError)
        {
            this.criticalError = criticalError;
            this.localAddress = localAddress;
        }

        public override async Task Invoke(ITransportReceiveContext context, Func<Task> next, Func<IFaultContext, Task> fork)
        {
            try
            {
                await next().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                await ForwardToErrorQueue(context, exception, fork).ConfigureAwait(false);
            }
        }

        async Task ForwardToErrorQueue(ITransportReceiveContext context, Exception exception, Func<IFaultContext, Task> fork)
        {
            try
            {
                var message = context.Message;
                Logger.Error($"Moving gateway message '{message.MessageId}' from '{localAddress}' to the error queue because processing failed due to an exception:", exception);

                var outgoingMessage = new OutgoingMessage(message.MessageId, message.Headers, message.Body);

                var faultContext = this.CreateFaultContext(context, outgoingMessage, localAddress, exception);

                await fork(faultContext).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                criticalError.Raise("Failed to forward failed gateway message to error queue", ex);
                throw;
            }
        }

        CriticalError criticalError;

        string localAddress;

        static ILog Logger = LogManager.GetLogger<ForwardFailedGatewayMessagesToErrorQueueBehavior>();

        internal const string StepId = "ForwardFailedGatewayMessagesToErrorQueue";
    }
}