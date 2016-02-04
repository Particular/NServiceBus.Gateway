namespace NServiceBus.Gateway.Channels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class ChannelReceiverFactory
    {
        public ChannelReceiverFactory(Type receiverType)
        {
            RegisterReceiver(receiverType);
        }

        public IChannelReceiver GetReceiver(string channelType)
        {
            return Activator.CreateInstance(receivers[channelType.ToLower()]) as IChannelReceiver;
        }

        void RegisterReceiver(Type receiver)
        {
            var channelTypes =
                receiver.GetCustomAttributes(true).OfType<ChannelTypeAttribute>().ToList();
            if (channelTypes.Any())
            {
                channelTypes.ForEach(type => RegisterReceiver(receiver, type.Type));
            }
            else
            {
                RegisterReceiver(receiver, receiver.Name.Substring(0, receiver.Name.IndexOf("Channel")));
            }
        }

        void RegisterReceiver(Type receiver, string type)
        {
            receivers.Add(type.ToLower(), receiver);
        }

        Dictionary<string, Type> receivers = new Dictionary<string, Type>();
    }
}