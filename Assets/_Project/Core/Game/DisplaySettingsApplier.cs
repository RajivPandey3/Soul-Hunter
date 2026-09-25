using UnityEngine;
using SoulHunter.Core.Persistence;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>Applies the Fullscreen option at startup and whenever the options change.</summary>
    public static class DisplaySettingsApplier
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            GameSettings.Changed -= Apply;
            GameSettings.Changed += Apply;
            Apply();
        }

        public static void Apply()
        {
            // The editor's Game view cannot go fullscreen; builds follow the option.
            if (Application.isEditor) return;
            if (Screen.fullScreen != GameSettings.Fullscreen) Screen.fullScreen = GameSettings.Fullscreen;
        }
    }
}
