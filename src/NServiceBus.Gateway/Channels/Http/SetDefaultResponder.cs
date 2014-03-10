namespace NServiceBus.Gateway.V2.Channels.Http
{
    public class SetDefaultResponder : IWantToRunBeforeConfigurationIsFinalized
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