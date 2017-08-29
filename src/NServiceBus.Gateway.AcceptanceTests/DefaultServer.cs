namespace NServiceBus.Gateway.AcceptanceTests
{
    public class DefaultServer : DefaultServerWithNoStorage
    {
        public DefaultServer()
        {
            ConfigureStorage = true;
        }
    }
}