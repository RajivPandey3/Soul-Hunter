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
        public const int CurrentVersion = 3;

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
        /// <summary>
        /// V-Sync, off by default (owner, 2026-10-15: "pehle V-Sync off"): with V-Sync on, a frame that takes a little over
        /// 16.7 ms waits for the next refresh and shows as a 33 ms jolt; off, the 60 cap holds the pace and such a frame shows
        /// as 58-59. Tearing is possible; a player can turn it on in Options.
        /// </summary>
        public bool VSync = false;
        public bool DisableBlood = false;
        public bool ShowStageProgression = true;
        /// <summary>On-screen touch joystick (VS "Visible Joysticks"); always shown on mobile.</summary>
        public bool VisibleJoystick = false;
        public bool PauseOnFocusLoss = true;
        /// <summary>VS Options, Display: "Show Defanged Enemies" (the green look of Defanged enemies).</summary>
        public bool ShowDefangedEnemies = true;
        /// <summary>
        /// VS Options, Display (PC-exclusive): "Screen Refresh Rate", "Only visible when using Exclusive Fullscreen Window Mode".
        /// Stored in whole hertz, not as a list index, so it survives a resolution change (each resolution has its own list);
        /// 0 = Default (the display picks). A save written before this field loads it as 0.
        /// </summary>
        public int RefreshRateHz = 0;
        /// <summary>
        /// VS Options, Gameplay: "Joystick Type" (Legacy, Default) for the touch stick: 0 Legacy, 1 Default. The wiki names no
        /// default value; Default is the default because that is the option's own name (GameSettings.JoystickDefault).
        /// </summary>
        public int JoystickType = 1;
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
        /// <summary>
        /// Maxed PowerUps the player switched off (VS PowerUps page; PowerUpId names). An older save has no such field and
        /// loads with every PowerUp on; SaveService's repair keeps only ids that can be switched.
        /// </summary>
        public List<string> DisabledPowerUps = new List<string>();

        // --- Current Run Data ---
        public string SelectedCharacterName;
        public List<string> UnlockedAchievements = new List<string>();

        // --- Characters (VS rule: meet a character's unlock condition, then buy it with gold) ---
        public List<string> UnlockedCharacters = new List<string>();
        public List<string> OwnedCharacters = new List<string>();
        /// <summary>VS character select (wiki Characters, Favorite): characters shown first on the grid, by name.</summary>
        public List<string> FavoriteCharacters = new List<string>();
        /// <summary>
        /// VS stage completion panel (wiki Characters): the levels each character completed. Soul Hunter: a level cleared
        /// at 30:00 (or Level 10 by destroying Shadow Kael) with that character, in any run.
        /// </summary>
        public List<CharacterStageRecord> CharacterStages = new List<CharacterStageRecord>();

        // --- Soul Eggs (VS Golden Egg rule: each egg permanently raises one random stat of one character) ---
        public List<SoulEggRecord> SoulEggs = new List<SoulEggRecord>();

        /// <summary>
        /// VS wiki (Treasure_Chest, Skipping): chests opened on this save, and how many held 5 items. The first six chest
        /// animations cannot be skipped; 5-item chests become skippable after 50 five-item chests or 500 chests.
        /// </summary>
        public int ChestsOpened;
        /// <summary>VS (wiki Level_up): Big Coin Bags received from any source; at 100 the "always gold/chicken" option opens.</summary>
        public int BigCoinBagsReceived;
        public int FiveItemChestsOpened;

        /// <summary>
        /// VS item unlocks (ItemUnlocks, 2026-10-12): Soul Lanterns broken on this save (wiki Light_source: "Destroying 20
        /// light sources unlocks the Fire Wand") and Roast Chickens picked up (wiki Achievements: Garlic, "Find 5 Floor
        /// Chickens"). An older save has no such fields and loads them as 0; SaveService's repair keeps them from going negative.
        /// </summary>
        public int LightSourcesBroken;
        public int FloorChickensFound;

        /// <summary>
        /// VS Collection (wiki Collection): the entries "used by the player at least once in their save file", by
        /// CollectionBook id. An older save has no list; SaveService's repair adds the entries found from the start.
        /// </summary>
        public List<string> CollectionFound = new List<string>();
        /// <summary>VS Bestiary (wiki Ars_Gouda): how many of each enemy type this save has defeated (EnemyData asset name).</summary>
        public List<EnemyKillRecord> BestiaryKills = new List<EnemyKillRecord>();
        /// <summary>
        /// Evolutions and unions obtained from a chest on this save (EvolutionDiscovery; VS Grim_Grimoire: "discovered on
        /// their save file"). Level-up cards hint them and the pause screen's evolution list shows them. An older save has no
        /// such field and loads it empty (SaveService repair).
        /// </summary>
        public List<string> DiscoveredEvolutions = new List<string>();

        // --- Run modes (VS "List of Modes"), picked on the character select screen and copied into RunModes when a
        // run starts. A save written before these existed has no such fields and loads them as off. ---
        public bool ModeHurry;
        public bool ModeHyper;
        public bool ModeInverse;
        public bool ModeEndless;
        public bool ModeArcana;
        public bool ModeLimitBreak;
        /// <summary>VS Random Events (release item P3-05): a wheel picks an event at every XX:30 that fires at XX:00.</summary>
        public bool ModeRandomEvents;
        /// <summary>VS Random LevelUp (release item P3-05): every level-up picks one of its choices at random.</summary>
        public bool ModeRandomLevelUp;

        /// <summary>
        /// VS Options, Gameplay: "Visually Invert Stages" (wiki Options; Stages, Inverse Mode: "The layout inversion effect
        /// can be toggled in the Settings"). The wiki names no default; on is the default because the option only exists to
        /// turn the inverted look off (Patch 1.0: "Added option to disable inverted mode visually inverting stages"), so the
        /// look is on until a player turns it off (balance method, 2026-10-09). A save written before this field loads it on.
        /// </summary>
        public bool VisuallyInvertStages = true;

        /// <summary>
        /// VS Music Selection (wiki Stages, "Music selection"; Music): the track chosen for the next run. 0 = each level's own
        /// track (the VS default), 1 = the main menu theme, 2-11 = the tracks of Levels 1-10. Repaired by SaveService.
        /// </summary>
        public int MusicChoice;
        /// <summary>VS music modifier: 0 Normal, 1 faster and higher (VS Hyper), 2 slower and lower (VS Forsaken).</summary>
        public int MusicTempo;
        /// <summary>VS Patch 1.0: "lock the selection of a music track so it persists between runs". Off: the choice lasts one run.</summary>
        public bool MusicLocked;

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

    /// <summary>The levels (1-10) one character has completed, each once.</summary>
    [Serializable]
    public class CharacterStageRecord
    {
        public string Character;
        public List<int> Levels = new List<int>();
    }

    /// <summary>
    /// VS stage completion per character (vampire.survivors.wiki/w/Characters, Stage completion panel, fetched 2026-10-09:
    /// "A stage with a checkmark displayed before the stage name indicates that the player completed that stage with a
    /// specific character"; the grid can be sorted "through the number of complete stages per character"). VS completes a
    /// stage per run; Soul Hunter's one run plays ten levels (owner, Run structure), so a level counts as completed when
    /// it is cleared at 30:00, or Level 10 when Shadow Kael is destroyed (release item P3-03, 2026-10-09).
    /// </summary>
    public static class CharacterStages
    {
        public const int LevelCount = 10;

        /// <summary>Records that <paramref name="character"/> completed <paramref name="level"/>; true when it is new.</summary>
        public static bool Record(GameData data, string character, int level)
        {
            if (data == null || string.IsNullOrEmpty(character) || level < 1 || level > LevelCount) return false;
            data.CharacterStages ??= new List<CharacterStageRecord>();
            var record = data.CharacterStages.Find(r => r != null && r.Character == character);
            if (record == null) data.CharacterStages.Add(record = new CharacterStageRecord { Character = character });
            record.Levels ??= new List<int>();
            if (record.Levels.Contains(level)) return false;
            record.Levels.Add(level);
            return true;
        }

        /// <summary>How many different levels <paramref name="character"/> has completed.</summary>
        public static int Count(GameData data, string character)
        {
            var record = data?.CharacterStages?.Find(r => r != null && r.Character == character);
            return record?.Levels?.Count ?? 0;
        }

        public static bool Has(GameData data, string character, int level)
        {
            var record = data?.CharacterStages?.Find(r => r != null && r.Character == character);
            return record?.Levels != null && record.Levels.Contains(level);
        }
    }

    /// <summary>
    /// VS favourite characters (vampire.survivors.wiki/w/Characters, fetched 2026-10-09): "Displays favorited characters at
    /// the top of the character grid, regardless of how it is sorted, but not if it is filtered ... Unlocked through
    /// Mindbender"; "Whether a character is favorited or not would remain unaffected" by CLEAR. Mindbender (wiki Mindbender)
    /// "is automatically obtained by filling out 50 entries in the Collection". Soul Hunter has no Collection yet, so, as
    /// with Banish, Omni, Charm, Defang and Preserve (2026-10-01), favourites stay locked until the Mindbender unlock id is
    /// on the save (Phases 2-3). <see cref="UnlockId"/> can be granted by a debug tool or a test.
    /// </summary>
    public static class CharacterFavourites
    {
        public const string UnlockId = "Relic.Mindbender";

        public static bool IsUnlocked(GameData data) => data?.UnlockedAchievements != null && data.UnlockedAchievements.Contains(UnlockId);

        public static bool IsFavourite(GameData data, string character) =>
            IsUnlocked(data) && data.FavoriteCharacters != null && data.FavoriteCharacters.Contains(character);

        /// <summary>Marks or clears a favourite (ignored while favourites are locked).</summary>
        public static void Set(GameData data, string character, bool favourite)
        {
            if (data == null || string.IsNullOrEmpty(character) || !IsUnlocked(data)) return;
            data.FavoriteCharacters ??= new List<string>();
            data.FavoriteCharacters.Remove(character);
            if (favourite) data.FavoriteCharacters.Add(character);
        }

        /// <summary>Grants the Mindbender unlock (tests and debug tools; Soul Hunter has no Collection yet).</summary>
        public static void Unlock(GameData data)
        {
            if (data == null) return;
            data.UnlockedAchievements ??= new List<string>();
            if (!data.UnlockedAchievements.Contains(UnlockId)) data.UnlockedAchievements.Add(UnlockId);
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
