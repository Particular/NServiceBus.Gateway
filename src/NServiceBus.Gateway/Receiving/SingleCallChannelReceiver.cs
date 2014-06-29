namespace NServiceBus.Gateway.Receiving
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Channels;
    using DataBus;
    using Deduplication;
    using HeaderManagement;
    using Logging;
    using Notifications;
    using Sending;
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
        public event EventHandler<MessageReceivedOnChannelArgs> MessageReceived;

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

                Hasher.Verify(stream, callInfo.Md5);

                var msg = CreatePhysicalMessage(headerManager.Reassemble(callInfo.ClientId, callInfo.Headers));

                if (IsMsmqTransport)
                {
                    msg.CorrelationId = StripSlashZeroFromCorrelationId(msg.CorrelationId);
                }
          
                msg.Body = new byte[stream.Length];
                stream.Read(msg.Body, 0, msg.Body.Length);

                if (deduplicator.DeduplicateMessage(callInfo.ClientId, DateTime.UtcNow))
                {
                    MessageReceived(this, new MessageReceivedOnChannelArgs { Message = msg });
                }
                else
                {
                    Logger.InfoFormat("Message with id: {0} is already on the bus, dropping the request", callInfo.ClientId);
                }
            }
        }

        static TransportMessage CreatePhysicalMessage(IDictionary<string, string> from)
        {
            if (!from.ContainsKey(GatewayHeaders.IsGatewayMessage))
            {
                var message = new TransportMessage();
                foreach (var header in from)
                {
                    message.Headers[header.Key] = header.Value;
                }

                return message;
            }

            var headers = ExtractHeaders(from);
            var to = new TransportMessage(from[NServiceBus + Id], headers);

            to.CorrelationId = from[NServiceBus + CorrelationId] ?? to.Id;

            bool recoverable;
            if (bool.TryParse(from[NServiceBus + Recoverable], out recoverable))
            {
                to.Recoverable = recoverable;
            }

            TimeSpan timeToBeReceived;
            TimeSpan.TryParse(from[NServiceBus + TimeToBeReceived], out timeToBeReceived);
            to.TimeToBeReceived = timeToBeReceived;

            if (to.TimeToBeReceived < MinimumTimeToBeReceived)
            {
                to.TimeToBeReceived = MinimumTimeToBeReceived;
            }

            return to;
        }

        static Dictionary<string, string> ExtractHeaders(IDictionary<string, string> from)
        {
            var result = new Dictionary<string, string>();

            foreach (var pair in from)
            {
                if (pair.Key.Contains(NServiceBus + Headers.HeaderName))
                {
                    result.Add(pair.Key.Replace(NServiceBus + Headers.HeaderName + ".", String.Empty), pair.Value);
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


            var newDatabusKey = DataBus.Put(callInfo.Data, callInfo.TimeToBeReceived);
            using (var databusStream = DataBus.Get(newDatabusKey))
            {
                Hasher.Verify(databusStream, callInfo.Md5);
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