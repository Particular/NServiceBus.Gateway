namespace NServiceBus.Gateway.V2.Channels.Http
{
    using System.Net;

    public interface IHttpResponder
    {
        void Handle(HttpListenerContext ctx);
    }
}