namespace NServiceBus.AcceptanceTests.Gateway
{
    interface ICountNumberOfRetries
    {
        int NumberOfRetries { get; set; }
    }
}