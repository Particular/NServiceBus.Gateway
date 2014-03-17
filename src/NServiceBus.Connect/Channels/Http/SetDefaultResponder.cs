namespace NServiceBus.Connect.Channels.Http
{
    internal class SetDefaultResponder : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run()
        {
            if (!Configure.Instance.Configurer.HasComponent<IHttpResponder>())
            {
                Configure.Instance.Configurer.ConfigureComponent<DefaultResponder>(DependencyLifecycle.InstancePerCall);
            }
        }
    }
}