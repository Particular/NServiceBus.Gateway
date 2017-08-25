namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    public class DefaultServer : DefaultServerWithNoStorage
    {
        public DefaultServer()
        {
            ConfigureStorage = true;
        }
    }
}