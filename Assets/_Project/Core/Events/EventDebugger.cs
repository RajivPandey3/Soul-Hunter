using UnityEngine;

namespace SoulHunter.Core.Events
{
    /// <summary>
    /// Central debug utility for EventBus.
    /// Automatically disabled in Release builds.
    /// </summary>
    public static class EventDebugger
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

        public static bool EnableLogs = true;

#else

        public static bool EnableLogs = false;

#endif

        public static void LogPublish<T>(int listenerCount)
            where T : IGameEvent
        {
            if (!EnableLogs)
                return;

            Debug.Log(
                $"[EVENT] {typeof(T).Name} | Listeners: {listenerCount}");
        }
    }
}