using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Core.Persistence
{
    /// <summary>
    /// Learning Comment:
    /// Centralized data model. Unity ke PlayerPrefs ki jagah hum ek solid object rakhenge
    /// jo JSON mein serialize hoga. Isme pure game ki state save hoti hai.
    /// </summary>
    [Serializable]
    public class GameData
    {
        public int PlayerHealth;
        public Vector3 PlayerPosition;
        public int Currency;
        public bool SettingsMigrated;
        public float MasterVolume = 1f;

        // --- Options (VS-style options screen) ---
        public float MusicVolume = 1f;
        public float SfxVolume = 1f;
        public bool FlashingVfx = true;
        public bool DamageNumbers = true;
        public bool ScreenShake = true;
        /// <summary>VS Window Mode: 0 Fullscreen Window, 1 Exclusive Fullscreen, 2 Maximised Window, 3 Windowed.</summary>
        public int WindowMode = 0;
        /// <summary>Index into the distinct screen resolutions; -1 = the display's native resolution.</summary>
        public int ResolutionIndex = -1;
        public bool VSync = true;
        public bool DisableBlood = false;
        public bool ShowStageProgression = true;
        /// <summary>On-screen touch joystick (VS "Visible Joysticks"); always shown on mobile.</summary>
        public bool VisibleJoystick = false;
        public bool PauseOnFocusLoss = true;
        /// <summary>Input System binding overrides (remapped controls) as JSON; empty = defaults.</summary>
        public string InputBindingOverrides = "";
        public float HighScoreTime;

        // --- Meta Progression (Power-Ups) ---
        public int MetaMightLevel;
        public int MetaArmorLevel;
        public int MetaGreedLevel;
        public int MetaRevivalLevel;

        // --- Current Run Data ---
        public string SelectedCharacterName;
        public List<string> UnlockedAchievements = new List<string>();

        // --- Characters (VS rule: meet a character's unlock condition, then buy it with gold) ---
        public List<string> UnlockedCharacters = new List<string>();
        public List<string> OwnedCharacters = new List<string>();

        public GameData()
        {
            // Default values for a new save
            PlayerHealth = 100;
            PlayerPosition = Vector3.zero;
            Currency = 0;

            MetaMightLevel = 0;
            MetaArmorLevel = 0;
            MetaGreedLevel = 0;
            MetaRevivalLevel = 0;

            SelectedCharacterName = "Kael"; // Default Hero
        }
    }
}
