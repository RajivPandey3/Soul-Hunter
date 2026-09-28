using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// VS wiki (Level_up, fetched 2026-09-28): "When ... player has nothing left to upgrade, level ups will instead grant
    /// the options to gain extra Gold or restore health with Floor Chicken." The level-up screen offers two cards and the
    /// player picks one: 25 gold (a Big Coin Bag, then Greed) or 30 health (a Floor Chicken). Before this, both were given
    /// silently with no screen. Soul Hunter's own card names.
    /// </summary>
    public static class MaxedRewards
    {
        public const int GoldAmount = 25;   // VS Big Coin Bag
        public const int HealAmount = 30;   // VS Floor Chicken

        private static readonly Dictionary<UpgradeData, bool> Cards = new Dictionary<UpgradeData, bool>(); // card -> is gold

        public static bool IsCard(UpgradeData upgrade) => upgrade != null && Cards.ContainsKey(upgrade);
        public static bool IsGoldCard(UpgradeData upgrade) => upgrade != null && Cards.TryGetValue(upgrade, out var gold) && gold;

        /// <summary>The two choices of a level-up with nothing left to upgrade.</summary>
        public static List<UpgradeData> BuildCards()
        {
            Cards.Clear();
            var gold = ScriptableObject.CreateInstance<UpgradeData>();
            gold.UpgradeName = "Gold Pouch";
            gold.Description = $"+{GoldAmount} gold.";
            var chicken = ScriptableObject.CreateInstance<UpgradeData>();
            chicken.UpgradeName = "Roast Chicken";
            chicken.Description = $"Restores {HealAmount} health.";
            Cards[gold] = true;
            Cards[chicken] = false;
            return new List<UpgradeData> { gold, chicken };
        }

        /// <summary>Applies a reward card to the player. False when <paramref name="card"/> is not one.</summary>
        public static bool TryApply(UpgradeData card, SoulHunter.Gameplay.Player.PlayerController player)
        {
            if (!IsCard(card)) return false;
            if (IsGoldCard(card))
                SoulHunter.Gameplay.Pickups.GoldRewards.Grant(GoldAmount, player != null ? player.Stats : null);
            else if (player != null)
                player.GetComponent<HealthController>()?.Heal(HealAmount);
            return true;
        }
    }
}
