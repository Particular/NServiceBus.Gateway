namespace NServiceBus.Gateway.Receiving
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Channels;
    using DataBus;
    using Deduplication;
    using Extensibility;
    using HeaderManagement;
    using Logging;
    using Notifications;
    using Sending;
    using Utils;

    class SingleCallChannelReceiver
    {
        public SingleCallChannelReceiver(Func<string, IChannelReceiver> channelFactory, IDeduplicateMessages deduplicator, IDataBus databus)
        {
            this.channelFactory = channelFactory;
            this.deduplicator = deduplicator;
            this.databus = databus;
            headerManager = new DataBusHeaderManager();
        }

        public void Start(Channel channel, int maxConcurrency, Func<MessageReceivedOnChannelArgs, Task> receivedHandler)
        {
            messageReceivedHandler = receivedHandler;
            channelReceiver = channelFactory(channel.Type);
            channelReceiver.Start(channel.Address, maxConcurrency, DataReceivedOnChannel);
        }

        public Task Stop()
        { 
            return channelReceiver?.Stop();
        }

        async Task DataReceivedOnChannel(DataReceivedOnChannelArgs e)
        {
            using (e.Data)
            {
                var callInfo = ChannelReceiverHeaderReader.GetCallInfo(e);

                Logger.DebugFormat("Received message of type {0} for client id: {1}", callInfo.Type, callInfo.ClientId);

                using (var scope = GatewayTransaction.Scope())
                {
                    switch (callInfo.Type)
                    {
                        case CallType.SingleCallDatabusProperty:
                            await HandleDatabusProperty(callInfo).ConfigureAwait(false);
                            break;
                        case CallType.SingleCallSubmit:
                            await HandleSubmit(callInfo).ConfigureAwait(false);
                            break;
                        default:
                            throw new Exception("Unknown call type: " + callInfo.Type);
                    }
                    scope.Complete();
                }
            }
        }

        async Task HandleSubmit(CallInfo callInfo)
        {
            using (var stream = new MemoryStream())
            {
                await callInfo.Data.CopyToAsync(stream).ConfigureAwait(false);
                stream.Position = 0;

                if (callInfo.Md5 != null)
                {
                    await Hasher.Verify(stream, callInfo.Md5).ConfigureAwait(false);
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
                
                if (IsMsmqTransport)
                {
                    args.Headers[Headers.CorrelationId] = StripSlashZeroFromCorrelationId(args.Headers[Headers.CorrelationId]);
                }
          
                var body = new byte[stream.Length];
                await stream.ReadAsync(body, 0, body.Length).ConfigureAwait(false);
                args.Body = body;

                if (await deduplicator.DeduplicateMessage(callInfo.ClientId, DateTime.UtcNow, new ContextBag()).ConfigureAwait(false))
                {
                    await messageReceivedHandler(args).ConfigureAwait(false);
                }
                else
                {
                    Logger.InfoFormat("Message with id: {0} is already on the bus, dropping the request", callInfo.ClientId);
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
            TimeSpan timeToBeReceived;
            TimeSpan.TryParse(headers[NServiceBus + TimeToBeReceived], out timeToBeReceived);

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
                if (pair.Key.Contains(NServiceBus + Headers.HeaderName))
                {
                    result.Add(pair.Key.Replace(NServiceBus + Headers.HeaderName + ".", string.Empty), pair.Value);
                }
            }

            return result;
        }

        public bool IsMsmqTransport{ get; set; }

        async Task HandleDatabusProperty(CallInfo callInfo)
        {
            if (databus == null)
            {
                throw new InvalidOperationException("Databus transmission received without a configured databus");
            }

            var newDatabusKey = await databus.Put(callInfo.Data, callInfo.TimeToBeReceived).ConfigureAwait(false);
            if (callInfo.Md5 != null)
            {
                using (var databusStream = await databus.Get(newDatabusKey).ConfigureAwait(false))
                {
                   await Hasher.Verify(databusStream, callInfo.Md5).ConfigureAwait(false);
                }
            }

            var specificDataBusHeaderToUpdate = callInfo.ReadDataBus();
            headerManager.InsertHeader(callInfo.ClientId, specificDataBusHeaderToUpdate, newDatabusKey);
        }

        static string StripSlashZeroFromCorrelationId(string corrId)
        {
            if (corrId == null)
            {
                return null;
            }

            if (corrId.EndsWith("\\0"))
            {
                return corrId.Replace("\\0", string.Empty);
            }

            return corrId;
        }


        static ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");

        Func<string, IChannelReceiver> channelFactory;
        IDeduplicateMessages deduplicator;
        readonly IDataBus databus;
        DataBusHeaderManager headerManager;

        IChannelReceiver channelReceiver;

        const string NServiceBus = "NServiceBus.";
        const string Id = "Id";
        
        const string CorrelationId = "CorrelationId";
        const string Recoverable = "Recoverable";
        const string TimeToBeReceived = "TimeToBeReceived";
        static readonly TimeSpan MinimumTimeToBeReceived = TimeSpan.FromSeconds(1);
        Func<MessageReceivedOnChannelArgs, Task> messageReceivedHandler;
    }
}