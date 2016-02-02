namespace NServiceBus.Gateway.Channels
{
    using System;

    /// <summary>
    /// Defines the channel types a <see cref="IChannelReceiver"/> or <see cref="IChannelSender"/> supports.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ChannelTypeAttribute : Attribute
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="type">The type to register.</param>
        public ChannelTypeAttribute(string type)
        {
            Type = type;
        }

        /// <summary>
        /// The type to register.
        /// </summary>
        public string Type { get; set; }
    }
}