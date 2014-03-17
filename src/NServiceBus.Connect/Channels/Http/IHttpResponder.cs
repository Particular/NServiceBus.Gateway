namespace NServiceBus.Connect.Channels.Http
{
    using System.Net;

    public interface IHttpResponder
    {
        void Handle(HttpListenerContext ctx);
    }
}