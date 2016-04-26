namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System.Collections.Generic;

    public class DefaultServerWithCallbacks : DefaultServer
    {
        protected override List<string> AssembliesToExclude { get; } = new List<string>();
    }
}