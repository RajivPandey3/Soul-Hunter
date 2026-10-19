using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// VS wiki (Level_up, fetched 2026-09-28 and 2026-10-01): "When ... player has nothing left to upgrade, level ups will
    /// instead grant the options to gain extra Gold or restore health with Floor Chicken, with the option to always
    /// receive either Floor Chickens or Gold once the player has received 100 Big Coin Bags from any source." and "The
    /// 'Always Floor Chicken' option will only give Floor Chickens if the player has missing health. If the player is
    /// fully healed, they will receive Gold instead." The level-up screen offers the cards and the player picks one:
    /// 25 gold (a Big Coin Bag, then Greed) or 30 health (a Floor Chicken). After 100 bags two more cards choose "always";
    /// for the rest of the run, maxed level-ups then pay out at once with no screen. Soul Hunter's own card names.
    /// </summary>
    public static class MaxedRewards
    {
        public const int GoldAmount = 25;   // VS Big Coin Bag
        /// <summary>One name for the 25-gold reward on the level-up card and the chest card (Bug MIS v2 C17).</summary>
        public const string GoldCardName = "Gold Pouch";
        public const int HealAmount = 30;   // VS Floor Chicken
        public const int AlwaysAfterBags = 100;

        public enum Kind { Gold, Chicken, AlwaysGold, AlwaysChicken }
        public enum AlwaysChoice { None, Gold, Chicken }

        /// <summary>The run's "always" choice; reset when a run starts (LevelUpManager).</summary>
        public static AlwaysChoice Always { get; set; } = AlwaysChoice.None;

        private static readonly Dictionary<UpgradeData, Kind> Cards = new Dictionary<UpgradeData, Kind>();

        public static bool IsCard(UpgradeData upgrade) => upgrade != null && Cards.ContainsKey(upgrade);
        public static bool IsGoldCard(UpgradeData upgrade) => upgrade != null && Cards.TryGetValue(upgrade, out var kind) && (kind == Kind.Gold || kind == Kind.AlwaysGold);
        public static bool TryGetKind(UpgradeData upgrade, out Kind kind) { kind = Kind.Gold; return upgrade != null && Cards.TryGetValue(upgrade, out kind); }

        /// <summary>The two choices of a level-up with nothing left to upgrade.</summary>
        public static List<UpgradeData> BuildCards() => BuildCards(false);

        /// <summary>The choices, with the two "always" cards once 100 Big Coin Bags have been received.</summary>
        public static List<UpgradeData> BuildCards(bool withAlways)
        {
            Cards.Clear();
            var content = SoulHunter.Gameplay.Data.GameContentCatalog.Load();
            var list = new List<UpgradeData>
            {
                Card(Kind.Gold, GoldCardName, $"+{GoldAmount} gold.", content != null ? content.GoldPouchIcon : null),
                Card(Kind.Chicken, "Roast Chicken", $"Restores {HealAmount} health.", content != null ? content.RoastChickenIcon : null),
            };
            if (withAlways)
            {
                list.Add(Card(Kind.AlwaysGold, "Always Gold Pouch", $"+{GoldAmount} gold now and at every maxed level-up this run.", content != null ? content.GoldPouchIcon : null));
                list.Add(Card(Kind.AlwaysChicken, "Always Roast Chicken", $"Restores {HealAmount} health now and at every maxed level-up this run (gold when at full health).", content != null ? content.RoastChickenIcon : null));
            }
            return list;
        }

        private static UpgradeData Card(Kind kind, string name, string description, Sprite icon)
        {
            var card = ScriptableObject.CreateInstance<UpgradeData>();
            card.UpgradeName = name;
            card.Description = description;
            card.Icon = icon;
            Cards[card] = kind;
            return card;
        }

        /// <summary>Applies a reward card to the player. False when <paramref name="card"/> is not one.</summary>
        public static bool TryApply(UpgradeData card, SoulHunter.Gameplay.Player.PlayerController player)
        {
            if (!TryGetKind(card, out var kind)) return false;
            switch (kind)
            {
                case Kind.Gold: GiveGold(player); break;
                case Kind.Chicken: GiveChicken(player); break;
                case Kind.AlwaysGold: Always = AlwaysChoice.Gold; ApplyAlways(player); break;
                case Kind.AlwaysChicken: Always = AlwaysChoice.Chicken; ApplyAlways(player); break;
            }
            return true;
        }

        /// <summary>Pays one maxed level-up by the run's "always" choice. False when none was chosen.</summary>
        public static bool ApplyAlways(SoulHunter.Gameplay.Player.PlayerController player)
        {
            if (Always == AlwaysChoice.None) return false;
            if (Always == AlwaysChoice.Chicken) GiveChickenOrGold(player);
            else GiveGold(player);
            return true;
        }

        /// <summary>A Floor Chicken (with Recovery) when the player is hurt, else a Big Coin Bag: "Always Floor Chicken" at
        /// full health gives gold (wiki Level_up).</summary>
        public static void GiveChickenOrGold(SoulHunter.Gameplay.Player.PlayerController player)
        {
            if (HasMissingHealth(player)) GiveChicken(player);
            else GiveGold(player);
        }

        private static bool HasMissingHealth(SoulHunter.Gameplay.Player.PlayerController player)
        {
            var health = player != null ? player.GetComponent<HealthController>() : null;
            return health != null && health.CurrentHealth < health.MaxHealth;
        }

        private static void GiveGold(SoulHunter.Gameplay.Player.PlayerController player) =>
            SoulHunter.Gameplay.Pickups.GoldRewards.GrantBigCoinBags(1, player != null ? player.Stats : null);

        private static void GiveChicken(SoulHunter.Gameplay.Player.PlayerController player)
        {
            if (player == null) return;
            player.GetComponent<HealthController>()?.Heal(SoulHunter.Gameplay.Pickups.HealthPickup.HealWithRecovery(HealAmount, player.Stats != null ? player.Stats.Regen : 0f));
        }
    }
}
