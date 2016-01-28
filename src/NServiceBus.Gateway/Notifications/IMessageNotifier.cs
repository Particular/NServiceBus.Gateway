namespace NServiceBus.Gateway.Notifications
{
    using System;
    using System.Collections.Generic;

    interface IMessageNotifier
    {
        event EventHandler<MessageReceivedOnChannelArgs> MessageForwarded;

        void RaiseMessageForwarded(string fromChannel, string toChannel, byte[] messageBody, Dictionary<string, string> headers);
    }
}