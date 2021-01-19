namespace NServiceBus.Gateway
{
    /// <summary>
    /// received request type.
    /// </summary>
    public enum CallType
    {
        /// <summary>
        /// Default request type.
        /// </summary>
        SingleCallSubmit,

        /// <summary>
        /// Request type for Databus properties.
        /// </summary>
        SingleCallDatabusProperty
    }
}