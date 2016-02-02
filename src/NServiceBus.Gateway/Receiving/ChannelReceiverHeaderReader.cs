namespace NServiceBus.Gateway.Receiving
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Channels;
    using Channels.Http;
    using HeaderManagement;
    using Sending;

    static class ChannelReceiverHeaderReader
    {
        public static CallInfo GetCallInfo(DataReceivedOnChannelArgs receivedData)
        {
            return new CallInfo
                {
                    ClientId = ReadClientId(receivedData.Headers),
                    TimeToBeReceived = ReadTimeToBeReceived(receivedData.Headers),
                    Type = ReadCallType(receivedData.Headers),
                    Headers = receivedData.Headers,
                    Data = receivedData.Data,
                    Md5 = ReadMd5(receivedData.Headers)
                };
        }

        static TimeSpan ReadTimeToBeReceived(IDictionary<string, string> headers)
        {
            string timeToBeReceivedString;
            if (headers.TryGetValue("NServiceBus.TimeToBeReceived", out timeToBeReceivedString))
            {
                TimeSpan timeToBeReceived;
                if (TimeSpan.TryParse(timeToBeReceivedString, out timeToBeReceived))
                {
                    return timeToBeReceived;
                }
            }
            return TimeSpan.MaxValue;
        }

        public static string ReadMd5(IDictionary<string, string> headers)
        {
            string md5;
            headers.TryGetValue(HttpHeaders.ContentMD5, out md5);

            return md5;
        }

        public static string ReadDataBus(this CallInfo callInfo)
        {
            string dataBus;
            callInfo.Headers.TryGetValue(GatewayHeaders.DatabusKey, out dataBus);

            if (string.IsNullOrWhiteSpace(dataBus))
            {
                throw new ChannelException(400, "Required header '" + GatewayHeaders.DatabusKey + "' missing.");
            }
            return dataBus;
        }

        public static string ReadClientId(IDictionary<string, string> headers)
        {
            string clientIdString;
            headers.TryGetValue(GatewayHeaders.ClientIdHeader, out clientIdString);
            if (string.IsNullOrWhiteSpace(clientIdString))
            {
                throw new ChannelException(400, "Required header '" + GatewayHeaders.ClientIdHeader + "' missing.");
            }
            return clientIdString;
        }

        public static CallType ReadCallType(IDictionary<string, string> headers)
        {
            string callTypeString;
            CallType callType;
            if (!headers.TryGetValue(GatewayHeaders.CallTypeHeader, out callTypeString))
            {
                throw new ChannelException(400, "Required header '" + GatewayHeaders.CallTypeHeader + "' missing.");
            }
            if (!Enum.TryParse(callTypeString, out callType))
            {
                throw new ChannelException(400, $"Invalid CallType '{callTypeString}'. CallTypes supported '{string.Join(", ", Enum.GetValues(typeof(CallType)).Cast<CallType>())}'");
            }
            return callType;
        }
    }
}