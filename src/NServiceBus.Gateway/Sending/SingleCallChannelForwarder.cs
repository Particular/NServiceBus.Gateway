namespace NServiceBus.Gateway.Sending
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Channels.Http;
    using ClaimCheck;
    using HeaderManagement;
    using Logging;
    using Routing;
    using Utils;

    class ReadOnlyStream : Stream
    {
        ReadOnlyMemory<byte> memory;
        long position;

        public ReadOnlyStream(ReadOnlyMemory<byte> memory)
        {
            this.memory = memory;
            position = 0;
        }

        public override void Flush() => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesToCopy = (int)Math.Min(count, memory.Length - position);

            var destination = buffer.AsSpan().Slice(offset, bytesToCopy);
            var source = memory.Span.Slice((int)position, bytesToCopy);

            source.CopyTo(destination);

            position += bytesToCopy;

            return bytesToCopy;
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => memory.Length;
        public override long Position { get => position; set => position = value; }
    }

    class SingleCallChannelForwarder
    {
        public SingleCallChannelForwarder(Func<string, IChannelSender> senderFactory, IClaimCheck claimCheck)
        {
            this.senderFactory = senderFactory;
            this.claimCheck = claimCheck;
        }

        public async Task Forward(ReadOnlyMemory<byte> body, Dictionary<string, string> headers, Site targetSite, CancellationToken cancellationToken = default)
        {
            var toHeaders = MapToHeaders(headers);

            var channelSender = senderFactory(targetSite.Channel.Type);

            //claimcheck properties have to be available at the receiver site
            //before the body of the message is forwarded on the bus
            await TransmitClaimCheckProperties(channelSender, targetSite, toHeaders, cancellationToken).ConfigureAwait(false);

            using (var messagePayload = new ReadOnlyStream(body))
            {
                await Transmit(channelSender, targetSite, CallType.SingleCallSubmit, toHeaders, messagePayload, cancellationToken).ConfigureAwait(false);
            }
        }

        Dictionary<string, string> MapToHeaders(Dictionary<string, string> fromHeaders)
        {
            var to = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase)
            {
                [NServiceBus + Id] = fromHeaders[Headers.MessageId],
                [NServiceBus + CorrelationId] = fromHeaders[Headers.CorrelationId]
            };

            if (fromHeaders.ContainsKey(Headers.TimeToBeReceived))
            {
                to.Add(NServiceBus + TimeToBeReceived, fromHeaders[Headers.TimeToBeReceived]);
            }

            if (fromHeaders.TryGetValue(Headers.ReplyToAddress, out string reply)) //Handles SendOnly endpoints, where ReplyToAddress is not set
            {
                to[NServiceBus + ReplyToAddress] = reply;
            }

            if (fromHeaders.TryGetValue(ReplyToAddress, out string replyToAddress))
            {
                to[Headers.RouteTo] = replyToAddress;
            }

            fromHeaders.ToList()
                .ForEach(header => to[NServiceBus + GatewayHeaders.CoreLegacyHeaderName + "." + header.Key] = header.Value);

            return to;
        }

        async Task Transmit(IChannelSender channelSender, Site targetSite, CallType callType,
            IDictionary<string, string> headers, Stream data, CancellationToken cancellationToken)
        {
            headers[GatewayHeaders.IsGatewayMessage] = bool.TrueString;
            headers["NServiceBus.CallType"] = Enum.GetName(typeof(CallType), callType);
            headers[HttpHeaders.ContentMD5] = await Hasher.Hash(data, cancellationToken).ConfigureAwait(false);

            Logger.DebugFormat("Sending message - {0} to: {1}", callType, targetSite.Channel.Address);

            await channelSender.Send(targetSite.Channel.Address, headers, data, cancellationToken).ConfigureAwait(false);
        }

        async Task TransmitClaimCheckProperties(IChannelSender channelSender, Site targetSite,
            IDictionary<string, string> headers, CancellationToken cancellationToken)
        {
            var headersToSend = new Dictionary<string, string>(headers);

            foreach (
                var headerKey in headers.Keys.Where(headerKey => headerKey.Contains("NServiceBus.DataBus.")))
            {
                if (claimCheck == null)
                {
                    throw new InvalidOperationException(
                        "Can't send a message with a claimcheck property without an implementation of the claimcheck pattern configured");
                }

                headersToSend[GatewayHeaders.DatabusKey] = headerKey;

                var claimCheckKeyForThisProperty = headers[headerKey];

                using (var stream = await claimCheck.Get(claimCheckKeyForThisProperty, cancellationToken).ConfigureAwait(false))
                {
                    await Transmit(channelSender, targetSite, CallType.SingleCallDatabusProperty, headersToSend, stream, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        const string NServiceBus = "NServiceBus.";
        const string Id = "Id";

        const string CorrelationId = "CorrelationId";
        const string ReplyToAddress = "ReplyToAddress";
        const string TimeToBeReceived = "TimeToBeReceived";

        static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");
        Func<string, IChannelSender> senderFactory;
        IClaimCheck claimCheck;
    }
}