using System;
using System.Collections.Generic;
using SoulHunter.Core.Persistence;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.Player
{
    /// <summary>What a run has achieved so far, for checking character unlock conditions.</summary>
    public readonly struct RunProgress
    {
        public readonly int Stage;
        public readonly float StageSeconds;
        public readonly int HighestClearedStage;
        public readonly int PlayerLevel;
        public readonly int Kills;
        public readonly Func<UpgradeData.UpgradeType, int> WeaponLevel;

        public RunProgress(int stage, float stageSeconds, int highestClearedStage, int playerLevel, int kills,
            Func<UpgradeData.UpgradeType, int> weaponLevel)
        {
            Stage = stage;
            StageSeconds = stageSeconds;
            HighestClearedStage = highestClearedStage;
            PlayerLevel = playerLevel;
            Kills = kills;
            WeaponLevel = weaponLevel;
        }
    }

    /// <summary>
    /// VS rule: a character is unlocked by meeting its condition during a run (saved for good), then
    /// bought once with gold at the character select screen. Characters with no condition start unlocked,
    /// and free ones need no purchase. Pure logic over GameData so it is testable without a scene.
    /// </summary>
    public static class CharacterUnlocks
    {
        public static bool StartsUnlocked(CharacterData character) =>
            character != null && character.Unlock == CharacterData.UnlockCondition.None;

        public static bool IsUnlocked(CharacterData character, GameData data) =>
            character != null && (StartsUnlocked(character) || Contains(data?.UnlockedCharacters, character.CharacterName));

        public static bool IsOwned(CharacterData character, GameData data) =>
            IsUnlocked(character, data) && (character.GoldPrice <= 0 || Contains(data?.OwnedCharacters, character.CharacterName));

        public static bool IsMet(CharacterData character, in RunProgress progress)
        {
            if (character == null) return false;
            switch (character.Unlock)
            {
                case CharacterData.UnlockCondition.None: return true;
                case CharacterData.UnlockCondition.ClearStage: return progress.HighestClearedStage >= character.UnlockStage;
                case CharacterData.UnlockCondition.SurviveInStage:
                    return progress.Stage == character.UnlockStage && progress.StageSeconds >= character.UnlockTarget;
                case CharacterData.UnlockCondition.ReachPlayerLevel: return progress.PlayerLevel >= character.UnlockTarget;
                case CharacterData.UnlockCondition.KillsInRun: return progress.Kills >= character.UnlockTarget;
                case CharacterData.UnlockCondition.WeaponLevel:
                    return progress.WeaponLevel != null && progress.WeaponLevel(character.UnlockWeapon) >= character.UnlockTarget;
                default: return false;
            }
        }

        /// <summary>Records the unlock in the save data. True if it was new.</summary>
        public static bool MarkUnlocked(CharacterData character, GameData data)
        {
            if (character == null || data == null || IsUnlocked(character, data)) return false;
            if (data.UnlockedCharacters == null) data.UnlockedCharacters = new List<string>();
            data.UnlockedCharacters.Add(character.CharacterName);
            return true;
        }

        /// <summary>
        /// VS wiki (Characters): "All non-secret characters need to be purchased using gold, increased by 10% additively
        /// per characters purchased". Each paid character already bought adds 10% of the base price.
        /// </summary>
        public const float VsPriceStepPerPurchase = 0.1f;

        /// <summary>
        /// The gold price now: base x (1 + 0.1 x characters purchased), rounded down to whole gold. The wiki gives no rounding;
        /// balance method: down, as VS rounds the PowerUp fee (floor(20 x 1.1^TotalBought)). Free characters stay free and
        /// are not counted as purchases.
        /// </summary>
        public static int PriceFor(CharacterData character, GameData data)
        {
            if (character == null || character.GoldPrice <= 0) return 0;
            // Whole-number maths, so 900 x 1.1 is 990 and not 989.99 rounded down.
            return character.GoldPrice * (10 + UnityEngine.Mathf.RoundToInt(VsPriceStepPerPurchase * 10f) * PurchasedCount(data)) / 10;
        }

        /// <summary>Characters bought with gold on this save (free characters in the list do not count).</summary>
        public static int PurchasedCount(GameData data)
        {
            if (data?.OwnedCharacters == null) return 0;
            var seen = new HashSet<string>();
            int count = 0;
            foreach (var name in data.OwnedCharacters)
            {
                if (string.IsNullOrEmpty(name) || !seen.Add(name)) continue;
                var character = GameContentCatalog.FindCharacterExact(GameContentCatalog.CurrentName(name));
                if (character != null && character.GoldPrice > 0) count++;
            }
            return count;
        }

        /// <summary>Buys an unlocked character. spendGold takes the price and returns false if the player cannot pay.</summary>
        public static bool TryBuy(CharacterData character, GameData data, Func<int, bool> spendGold)
        {
            if (character == null || data == null || !IsUnlocked(character, data)) return false;
            if (IsOwned(character, data)) return true;
            if (spendGold == null || !spendGold(PriceFor(character, data))) return false;
            if (data.OwnedCharacters == null) data.OwnedCharacters = new List<string>();
            data.OwnedCharacters.Add(character.CharacterName);
            return true;
        }

        /// <summary>The unlock condition as the select screen shows it.</summary>
        public static string ConditionText(CharacterData character)
        {
            if (character == null) return "";
            string stage = $"Stage {character.UnlockStage}: {CampaignLevelCatalog.Get(character.UnlockStage).Name}";
            switch (character.Unlock)
            {
                case CharacterData.UnlockCondition.None: return "Available from the start.";
                // A level clears at 30:00 once its guardian is defeated (owner rule 2026-10-13); Level 10 clears when Shadow
                // Kael is destroyed (LevelProgressionManager). Before 2026-10-19 the text said only "Defeat the boss",
                // though the boss kill alone unlocks nothing (Bug MIS ITM-12).
                case CharacterData.UnlockCondition.ClearStage:
                    return character.UnlockStage >= CampaignLevelCatalog.Count
                        ? $"Destroy Shadow Kael in {stage}."
                        : $"Clear {stage}: defeat its guardian and survive to 30:00.";
                case CharacterData.UnlockCondition.SurviveInStage:
                    return $"Survive {Clock(character.UnlockTarget)} in {stage}.";
                case CharacterData.UnlockCondition.ReachPlayerLevel: return $"Reach level {character.UnlockTarget:0} in one run.";
                case CharacterData.UnlockCondition.KillsInRun: return $"Defeat {character.UnlockTarget:0} enemies in one run.";
                case CharacterData.UnlockCondition.WeaponLevel:
                    return $"Get {character.UnlockWeapon} to level {character.UnlockTarget:0}.";
                default: return "";
            }
        }

        private static string Clock(float seconds) => $"{(int)seconds / 60}:{(int)seconds % 60:00}";

        private static bool Contains(List<string> list, string name) => list != null && name != null && list.Contains(name);
    }
}
