namespace SoulHunter.Legacy.Events
{
    /// <summary>Compatibility adapter; canonical event diagnostics live in Foundation.</summary>
    [System.Obsolete("Use SoulHunter.Core.Events.EventDebugger.")]
    public static class EventDebugger
    {
        public static bool EnableLogs
        {
            get => SoulHunter.Core.Events.EventDebugger.EnableLogs;
            set => SoulHunter.Core.Events.EventDebugger.EnableLogs = value;
        }
        public static void LogPublish<T>(int listenerCount) where T : SoulHunter.Core.Events.IGameEvent
            => SoulHunter.Core.Events.EventDebugger.LogPublish<T>(listenerCount);
    }
}
