namespace NServiceBus.Gateway
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Logging;
    using NServiceBus.Routing;
    using NServiceBus.Transports;

    class ForwardFailedGatewayMessagesToErrorQueueBehavior
    {
        public ForwardFailedGatewayMessagesToErrorQueueBehavior(string localAddress, CriticalError criticalError, IDispatchMessages dispatcher, string errorQueueAddress)
        {
            this.criticalError = criticalError;
            this.dispatcher = dispatcher;
            this.errorQueueAddress = errorQueueAddress;
            this.localAddress = localAddress;
        }

        public async Task Invoke(PushContext context, Func<Task> next)
        {
            try
            {
                await next().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                await ForwardToErrorQueue(context, exception).ConfigureAwait(false);
            }
        }

        async Task ForwardToErrorQueue(PushContext context, Exception exception)
        {
            try
            {
                Logger.Error($"Moving gateway message '{context.MessageId}' from '{localAddress}' to the error queue because processing failed due to an exception:", exception);

                var body = new byte[context.BodyStream.Length];
                await context.BodyStream.ReadAsync(body, 0, body.Length).ConfigureAwait(false);

                var outgoingMessage = new OutgoingMessage(context.MessageId, context.Headers, body);
                var addressTag = new UnicastAddressTag(errorQueueAddress);

                var transportOperations = new TransportOperations(new TransportOperation(outgoingMessage, addressTag));

                await dispatcher.Dispatch(transportOperations, context.Context).ConfigureAwait(false);
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
        IDispatchMessages dispatcher;
        string errorQueueAddress;

        internal const string StepId = "ForwardFailedGatewayMessagesToErrorQueue";
    }
}