namespace NServiceBus.Gateway.Installer
{
    using Receiving;

    class InstallerSettings
    {
        public IManageReceiveChannels ChannelManager { get; set; }

        public bool Enabled { get; set; }
    }
}
