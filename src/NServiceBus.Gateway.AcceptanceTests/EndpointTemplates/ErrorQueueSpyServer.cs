namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System.Collections.Generic;

    public class ErrorQueueSpyServer : DefaultServer
    {
        protected override List<string> AssembliesToExclude { get; } = new List<string>
        {
            "NServiceBus.Gateway",
            "NServiceBus.Callbacks"
        };
    }
}