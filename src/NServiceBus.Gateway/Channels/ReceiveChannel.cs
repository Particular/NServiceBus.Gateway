namespace NServiceBus.Gateway.Channels
{
    class ReceiveChannel : Channel
    {
        public int MaxConcurrency { get; set; }
        public bool Default { get; set; }

        public override string ToString()
        {
            return $"{base.ToString()}MaxConcurrency={MaxConcurrency}Default={Default}";
        }
    }
}