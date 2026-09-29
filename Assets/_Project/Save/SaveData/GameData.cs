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
        /// <summary>Save schema version this build writes. Raise it and add a step to SaveService.Migrate when the format changes.</summary>
        public const int CurrentVersion = 2;

        /// <summary>
        /// Schema version of this save. No initializer on purpose: a file written before versioning has no such
        /// field, so it loads as 0 and is migrated. Fresh saves get CurrentVersion from SaveService.
        /// </summary>
        public int SaveVersion;
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
        /// <summary>VS Options, Display: "Show Defanged Enemies" (the green look of Defanged enemies).</summary>
        public bool ShowDefangedEnemies = true;
        /// <summary>Input System binding overrides (remapped controls) as JSON; empty = defaults.</summary>
        public string InputBindingOverrides = "";
        public float HighScoreTime;

        // --- Meta Progression (Power-Ups) ---
        public int MetaMightLevel;
        public int MetaArmorLevel;
        public int MetaGreedLevel;
        public int MetaRevivalLevel;
        /// <summary>Old shop (2026-09-29 buttons); v1 saves only. Refunded and zeroed by the v2 migration.</summary>
        public int MetaMaxHealthLevel;
        /// <summary>Old shop (2026-09-29 buttons); v1 saves only. Refunded and zeroed by the v2 migration.</summary>
        public int MetaRecoveryLevel;
        /// <summary>VS PowerUp ranks bought (PowerUps; since save version 2, 2026-09-30).</summary>
        public System.Collections.Generic.List<PowerUpRank> PowerUpRanks = new System.Collections.Generic.List<PowerUpRank>();

        // --- Current Run Data ---
        public string SelectedCharacterName;
        public List<string> UnlockedAchievements = new List<string>();

        // --- Characters (VS rule: meet a character's unlock condition, then buy it with gold) ---
        public List<string> UnlockedCharacters = new List<string>();
        public List<string> OwnedCharacters = new List<string>();

        // --- Soul Eggs (VS Golden Egg rule: each egg permanently raises one random stat of one character) ---
        public List<SoulEggRecord> SoulEggs = new List<SoulEggRecord>();

        /// <summary>
        /// VS wiki (Treasure_Chest, Skipping): chests opened on this save, and how many held 5 items. The first six chest
        /// animations cannot be skipped; 5-item chests become skippable after 50 five-item chests or 500 chests.
        /// </summary>
        public int ChestsOpened;
        public int FiveItemChestsOpened;

        // --- Run modes (VS "List of Modes"), picked on the character select screen and copied into RunModes when a
        // run starts. A save written before these existed has no such fields and loads them as off. ---
        public bool ModeHurry;
        public bool ModeHyper;
        public bool ModeInverse;
        public bool ModeEndless;
        public bool ModeArcana;
        public bool ModeLimitBreak;

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
            MetaMaxHealthLevel = 0;
            MetaRecoveryLevel = 0;

            SelectedCharacterName = "Kael"; // Default Hero
        }
    }

    /// <summary>
    /// Soul Egg bonuses of one character, as totals. Fractional flat stats (Armor, Amount, Revival, Reroll, Skip,
    /// Banish) add up egg by egg and count only in whole points. Multipliers are fractions (0.01 = +1%).
    /// </summary>
    [Serializable]
    public class SoulEggRecord
    {
        public string Character;
        public int Eggs;
        public float MaxHealth, Recovery, Armor, Amount, Revival, Reroll, Skip, Banish;
        public float MoveSpeed, Might, Speed, Duration, Area, Luck, Growth, Greed, Curse, Magnet;
        /// <summary>Cooldown reduction as a positive fraction (0.005 = 0.5% faster).</summary>
        public float Cooldown;
    }
}
