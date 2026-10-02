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
            { UpgradeData.UpgradeType.MaxHealth, "Max health x1.2." },
            { UpgradeData.UpgradeType.EmptyTome, "-8% weapon cooldown." },
            { UpgradeData.UpgradeType.Spinach, "+10% Might (all damage)." },
            { UpgradeData.UpgradeType.Bracer, "+10% projectile speed." },
            { UpgradeData.UpgradeType.Candelabrador, "+10% area." },
            { UpgradeData.UpgradeType.Spellbinder, "+10% weapon duration." },
            { UpgradeData.UpgradeType.Duplicator, "+1 projectile for every weapon." },
            { UpgradeData.UpgradeType.Armor, "+1 armor (less damage taken)." },
            { UpgradeData.UpgradeType.Pummarola, "Recover 0.2 health every second." },
            { UpgradeData.UpgradeType.Attractorb, "Bigger pickup range." },
            { UpgradeData.UpgradeType.Clover, "+10% luck." },
            { UpgradeData.UpgradeType.Crown, "+8% experience." },
            { UpgradeData.UpgradeType.SkullOManiac, "+10% curse: more and tougher enemies." },
            { UpgradeData.UpgradeType.Tiragisu, "+1 revival." },
            { UpgradeData.UpgradeType.MetaglioLeft, "+0.1 health per second and +5% max health." },
            { UpgradeData.UpgradeType.MetaglioRight, "+5% curse." },
        };

        /// <summary>
        /// Bug MIS v2 C14 (2026-10-02): an owned weapon's card said only that it got stronger. Each level now says what it
        /// adds: the facts of each weapon's VS level table (vampire.survivors.wiki, the weapon's "Levels" section, fetched
        /// 2026-10-01), in Soul Hunter's own short wording (no VS text).
        /// </summary>
        private static readonly Dictionary<UpgradeData.UpgradeType, Dictionary<int, string>> WeaponLevels = new Dictionary<UpgradeData.UpgradeType, Dictionary<int, string>>
        {
            { UpgradeData.UpgradeType.Whip, new Dictionary<int, string> { { 2, "+1 projectile" }, { 3, "+5 damage" }, { 4, "+5 damage, +10% area" }, { 5, "+5 damage" }, { 6, "+5 damage, +10% area" }, { 7, "+5 damage" }, { 8, "+5 damage" } } },
            { UpgradeData.UpgradeType.MagicWand, new Dictionary<int, string> { { 2, "+1 projectile" }, { 3, "-0.2 s cooldown" }, { 4, "+1 projectile" }, { 5, "+10 damage" }, { 6, "+1 projectile" }, { 7, "+1 pierce" }, { 8, "+10 damage" } } },
            { UpgradeData.UpgradeType.Knife, new Dictionary<int, string> { { 2, "+1 projectile" }, { 3, "+1 projectile, +5 damage" }, { 4, "+1 projectile" }, { 5, "+1 pierce" }, { 6, "+1 projectile" }, { 7, "+1 projectile, +5 damage" }, { 8, "+1 pierce" } } },
            { UpgradeData.UpgradeType.Axe, new Dictionary<int, string> { { 2, "+1 projectile" }, { 3, "+20 damage" }, { 4, "+2 pierce" }, { 5, "+1 projectile" }, { 6, "+20 damage" }, { 7, "+2 pierce" }, { 8, "+20 damage" } } },
            { UpgradeData.UpgradeType.Cross, new Dictionary<int, string> { { 2, "+10 damage" }, { 3, "+10% area, +25% speed" }, { 4, "+1 projectile" }, { 5, "+10 damage" }, { 6, "+10% area, +25% speed" }, { 7, "+1 projectile" }, { 8, "+10 damage" } } },
            { UpgradeData.UpgradeType.Bible, new Dictionary<int, string> { { 2, "+1 projectile" }, { 3, "+25% area, +30% speed" }, { 4, "+10 damage, +0.5 s duration" }, { 5, "+1 projectile" }, { 6, "+25% area, +30% speed" }, { 7, "+10 damage, +0.5 s duration" }, { 8, "+1 projectile" } } },
            { UpgradeData.UpgradeType.FireWand, new Dictionary<int, string> { { 2, "+10 damage" }, { 3, "+10 damage, +20% speed" }, { 4, "+10 damage" }, { 5, "+10 damage, +20% speed" }, { 6, "+10 damage" }, { 7, "+10 damage, +20% speed" }, { 8, "+10 damage" } } },
            { UpgradeData.UpgradeType.Garlic, new Dictionary<int, string> { { 2, "+2 damage, +40% area" }, { 3, "+1 damage, -0.1 s cooldown" }, { 4, "+1 damage, +20% area" }, { 5, "+2 damage, -0.1 s cooldown" }, { 6, "+1 damage, +20% area" }, { 7, "+1 damage, -0.1 s cooldown" }, { 8, "+2 damage, +20% area" } } },
            { UpgradeData.UpgradeType.SantaWater, new Dictionary<int, string> { { 2, "+1 projectile, +20% area" }, { 3, "+10 damage, +0.5 s duration" }, { 4, "+1 projectile, +20% area" }, { 5, "+10 damage, +0.3 s duration" }, { 6, "+1 projectile, +20% area" }, { 7, "+5 damage, +0.3 s duration" }, { 8, "+5 damage, +20% area" } } },
            { UpgradeData.UpgradeType.Runetracer, new Dictionary<int, string> { { 2, "+5 damage, +20% speed" }, { 3, "+5 damage, +0.3 s duration" }, { 4, "+1 projectile" }, { 5, "+5 damage, +20% speed" }, { 6, "+5 damage, +0.3 s duration" }, { 7, "+1 projectile" }, { 8, "+0.5 s duration" } } },
            { UpgradeData.UpgradeType.LightningRing, new Dictionary<int, string> { { 2, "+1 projectile" }, { 3, "+10 damage, +100% area" }, { 4, "+1 projectile" }, { 5, "+20 damage, +100% area" }, { 6, "+1 projectile" }, { 7, "+20 damage, +100% area" }, { 8, "+1 projectile" } } },
            { UpgradeData.UpgradeType.Pentagram, new Dictionary<int, string> { { 2, "-10 s cooldown" }, { 3, "25% chance to spare items on screen" }, { 4, "-10 s cooldown" }, { 5, "45% chance to spare items on screen" }, { 6, "-5 s cooldown" }, { 7, "65% chance to spare items on screen" }, { 8, "-5 s cooldown" } } },
            { UpgradeData.UpgradeType.Peachone, new Dictionary<int, string> { { 2, "+1 projectile, +40% area" }, { 3, "+1 projectile, +10 damage" }, { 4, "+1 projectile, -0.3 s cooldown" }, { 5, "+1 projectile, +40% area" }, { 6, "+1 projectile, +10 damage" }, { 7, "+1 projectile, -0.3 s cooldown" }, { 8, "+1 projectile, +40% area" } } },
            { UpgradeData.UpgradeType.SongOfMana, new Dictionary<int, string> { { 2, "+100% area" }, { 3, "+0.5 s duration, +0.8 s cooldown" }, { 4, "+10 damage, +25% area" }, { 5, "+0.5 s duration, +0.8 s cooldown" }, { 6, "+10 damage, +25% area" }, { 7, "+0.5 s duration, +0.8 s cooldown" }, { 8, "+10 damage, +25% area" } } },
            { UpgradeData.UpgradeType.ClockLancet, new Dictionary<int, string> { { 2, "+1 s duration" }, { 3, "-0.5 s cooldown" }, { 4, "+1 s duration" }, { 5, "+1 s duration" }, { 6, "-0.5 s cooldown" }, { 7, "+1 s duration" } } },
            { UpgradeData.UpgradeType.Laurel, new Dictionary<int, string> { { 2, "-0.5 s cooldown, +0.2 s ward" }, { 3, "-0.5 s cooldown, +0.2 s ward" }, { 4, "+1 charge" }, { 5, "-0.5 s cooldown, +0.2 s ward" }, { 6, "-0.5 s cooldown, +0.2 s ward" }, { 7, "+1 charge" } } },
            { UpgradeData.UpgradeType.Bone, new Dictionary<int, string> { { 2, "+20% area, +0.2 s duration" }, { 3, "+1 projectile, +20 damage" }, { 4, "+20% area, +50% speed" }, { 5, "+1 projectile, +20 damage" }, { 6, "+0.2 s duration" }, { 7, "+20 damage" }, { 8, "+0.2 s duration, +50% speed" } } },
            { UpgradeData.UpgradeType.CherryBomb, new Dictionary<int, string> { { 2, "+25% area, +30% speed" }, { 3, "50% chance to explode" }, { 4, "+1 projectile" }, { 5, "+5 damage, +25% area" }, { 6, "60% chance to explode" }, { 7, "+25% area, +30% speed" }, { 8, "+5 damage, +25% area" } } },
            { UpgradeData.UpgradeType.Gun, new Dictionary<int, string> { { 2, "+1 projectile" }, { 3, "+2 pierce" }, { 4, "+1 projectile" }, { 5, "+5 damage" }, { 6, "+2 pierce" }, { 7, "+5 damage, +50% speed" }, { 8, "+2 pierce" } } },
        };

        /// <summary>What level <paramref name="level"/> of a weapon adds ("+5 damage, +10% area"), or null.</summary>
        public static string WeaponLevelText(UpgradeData.UpgradeType type, int level) =>
            WeaponLevels.TryGetValue(type, out var levels) && levels.TryGetValue(level, out var text) ? text : null;

        private static string LevelLine(UpgradeData upgrade)
        {
            string text = WeaponLevelText(upgrade.Type, upgrade.Level);
            return text != null ? char.ToUpperInvariant(text[0]) + text.Substring(1) + "." : $"{DisplayName(upgrade)} level {upgrade.Level}.";
        }

        public static bool IsWeapon(UpgradeData.UpgradeType type)
        {
            var definition = SoulHunter.Gameplay.Data.WeaponDefinitionLookup.Find(type);
            return definition != null ? !definition.IsPassive : Weapons.ContainsKey(type);
        }

        /// <summary>
        /// The built-in level-1 text of <paramref name="type"/> (what a weapon does, or a passive's per-level effect), or
        /// null. Used when no WeaponDefinition exists, and by WeaponDefinitionBuilder to fill the definitions.
        /// </summary>
        public static string FallbackDescription(UpgradeData.UpgradeType type) =>
            Passives.TryGetValue(type, out var passive) ? passive : Weapons.TryGetValue(type, out var weapon) ? weapon : null;

        /// <summary>"SongOfMana Lv.2" -> "Song Of Mana". Prefers the item's WeaponDefinition.DisplayName.</summary>
        public static string DisplayName(UpgradeData upgrade)
        {
            var definition = DefinitionFor(upgrade);
            if (definition != null && !string.IsNullOrWhiteSpace(definition.DisplayName)) return definition.DisplayName;
            return NameFromAsset(upgrade);
        }

        /// <summary>An item's player-facing name from its type alone (its WeaponDefinition first), for the Results screen.</summary>
        public static string NameOf(UpgradeData.UpgradeType type)
        {
            var definition = SoulHunter.Gameplay.Data.WeaponDefinitionLookup.Find(type);
            if (definition != null && !string.IsNullOrWhiteSpace(definition.DisplayName)) return definition.DisplayName;
            return CamelSplit.Replace(type.ToString(), " ").Trim();
        }

        /// <summary>The name derived from the UpgradeData asset alone (the pre-definition rule).</summary>
        public static string NameFromAsset(UpgradeData upgrade)
        {
            string raw = upgrade != null && !string.IsNullOrWhiteSpace(upgrade.UpgradeName) ? upgrade.UpgradeName : upgrade?.Type.ToString() ?? "";
            raw = LevelSuffix.Replace(raw, "");
            return CamelSplit.Replace(raw, " ").Trim();
        }

        /// <summary>The definition behind a real item card; null for Limit Break and maxed-reward cards (not items).</summary>
        private static SoulHunter.Gameplay.Data.WeaponDefinition DefinitionFor(UpgradeData upgrade)
        {
            if (upgrade == null || LimitBreak.IsCard(upgrade) || MaxedRewards.IsCard(upgrade)) return null;
            return SoulHunter.Gameplay.Data.WeaponDefinitionLookup.Find(upgrade.Type);
        }

        /// <summary>The card's small tag: NEW! for something the player does not have yet, else the level it goes to.</summary>
        public static string Tag(UpgradeData upgrade, int ownedLevel) =>
            LimitBreak.IsCard(upgrade) ? "LIMIT BREAK" : MaxedRewards.IsCard(upgrade) ? "REWARD" : ownedLevel <= 0 ? "NEW!" : $"Level {upgrade.Level}";

        public static string Describe(UpgradeData upgrade, int ownedLevel)
        {
            if (upgrade == null) return "";
            if (!string.IsNullOrWhiteSpace(upgrade.Description) && !Placeholder.IsMatch(upgrade.Description)) return upgrade.Description;
            var definition = DefinitionFor(upgrade);
            if (definition != null && !string.IsNullOrWhiteSpace(definition.Description))
            {
                if (definition.IsPassive) return definition.Description;
                return ownedLevel <= 0 ? definition.Description : LevelLine(upgrade);
            }
            if (Passives.TryGetValue(upgrade.Type, out var passive)) return passive;
            if (Weapons.TryGetValue(upgrade.Type, out var weapon))
                return ownedLevel <= 0 ? weapon : LevelLine(upgrade);
            return $"Upgrade to level {upgrade.Level}.";
        }
    }
}
