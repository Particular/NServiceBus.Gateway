namespace NServiceBus.Gateway.Notifications
{
    using System;
    using System.Collections.Generic;

    class MessageNotifier
    {
        public event EventHandler<MessageReceivedOnChannelArgs> MessageForwarded = (sender, e) => { };

        public void RaiseMessageForwarded(string from, string to, ReadOnlyMemory<byte> messageBody, Dictionary<string, string> headers)
        {
            MessageForwarded(this, new MessageReceivedOnChannelArgs
            {
                FromChannel = from,
                ToChannel = to,
                Body = messageBody,
                Headers = headers,
            });
        }
    }
}