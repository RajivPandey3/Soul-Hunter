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

        /// <summary>
        /// VS Big Coin Bags (25 gold each, then Greed): a maxed level-up's gold card, or a chest item with nothing left to
        /// upgrade. Each is counted on the save (wiki Level_up: the "always" option comes "once the player has received
        /// 100 Big Coin Bags from any source"). Returns the gold given.
        /// </summary>
        public static int GrantBigCoinBags(int bags, PlayerStats stats)
        {
            if (bags <= 0) return 0;
            int given = Grant(BigCoinBagGold * bags, stats);
            CountBigCoinBags(bags);
            return given;
        }

        /// <summary>Counts Big Coin Bags whose gold was paid elsewhere (a chest pays its own gold and the bags together).</summary>
        public static void CountBigCoinBags(int bags)
        {
            if (bags <= 0) return;
            if (GameServices.Instance != null && GameServices.Instance.TryGet<SoulHunter.Core.Persistence.SaveService>(out var save) && save.CurrentData != null)
            {
                save.CurrentData.BigCoinBagsReceived += bags;
                save.SaveSoon();
            }
        }

        /// <summary>VS Big Coin Bag: 25 gold (wiki Big Coin Bag).</summary>
        public const int BigCoinBagGold = 25;

        /// <summary>Big Coin Bags this save has received (0 without a save).</summary>
        public static int BigCoinBagsReceived =>
            GameServices.Instance != null && GameServices.Instance.TryGet<SoulHunter.Core.Persistence.SaveService>(out var save) && save.CurrentData != null
                ? save.CurrentData.BigCoinBagsReceived : 0;

        private static void Deposit(int amount)
        {
            if (amount <= 0) return;

            if (GameServices.Instance != null && GameServices.Instance.TryGet<EconomyService>(out var economy))
                economy.AddGold(amount);
            if (SoulHunter.Gameplay.Core.RunStatsTracker.Instance != null)
                SoulHunter.Gameplay.Core.RunStatsTracker.Instance.AddGold(amount);
            // VS Disco of Gold (XV): "Obtaining gold restores as many HP."
            SoulHunter.Gameplay.Core.ArcanaManager.OnGoldObtained(amount);
        }
    }
}
