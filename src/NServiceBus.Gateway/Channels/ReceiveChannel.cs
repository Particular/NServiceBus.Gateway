namespace NServiceBus.Gateway.Channels
{
    class ReceiveChannel : Channel
    {
        public int NumberOfWorkerThreads { get; set; }
        public bool Default { get; set; }

        public override string ToString()
        {
            return base.ToString() + "NumberOfWorkerThreads=" + NumberOfWorkerThreads + "Default=" + Default;
        }
    }
}