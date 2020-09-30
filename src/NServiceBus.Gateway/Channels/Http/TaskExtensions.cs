namespace NServiceBus.Gateway.Channels.Http
{
    using System.Threading.Tasks;

    static class TaskExtensions
    {
        public static void Forget(this Task task)
        {
        }
    }
}