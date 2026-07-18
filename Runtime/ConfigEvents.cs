namespace SiPVLib.Config
{
    /// <summary>
    /// Broadcast via <c>SiPVLib.Event.EventManager</c> (type-keyed, no string key) whenever a single
    /// <see cref="ConfigLocation"/> finishes initializing — on both success and failure, so a
    /// listener can react to a source-specific failure (retry UI, fallback content) rather than
    /// only ever waiting silently. Fired by <see cref="ConfigManager"/>.
    /// </summary>
    public struct ConfigLocationInitializedEvent
    {
        public ConfigLocation Location;
        public bool Success;
    }

    /// <summary>
    /// Broadcast once, the first time <see cref="ConfigManager.IsFullInitialized"/> becomes true
    /// (i.e. Local/Resources/Addressable/RemoteConfig have all completed their init attempt).
    /// </summary>
    public struct ConfigFullyInitializedEvent
    {
    }
}
