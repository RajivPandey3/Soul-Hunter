using System;
using SoulHunter.Core.Services;

namespace SoulHunter.Core.Persistence
{
    /// <summary>
    /// The player's options (VS-style options screen): volumes, flashing VFX, damage numbers, screen shake,
    /// window mode, resolution, refresh rate, V-Sync, blood, stage progression, touch joystick (and its type) and pause-on-focus-loss.
    /// Stored in GameData; defaults apply when there is no save (e.g. isolated tests). Systems read these
    /// each time they need them, so a change applies at once; listeners that cache values subscribe to Changed.
    /// </summary>
    public static class GameSettings
    {
        public static event Action Changed;

        public static GameData Data
        {
            get
            {
                if (GameServices.Instance == null || !GameServices.Instance.TryGet(out SaveService save)) return null;
                return save.CurrentData;
            }
        }

        public static float MusicVolume => Data != null ? Clamp01(Data.MusicVolume) : 1f;
        public static float SfxVolume => Data != null ? Clamp01(Data.SfxVolume) : 1f;
        public static bool FlashingVfx => Data == null || Data.FlashingVfx;
        public static bool DamageNumbers => Data == null || Data.DamageNumbers;
        public static bool ScreenShake => Data == null || Data.ScreenShake;
        public static int WindowMode => Data != null ? Data.WindowMode : 0;
        public static int ResolutionIndex => Data != null ? Data.ResolutionIndex : -1;
        public static bool VSync => Data == null || Data.VSync;
        public static bool DisableBlood => Data != null && Data.DisableBlood;
        public static bool ShowStageProgression => Data == null || Data.ShowStageProgression;
        public static bool VisibleJoystick => Data != null && Data.VisibleJoystick;
        public static bool PauseOnFocusLoss => Data == null || Data.PauseOnFocusLoss;
        public static bool ShowDefangedEnemies => Data == null || Data.ShowDefangedEnemies;
        /// <summary>VS Options, Gameplay: "Visually Invert Stages" (the Inverse look; on without a save).</summary>
        public static bool VisuallyInvertStages => Data == null || Data.VisuallyInvertStages;
        /// <summary>VS Screen Refresh Rate in whole hertz; 0 = Default (the display picks).</summary>
        public static int RefreshRateHz => Data != null && Data.RefreshRateHz > 0 ? Data.RefreshRateHz : 0;
        /// <summary>VS Joystick Type values, in the wiki's order.</summary>
        public const int JoystickLegacy = 0, JoystickDefault = 1;
        public static readonly string[] JoystickTypeNames = { "Legacy", "Default" };
        /// <summary>VS Joystick Type of the touch stick (JoystickLegacy or JoystickDefault).</summary>
        public static int JoystickType => Data != null && Data.JoystickType == JoystickLegacy ? JoystickLegacy : JoystickDefault;

        /// <summary>Tells listeners (audio, screen mode) that an option changed.</summary>
        public static void NotifyChanged() => Changed?.Invoke();

        /// <summary>Writes the options to the save file.</summary>
        public static void Save()
        {
            if (GameServices.Instance != null && GameServices.Instance.TryGet(out SaveService save)) save.SaveGame();
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
