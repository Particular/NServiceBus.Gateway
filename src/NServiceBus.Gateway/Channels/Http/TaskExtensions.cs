namespace NServiceBus.Gateway.Channels.Http
{
    using System.Threading.Tasks;

    static class TaskExtensions
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Supposed to be no-op")]
        public static void Forget(this Task task)
        {
        }
    }
}