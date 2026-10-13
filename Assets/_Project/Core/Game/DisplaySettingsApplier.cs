using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SoulHunter.Core.Persistence;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// Applies the VS display options (Window Mode, Resolution, Screen Refresh Rate, V-Sync) at startup and whenever the
    /// options change. VS wiki (Options, Display, PC-exclusive): "Screen Refresh Rate ... Only visible when using Exclusive
    /// Fullscreen Window Mode". The wiki lists no values, so the choices are Default plus the rates the display offers for
    /// the chosen resolution (Soul Hunter's reading); outside Exclusive Fullscreen the stored rate is kept but not used.
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

        /// <summary>The window size the options ask for: the chosen resolution, or the display's own.</summary>
        public static Vector2Int ChosenSize()
        {
            var list = Resolutions();
            int index = GameSettings.ResolutionIndex;
            return index >= 0 && index < list.Count
                ? list[index]
                : new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
        }

        /// <summary>VS: the Screen Refresh Rate row exists on PC only, and only in Exclusive Fullscreen (Window Mode 1).</summary>
        public static bool ShowsRefreshRate(int windowMode, bool mobile) => !mobile && windowMode == 1;

        /// <summary>The whole-hertz refresh rates the display offers at <paramref name="size"/>, lowest first.</summary>
        public static List<int> RefreshRates(Vector2Int size) =>
            RefreshRates(Screen.resolutions.Select(r => (new Vector2Int(r.width, r.height), r.refreshRateRatio.value)), size);

        /// <summary>Distinct whole-hertz rates of the modes at <paramref name="size"/>, lowest first (59.94 reads as 60).</summary>
        public static List<int> RefreshRates(IEnumerable<(Vector2Int size, double hz)> modes, Vector2Int size) =>
            modes.Where(m => m.size == size && m.hz > 0d).Select(m => Mathf.RoundToInt((float)m.hz))
                .Distinct().OrderBy(hz => hz).ToList();

        public static string RefreshRateLabel(int hz) => hz <= 0 ? "Default" : hz + " Hz";

        /// <summary>Frames a second the game asks for on every platform (the performance contract's target).</summary>
        public const int TargetFrameRate = 60;
        public const int MobileFrameRate = TargetFrameRate;
        /// <summary>Measuring tools (the benchmark, the bot's quality A/B) set this to run without V-Sync or a frame cap.</summary>
        public static bool Uncapped;

        /// <summary>
        /// V-Sync intervals that bring a display of <paramref name="hz"/> closest to 60 frames a second: 1 on a 60 or 75 Hz
        /// display, 2 on 120 or 144 Hz, 4 on 240 Hz.
        /// </summary>
        public static int VSyncCountFor(double hz) => hz <= 0d ? 1 : Mathf.Clamp(Mathf.RoundToInt((float)hz / TargetFrameRate), 1, 4);

        public static void Apply()
        {
            if (Uncapped)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;
            }
            else
            {
                // Owner (2026-10-12: "60 fps kar do" on phones; 2026-10-13: "pure game ko 60 FPS par chalao"): the whole game
                // runs at 60 frames a second. Unity holds a phone at 30 unless told otherwise; on a PC the cap holds 60
                // with V-Sync off, and with V-Sync on a faster display skips refreshes (120 Hz shows every second one).
                QualitySettings.vSyncCount = GameSettings.VSync ? VSyncCountFor(Screen.currentResolution.refreshRateRatio.value) : 0;
                // Not in batch mode: the test runner has no display, and a cap would only slow every test down.
                Application.targetFrameRate = Application.isBatchMode ? -1 : TargetFrameRate;
            }
            // The editor's Game view cannot change window mode or resolution; builds follow the options.
            if (Application.isEditor || Application.isMobilePlatform) return;
            var mode = ModeFor(GameSettings.WindowMode);
            var size = ChosenSize();
            // Screen Refresh Rate: Exclusive Fullscreen only (VS); Default (0) or a rate the display lacks lets the display pick.
            int hz = mode == FullScreenMode.ExclusiveFullScreen ? GameSettings.RefreshRateHz : 0;
            if (hz > 0)
            {
                foreach (var r in Screen.resolutions)
                {
                    if (r.width != size.x || r.height != size.y || Mathf.RoundToInt((float)r.refreshRateRatio.value) != hz) continue;
                    bool rateDiffers = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value) != hz;
                    if (Screen.fullScreenMode != mode || Screen.width != size.x || Screen.height != size.y || rateDiffers)
                        Screen.SetResolution(size.x, size.y, mode, r.refreshRateRatio);
                    return;
                }
            }
            if (Screen.fullScreenMode != mode || Screen.width != size.x || Screen.height != size.y)
                Screen.SetResolution(size.x, size.y, mode);
        }
    }
}
