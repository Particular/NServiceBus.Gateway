namespace NServiceBus.Gateway.Receiving
{
    using System;

    class ChannelException : Exception
    {
        public ChannelException(int statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }

        public int StatusCode { get; }
    }
}