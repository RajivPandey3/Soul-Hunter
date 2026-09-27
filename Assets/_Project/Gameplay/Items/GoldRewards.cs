using UnityEngine;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// Learning Comment:
    /// VS rule: har gold reward (coins, chest, maxed level-up) par player ka Greed multiplier lagta hai.
    /// Ye ek hi jagah se gold deta hai: Greed apply karta hai, EconomyService mein save karta hai,
    /// aur RunStatsTracker mein is run ka gold record karta hai.
    /// </summary>
    public static class GoldRewards
    {
        /// <summary>Base gold scaled by the player's Greed. Without stats, Greed counts as 1.</summary>
        public static int ApplyGreed(int baseAmount, PlayerStats stats)
        {
            if (baseAmount <= 0) return 0;
            float greed = stats != null ? Mathf.Max(0f, stats.Greed) : 1f;
            // VS: the stage's gold bonus multiplies separately from Greed.
            return Mathf.Max(1, Mathf.RoundToInt(baseAmount * greed * SoulHunter.Gameplay.Data.CampaignLevelCatalog.CurrentGold));
        }

        /// <summary>
        /// A fractional base (VS Gold Fever coins are worth 0.5) times Greed and the stage's gold bonus, rounded, and
        /// at least 1 so a picked-up coin always pays something.
        /// </summary>
        public static int ApplyGreedFraction(float baseAmount, float greed, float stageGold)
        {
            if (baseAmount <= 0f) return 0;
            return Mathf.Max(1, Mathf.RoundToInt(baseAmount * Mathf.Max(0f, greed) * Mathf.Max(0f, stageGold)));
        }

        /// <summary>Grants a fractional base amount after Greed (see ApplyGreedFraction) and returns the gold given.</summary>
        public static int GrantFraction(float baseAmount, PlayerStats stats)
        {
            int amount = ApplyGreedFraction(baseAmount, stats != null ? stats.Greed : 1f, SoulHunter.Gameplay.Data.CampaignLevelCatalog.CurrentGold);
            Deposit(amount);
            return amount;
        }

        /// <summary>Grants gold after Greed and returns the amount actually given.</summary>
        public static int Grant(int baseAmount, PlayerStats stats)
        {
            int amount = ApplyGreed(baseAmount, stats);
            Deposit(amount);
            return amount;
        }

        private static void Deposit(int amount)
        {
            if (amount <= 0) return;

            if (GameServices.Instance != null && GameServices.Instance.TryGet<EconomyService>(out var economy))
                economy.AddGold(amount);
            if (SoulHunter.Gameplay.Core.RunStatsTracker.Instance != null)
                SoulHunter.Gameplay.Core.RunStatsTracker.Instance.AddGold(amount);
        }
    }
}
