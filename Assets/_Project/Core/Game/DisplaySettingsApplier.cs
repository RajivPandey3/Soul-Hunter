using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SoulHunter.Core.Persistence;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// Applies the VS display options (Window Mode, Resolution, V-Sync) at startup and whenever the options change.
    /// </summary>
    public static class DisplaySettingsApplier
    {
        public static readonly string[] WindowModeNames = { "Fullscreen Window", "Exclusive Fullscreen", "Maximised Window", "Windowed" };
        private static readonly FullScreenMode[] Modes =
            { FullScreenMode.FullScreenWindow, FullScreenMode.ExclusiveFullScreen, FullScreenMode.MaximizedWindow, FullScreenMode.Windowed };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            GameSettings.Changed -= Apply;
            GameSettings.Changed += Apply;
            Apply();
        }

        public static FullScreenMode ModeFor(int windowMode) => Modes[Mathf.Clamp(windowMode, 0, Modes.Length - 1)];

        /// <summary>Distinct width x height pairs the display supports, smallest first.</summary>
        public static List<Vector2Int> Resolutions() =>
            Screen.resolutions.Select(r => new Vector2Int(r.width, r.height)).Distinct()
                .OrderBy(r => r.x).ThenBy(r => r.y).ToList();

        public static string ResolutionLabel(int index)
        {
            var list = Resolutions();
            return index < 0 || index >= list.Count ? "Native" : $"{list[index].x} x {list[index].y}";
        }

        public static void Apply()
        {
            QualitySettings.vSyncCount = GameSettings.VSync ? 1 : 0;
            // The editor's Game view cannot change window mode or resolution; builds follow the options.
            if (Application.isEditor || Application.isMobilePlatform) return;
            var mode = ModeFor(GameSettings.WindowMode);
            var list = Resolutions();
            int index = GameSettings.ResolutionIndex;
            var size = index >= 0 && index < list.Count
                ? list[index]
                : new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
            if (Screen.fullScreenMode != mode || Screen.width != size.x || Screen.height != size.y)
                Screen.SetResolution(size.x, size.y, mode);
        }
    }
}
