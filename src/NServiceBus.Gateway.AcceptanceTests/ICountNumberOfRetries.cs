namespace NServiceBus.Gateway.AcceptanceTests
{
    interface ICountNumberOfRetries
    {
        int NumberOfRetries { get; set; }
    }
}