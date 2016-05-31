#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global

namespace NServiceBus
{
    [ObsoleteEx(
        ReplacementTypeOrMember = "MessageHandlerContextExtensions",
        RemoveInVersion = "3.0",
        TreatAsErrorFromVersion = "2.0")]
    public static class BusExtensions
    {
    }
}

namespace NServiceBus.Gateway.Routing
{
    [ObsoleteEx(
       Message = "Not used anymore. Use config.Gateway().ChannelFactories() to provide custom channel factories if you want to override the gatways default http implementation.",
       RemoveInVersion = "3.0",
       TreatAsErrorFromVersion = "2.0")]
    public interface IRouteMessagesToEndpoints
    {
    }

    [ObsoleteEx(
        Message = "Not used anymore. Use config.Gateway().ChannelFactories() to provide custom channel factories if you want to override the gatways default http implementation.",
        RemoveInVersion = "3.0",
        TreatAsErrorFromVersion = "2.0")]
    public interface IRouteMessagesToSites
    {
    }
}

namespace NServiceBus.Gateway.Sending
{
    [ObsoleteEx(
         Message = "Not used anymore. Use config.Gateway().ChannelFactories() to provide custom channel factories if you want to override the gatways default http implementation.",
         RemoveInVersion = "3.0",
         TreatAsErrorFromVersion = "2.0")]
    public interface IForwardMessagesToSites
    {
    }
}