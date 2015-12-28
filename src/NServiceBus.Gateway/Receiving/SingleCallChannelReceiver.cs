namespace NServiceBus.Gateway.Receiving
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Channels;
    using DataBus;
    using Deduplication;
    using Extensibility;
    using HeaderManagement;
    using Logging;
    using Notifications;
    using Sending;
    using Transports;
    using Utils;

    class SingleCallChannelReceiver : IReceiveMessagesFromSites
    {
        public SingleCallChannelReceiver(IChannelFactory channelFactory, IDeduplicateMessages deduplicator,
            DataBusHeaderManager headerManager, GatewayTransaction transaction)
        {
            this.channelFactory = channelFactory;
            this.deduplicator = deduplicator;
            this.headerManager = headerManager;
            this.transaction = transaction;
        }

        public IDataBus DataBus { get; set; }
        public event EventHandler<MessageReceivedOnChannelArgs> MessageReceived = delegate { };

        public void Start(Channel channel, int numberOfWorkerThreads)
        {
            channelReceiver = channelFactory.GetReceiver(channel.Type);
            channelReceiver.DataReceived += DataReceivedOnChannel;
            channelReceiver.Start(channel.Address, numberOfWorkerThreads);
        }

        public void Dispose()
        {
            //Injected at compile time
            DisposeManaged();
        }

        void DisposeManaged()
        {
            if (channelReceiver != null)
            {
                channelReceiver.DataReceived -= DataReceivedOnChannel;
                channelReceiver.Dispose();
            }
        }
        
        void DataReceivedOnChannel(object sender, DataReceivedOnChannelArgs e)
        {
            using (e.Data)
            {
                var callInfo = ChannelReceiverHeaderReader.GetCallInfo(e);

                Logger.DebugFormat("Received message of type {0} for client id: {1}", callInfo.Type, callInfo.ClientId);

                using (var scope = transaction.Scope())
                {
                    switch (callInfo.Type)
                    {
                        case CallType.SingleCallDatabusProperty:
                            HandleDatabusProperty(callInfo);
                            break;
                        case CallType.SingleCallSubmit:
                            HandleSubmit(callInfo);
                            break;
                        default:
                            throw new Exception("Unknown call type: " + callInfo.Type);
                    }
                    scope.Complete();
                }
            }
        }

        void HandleSubmit(CallInfo callInfo)
        {
            using (var stream = new MemoryStream())
            {
                callInfo.Data.CopyTo(stream);
                stream.Position = 0;

                if (callInfo.Md5 != null)
                {
                    Hasher.Verify(stream, callInfo.Md5);
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
                stream.Read(body, 0, body.Length);
                args.Body = body;

                if (deduplicator.DeduplicateMessage(callInfo.ClientId, DateTime.UtcNow, new ContextBag()).GetAwaiter().GetResult())
                {
                    MessageReceived(this, args);
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

        void HandleDatabusProperty(CallInfo callInfo)
        {
            if (DataBus == null)
            {
                throw new InvalidOperationException("Databus transmission received without a configured databus");
            }

            var newDatabusKey = DataBus.Put(callInfo.Data, callInfo.TimeToBeReceived).GetAwaiter().GetResult();
            if (callInfo.Md5 != null)
            {
                using (var databusStream = DataBus.Get(newDatabusKey).GetAwaiter().GetResult())
                {
                    Hasher.Verify(databusStream, callInfo.Md5);
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
                return corrId.Replace("\\0", String.Empty);
            }

            return corrId;
        }


        static ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");

        IChannelFactory channelFactory;
        IDeduplicateMessages deduplicator;
        DataBusHeaderManager headerManager;

        readonly GatewayTransaction transaction;

        IChannelReceiver channelReceiver;

        const string NServiceBus = "NServiceBus.";
        const string Id = "Id";
        
        const string CorrelationId = "CorrelationId";
        const string Recoverable = "Recoverable";
        const string TimeToBeReceived = "TimeToBeReceived";
        static readonly TimeSpan MinimumTimeToBeReceived = TimeSpan.FromSeconds(1);
    }
}