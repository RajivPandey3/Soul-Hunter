using System.Diagnostics;

namespace SoulHunter.Core.Services
{
    /// <summary>
    /// Routine gameplay messages (a level-up's steps, an upgrade, a chest's gold). Hang hunt (2026-10-16): every
    /// Debug.Log is a synchronous write to the player log on the main thread, a run wrote about 1,500 of them (13,824
    /// LevelUpManager lines in 12 Level 5 runs), and writes on this laptop's full D: drive stalled up to 735 ms. A frozen
    /// run's last line was such a message. These calls compile only in the Unity Editor; player builds skip them, and their
    /// arguments are not even built. Warnings and errors still use Debug.LogWarning / Debug.LogError everywhere.
    /// </summary>
    public static class GameLog
    {
        [Conditional("UNITY_EDITOR")]
        public static void Info(string message) => UnityEngine.Debug.Log(message);

        [Conditional("UNITY_EDITOR")]
        public static void Info(string message, UnityEngine.Object context) => UnityEngine.Debug.Log(message, context);
    }
}
