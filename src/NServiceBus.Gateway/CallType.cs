namespace NServiceBus.Gateway
{
    using System;

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
        SingleCallDatabusProperty,

        /// <summary>
        /// Legacy Ack, these are sent by V3 Gateway and are ignored
        /// </summary>
        Ack,

        /// <summary>
        /// Legacy Submit, this is equivalent to SingleCallSubmit
        /// </summary>
        [Obsolete("Legacy use only. Do not use.", false)]
        Submit = SingleCallSubmit
    }
}