namespace NServiceBus.Gateway;

using Settings;

/// <summary>
/// Configures the deduplication storage.
/// </summary>
public abstract class GatewayDeduplicationConfiguration
{
    /// <summary>
    /// Called when the deduplication implementation should enable its feature.
    /// </summary>
    protected internal abstract void EnableFeature(SettingsHolder settings);
}