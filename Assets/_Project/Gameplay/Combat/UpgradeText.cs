using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Learning Comment: Level-up card text (owner request 2026-09-25: a VS-style level-up screen). The upgrade assets
    /// were generated with names like "SongOfMana Lv.2" and placeholder descriptions ("SongOfMana upgrade level 2."),
    /// so the cards showed internal names. This gives a readable name, what a new weapon does (Soul Hunter's own
    /// wording, written from each weapon's code), and each passive's real per-level effect (WeaponManager.ApplyUpgrade).
    /// A description written by hand in the asset is kept.
    /// </summary>
    public static class UpgradeText
    {
        private static readonly Regex Placeholder = new Regex(@"^\s*\w+ upgrade level \d+\.?\s*$");
        private static readonly Regex LevelSuffix = new Regex(@"\s*Lv\.?\s*\d+\s*$");
        private static readonly Regex CamelSplit = new Regex(@"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])");

        private static readonly Dictionary<UpgradeData.UpgradeType, string> Weapons = new Dictionary<UpgradeData.UpgradeType, string>
        {
            { UpgradeData.UpgradeType.MagicWand, "Fires a soul bolt at the nearest enemy." },
            { UpgradeData.UpgradeType.Whip, "Lashes the nearest enemy, then the other side." },
            { UpgradeData.UpgradeType.Garlic, "A cursed aura that hurts every enemy close to you." },
            { UpgradeData.UpgradeType.Axe, "Hurls a heavy axe that tumbles through enemies." },
            { UpgradeData.UpgradeType.Bible, "Tomes circle around you and strike what they touch." },
            { UpgradeData.UpgradeType.Cross, "A holy cross flies at the nearest enemy and returns." },
            { UpgradeData.UpgradeType.LightningRing, "Calls lightning down on nearby enemies." },
            { UpgradeData.UpgradeType.FireWand, "Throws fireballs at the nearest enemy." },
            { UpgradeData.UpgradeType.SantaWater, "Pours holy water that burns enemies standing in it." },
            { UpgradeData.UpgradeType.Knife, "Throws fast knives at the nearest enemy." },
            { UpgradeData.UpgradeType.Runetracer, "A glowing rune that races across the field." },
            { UpgradeData.UpgradeType.Pentagram, "Now and then erases every enemy on the screen." },
            { UpgradeData.UpgradeType.ClockLancet, "A beam of frost that freezes enemies in place." },
            { UpgradeData.UpgradeType.Laurel, "A ward that blocks incoming hits." },
            { UpgradeData.UpgradeType.Bone, "Throws a spinning bone at the nearest enemy." },
            { UpgradeData.UpgradeType.Peachone, "A spirit bird circles you and drops blasts." },
            { UpgradeData.UpgradeType.SongOfMana, "Piercing magic shots toward the nearest enemy." },
            { UpgradeData.UpgradeType.CherryBomb, "Bombs that bounce away and explode." },
            { UpgradeData.UpgradeType.Gun, "Shoots bullets in four directions at once." },
        };

        private static readonly Dictionary<UpgradeData.UpgradeType, string> Passives = new Dictionary<UpgradeData.UpgradeType, string>
        {
            { UpgradeData.UpgradeType.PlayerSpeed, "+10% move speed." },
            { UpgradeData.UpgradeType.MaxHealth, "+20 max health." },
            { UpgradeData.UpgradeType.EmptyTome, "-8% weapon cooldown." },
            { UpgradeData.UpgradeType.Spinach, "+10% Might (all damage)." },
            { UpgradeData.UpgradeType.Bracer, "+10% projectile speed." },
            { UpgradeData.UpgradeType.Candelabrador, "+10% area." },
            { UpgradeData.UpgradeType.Spellbinder, "+10% weapon duration." },
            { UpgradeData.UpgradeType.Duplicator, "+1 projectile for every weapon." },
            { UpgradeData.UpgradeType.Armor, "+1 armor (less damage taken)." },
            { UpgradeData.UpgradeType.Pummarola, "Recover 0.2 health every second." },
            { UpgradeData.UpgradeType.Attractorb, "+25% pickup range." },
            { UpgradeData.UpgradeType.Clover, "+10% luck." },
            { UpgradeData.UpgradeType.Crown, "+8% experience." },
            { UpgradeData.UpgradeType.SkullOManiac, "+10% curse: more and tougher enemies." },
            { UpgradeData.UpgradeType.Tiragisu, "+1 revival." },
            { UpgradeData.UpgradeType.MetaglioLeft, "+0.1 health per second and +5% max health." },
            { UpgradeData.UpgradeType.MetaglioRight, "+5% curse." },
        };

        public static bool IsWeapon(UpgradeData.UpgradeType type) => Weapons.ContainsKey(type);

        /// <summary>"SongOfMana Lv.2" -> "Song Of Mana".</summary>
        public static string DisplayName(UpgradeData upgrade)
        {
            string raw = upgrade != null && !string.IsNullOrWhiteSpace(upgrade.UpgradeName) ? upgrade.UpgradeName : upgrade?.Type.ToString() ?? "";
            raw = LevelSuffix.Replace(raw, "");
            return CamelSplit.Replace(raw, " ").Trim();
        }

        /// <summary>The card's small tag: NEW! for something the player does not have yet, else the level it goes to.</summary>
        public static string Tag(UpgradeData upgrade, int ownedLevel) =>
            LimitBreak.IsCard(upgrade) ? "LIMIT BREAK" : ownedLevel <= 0 ? "NEW!" : $"Level {upgrade.Level}";

        public static string Describe(UpgradeData upgrade, int ownedLevel)
        {
            if (upgrade == null) return "";
            if (!string.IsNullOrWhiteSpace(upgrade.Description) && !Placeholder.IsMatch(upgrade.Description)) return upgrade.Description;
            if (Passives.TryGetValue(upgrade.Type, out var passive)) return passive;
            if (Weapons.TryGetValue(upgrade.Type, out var weapon))
                return ownedLevel <= 0 ? weapon : $"Stronger {DisplayName(upgrade)}: level {upgrade.Level}.";
            return $"Upgrade to level {upgrade.Level}.";
        }
    }
}
