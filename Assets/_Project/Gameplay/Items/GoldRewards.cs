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
            return Mathf.Max(1, Mathf.RoundToInt(baseAmount * greed));
        }

        /// <summary>Grants gold after Greed and returns the amount actually given.</summary>
        public static int Grant(int baseAmount, PlayerStats stats)
        {
            int amount = ApplyGreed(baseAmount, stats);
            if (amount <= 0) return 0;

            if (GameServices.Instance != null && GameServices.Instance.TryGet<EconomyService>(out var economy))
                economy.AddGold(amount);
            if (SoulHunter.Gameplay.Core.RunStatsTracker.Instance != null)
                SoulHunter.Gameplay.Core.RunStatsTracker.Instance.AddGold(amount);
            return amount;
        }
    }
}
