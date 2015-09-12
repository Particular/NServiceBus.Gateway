namespace NServiceBus.Gateway.Channels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class ChannelFactory : IChannelFactory
    {
        public IChannelReceiver GetReceiver(string channelType)
        {
            return Activator.CreateInstance(receivers[channelType.ToLower()]) as IChannelReceiver;
        }

        public IChannelSender GetSender(string channelType)
        {
            return Activator.CreateInstance(senders[channelType.ToLower()]) as IChannelSender;
        }

        public void RegisterReceiver(Type receiver)
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

        public void RegisterSender(Type sender)
        {
            var channelTypes =
                sender.GetCustomAttributes(true).OfType<ChannelTypeAttribute>().ToList();
            if (channelTypes.Any())
            {
                channelTypes.ForEach(type => RegisterSender(sender, type.Type));
            }
            else
            {
                RegisterSender(sender, sender.Name.Substring(0, sender.Name.IndexOf("Channel")));
            }
        }

        void RegisterReceiver(Type receiver, string type)
        {
            receivers.Add(type.ToLower(), receiver);
        }

        void RegisterSender(Type sender, string type)
        {
            senders.Add(type.ToLower(), sender);
        }

        Dictionary<string, Type> receivers = new Dictionary<string, Type>();
        Dictionary<string, Type> senders = new Dictionary<string, Type>();
    }
}