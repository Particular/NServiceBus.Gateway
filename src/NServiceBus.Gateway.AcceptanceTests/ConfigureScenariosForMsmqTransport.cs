using System;
using System.Collections.Generic;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.AcceptanceTests.ScenarioDescriptors;

public class ConfigureScenariosForMsmqTransport : IConfigureSupportedScenariosForTestExecution
{
    public IEnumerable<Type> UnsupportedScenarioDescriptorTypes { get; } = new[]
    {
        typeof(AllTransportsWithCentralizedPubSubSupport)
    };
}