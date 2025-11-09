namespace NServiceBus.Gateway.Channels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class ChannelSenderFactory
    {
        public ChannelSenderFactory(Type senderType)
        {
            RegisterSender(senderType);
        }

        public IChannelSender GetSender(string channelType)
        {
            return Activator.CreateInstance(senders[channelType.ToLower()]) as IChannelSender;
        }

        void RegisterSender(Type sender)
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

        void RegisterSender(Type sender, string type)
        {
            senders.Add(type.ToLower(), sender);
        }

        readonly Dictionary<string, Type> senders = [];
    }
}