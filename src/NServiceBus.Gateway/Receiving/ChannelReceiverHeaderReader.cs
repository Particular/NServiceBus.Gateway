namespace NServiceBus.Gateway.Receiving
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Channels.Http;
    using HeaderManagement;
    using Sending;

    static class ChannelReceiverHeaderReader
    {
        public static CallInfo GetCallInfo(DataReceivedOnChannelEventArgs receivedData)
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
            if (headers.TryGetValue("NServiceBus.TimeToBeReceived", out string timeToBeReceivedString))
            {
                if (TimeSpan.TryParse(timeToBeReceivedString, out TimeSpan timeToBeReceived))
                {
                    return timeToBeReceived;
                }
            }
            return TimeSpan.MaxValue;
        }

        public static string ReadMd5(IDictionary<string, string> headers)
        {
            headers.TryGetValue(HttpHeaders.ContentMD5, out string md5);

            return md5;
        }

        public static string ReadDataBus(this CallInfo callInfo)
        {
            callInfo.Headers.TryGetValue(GatewayHeaders.DatabusKey, out string dataBus);

            if (string.IsNullOrWhiteSpace(dataBus))
            {
                throw new ChannelException(400, "Required header '" + GatewayHeaders.DatabusKey + "' missing.");
            }
            return dataBus;
        }

        public static string ReadClientId(IDictionary<string, string> headers)
        {
            headers.TryGetValue(GatewayHeaders.ClientIdHeader, out string clientIdString);
            if (string.IsNullOrWhiteSpace(clientIdString))
            {
                throw new ChannelException(400, "Required header '" + GatewayHeaders.ClientIdHeader + "' missing.");
            }
            return clientIdString;
        }

        public static CallType ReadCallType(IDictionary<string, string> headers)
        {
            if (!headers.TryGetValue(GatewayHeaders.CallTypeHeader, out string callTypeString))
            {
                throw new ChannelException(400, "Required header '" + GatewayHeaders.CallTypeHeader + "' missing.");
            }
            if (!Enum.TryParse(callTypeString, out CallType callType))
            {
                throw new ChannelException(400, $"Invalid CallType '{callTypeString}'. CallTypes supported '{string.Join(", ", Enum.GetValues(typeof(CallType)).Cast<CallType>())}'");
            }
            return callType;
        }
    }
}