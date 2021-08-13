namespace NServiceBus.Gateway.Receiving
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Channels;
    using DataBus;
    using Extensibility;
    using HeaderManagement;
    using Logging;
    using Notifications;
    using Sending;
    using Utils;

    class SingleCallChannelReceiver
    {
        public SingleCallChannelReceiver(Func<string, IChannelReceiver> channelFactory, IGatewayDeduplicationStorage deduplicationStorage, IDataBus databus, bool useTransactionScope)
        {
            this.channelFactory = channelFactory;
            this.deduplicationStorage = deduplicationStorage;
            this.databus = databus;
            this.useTransactionScope = useTransactionScope;
            headerManager = new DataBusHeaderManager();
        }

        public void Start(Channel channel, int maxConcurrency, Func<MessageReceivedOnChannelArgs, CancellationToken, Task> receivedHandler)
        {
            messageReceivedHandler = receivedHandler;
            channelReceiver = channelFactory(channel.Type);
            channelReceiver.Start(channel.Address, maxConcurrency, DataReceivedOnChannel);
        }

        public Task Stop(CancellationToken cancellationToken = default) =>
            channelReceiver?.Stop(cancellationToken);

        async Task DataReceivedOnChannel(DataReceivedOnChannelEventArgs e, CancellationToken cancellationToken)
        {
            using (e.Data)
            {
                var callInfo = ChannelReceiverHeaderReader.GetCallInfo(e);

                Logger.DebugFormat("Received message of type {0} for client id: {1}", callInfo.Type, callInfo.ClientId);

                if (useTransactionScope)
                {
                    using (var scope = GatewayTransaction.Scope())
                    {
                        await Receive(callInfo).ConfigureAwait(false);
                        scope.Complete();
                    }
                }
                else
                {
                    // create no transaction scope to avoid that only the persistence or the transport enlist with a transaction.
                    // this would cause issues when commiting the transaction fails after the persistence or transport operation has succeeded.
                    await Receive(callInfo).ConfigureAwait(false);
                }
            }

            async Task Receive(CallInfo callInfo)
            {
                switch (callInfo.Type)
                {
                    case CallType.SingleCallDatabusProperty:
                        await HandleDatabusProperty(callInfo, cancellationToken).ConfigureAwait(false);
                        break;
                    case CallType.SingleCallSubmit:
                        await HandleSubmit(callInfo, cancellationToken).ConfigureAwait(false);
                        break;
                    default:
                        throw new Exception("Unknown call type: " + callInfo.Type);
                }
            }
        }

        async Task HandleSubmit(CallInfo callInfo, CancellationToken cancellationToken)
        {
            using (var stream = new MemoryStream())
            {
                // 81920 is the default value in the 1-argument overload
                // .NET Framework has no overload that accepts only Stream + CancellationToken
                await callInfo.Data.CopyToAsync(stream, 81920, cancellationToken).ConfigureAwait(false);
                stream.Position = 0;

                if (callInfo.Md5 != null)
                {
                    await Hasher.Verify(stream, callInfo.Md5, cancellationToken).ConfigureAwait(false);
                }

                var headers = headerManager.ReassembleDataBusProperties(callInfo.ClientId, callInfo.Headers);
                var args = CreateMessageReceivedArgsWithDefaultValues(callInfo.TimeToBeReceived, headers[NServiceBus + Id]);

                var isGatewayMessage = IsGatewayMessage(headers);
                if (isGatewayMessage)
                {
                    args.Headers = MapGatewayMessageHeaders(headers);
                    args.Recoverable = GetRecoverable(headers);
                    args.TimeToBeReceived = GetTimeToBeReceived(headers);
                }
                else
                {
                    args.Headers = MapCustomMessageHeaders(headers);
                }

                var body = new byte[stream.Length];
                await stream.ReadAsync(body, 0, body.Length, cancellationToken).ConfigureAwait(false);
                args.Body = body;

                var context = new ContextBag();

                using (var duplicationCheck = await deduplicationStorage.CheckForDuplicate(callInfo.ClientId, context, cancellationToken).ConfigureAwait(false))
                {
                    if (duplicationCheck.IsDuplicate)
                    {
                        Logger.InfoFormat("Message with id: {0} has already been dispatched, ignoring incoming gateway message.", callInfo.ClientId);
                    }
                    else
                    {
                        await messageReceivedHandler(args, cancellationToken).ConfigureAwait(false);
                        try
                        {
                            await duplicationCheck.MarkAsDispatched(cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception ex) when (!ex.IsCausedBy(cancellationToken) && !useTransactionScope)
                        {
                            // swallow exception in non-dtc modes.
                            // When using no transactions, the message has been sent to the transport already. Throwing would cause the operation to be retried and a guaranteed duplicate to be created. By swallowing the exception, the duplicate is only created if the same message is sent to the gateway for another reason.
                            // When using distributed transactions, throw so that both persistence and transport can rollback atomically.
                            Logger.Warn($"Failed to mark message with id '{callInfo.ClientId}' as dispatched. This message might not be deduplicated.'", ex);
                        }
                    }
                }
            }
        }

        static MessageReceivedOnChannelArgs CreateMessageReceivedArgsWithDefaultValues(TimeSpan defaultTimeToBeReceived, string id)
        {
            return new MessageReceivedOnChannelArgs
            {
                Recoverable = false,
                Id = id,
                TimeToBeReceived = defaultTimeToBeReceived
            };
        }

        static bool IsGatewayMessage(IDictionary<string, string> headers)
        {
            return headers.ContainsKey(GatewayHeaders.IsGatewayMessage);
        }

        static bool GetRecoverable(IDictionary<string, string> headers)
        {
            if (!headers.ContainsKey(NServiceBus + Recoverable))
            {
                return true;
            }
            bool recoverable = bool.TryParse(headers[NServiceBus + Recoverable], out recoverable) && recoverable;
            return recoverable;
        }

        static TimeSpan GetTimeToBeReceived(IDictionary<string, string> headers)
        {
            if (!headers.ContainsKey(NServiceBus + TimeToBeReceived))
            {
                return TimeSpan.MaxValue;
            }
            TimeSpan.TryParse(headers[NServiceBus + TimeToBeReceived], out TimeSpan timeToBeReceived);

            return timeToBeReceived < MinimumTimeToBeReceived ? MinimumTimeToBeReceived : timeToBeReceived;
        }

        static Dictionary<string, string> MapCustomMessageHeaders(IDictionary<string, string> receivedHeaders)
        {
            var headers = new Dictionary<string, string>();

            foreach (var header in receivedHeaders)
            {
                headers[header.Key] = header.Value;
            }

            return headers;
        }

        static Dictionary<string, string> MapGatewayMessageHeaders(IDictionary<string, string> receivedHeaders)
        {
            var gatewayMessageHeaders = ExtractHeaders(receivedHeaders);
            gatewayMessageHeaders[Headers.CorrelationId] = receivedHeaders[NServiceBus + CorrelationId] ?? receivedHeaders[NServiceBus + Id];

            return gatewayMessageHeaders;
        }

        static Dictionary<string, string> ExtractHeaders(IDictionary<string, string> from)
        {
            var result = new Dictionary<string, string>();

            foreach (var pair in from)
            {
                if (pair.Key.Contains(NServiceBus + GatewayHeaders.CoreLegacyHeaderName))
                {
                    result.Add(pair.Key.Replace(NServiceBus + GatewayHeaders.CoreLegacyHeaderName + ".", string.Empty), pair.Value);
                }
            }

            return result;
        }

        async Task HandleDatabusProperty(CallInfo callInfo, CancellationToken cancellationToken)
        {
            if (databus == null)
            {
                throw new InvalidOperationException("Databus transmission received without a configured databus");
            }

            var newDatabusKey = await databus.Put(callInfo.Data, callInfo.TimeToBeReceived, cancellationToken).ConfigureAwait(false);
            if (callInfo.Md5 != null)
            {
                using (var databusStream = await databus.Get(newDatabusKey, cancellationToken).ConfigureAwait(false))
                {
                    await Hasher.Verify(databusStream, callInfo.Md5, cancellationToken).ConfigureAwait(false);
                }
            }

            var specificDataBusHeaderToUpdate = callInfo.ReadDataBus();
            headerManager.InsertHeader(callInfo.ClientId, specificDataBusHeaderToUpdate, newDatabusKey);
        }

        static ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");

        Func<string, IChannelReceiver> channelFactory;
        IGatewayDeduplicationStorage deduplicationStorage;
        IDataBus databus;
        readonly bool useTransactionScope;
        DataBusHeaderManager headerManager;
        IChannelReceiver channelReceiver;

        const string NServiceBus = "NServiceBus.";
        const string Id = "Id";
        const string CorrelationId = "CorrelationId";
        const string Recoverable = "Recoverable";
        const string TimeToBeReceived = "TimeToBeReceived";
        static TimeSpan MinimumTimeToBeReceived = TimeSpan.FromSeconds(1);

        Func<MessageReceivedOnChannelArgs, CancellationToken, Task> messageReceivedHandler;
    }
}