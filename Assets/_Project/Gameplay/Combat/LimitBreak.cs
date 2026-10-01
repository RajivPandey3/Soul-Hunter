using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// VS Limit Break (vampire.survivors.wiki/w/Limit_Break, "List of upgrades", fetched 2026-10-01). With the mode on, a
    /// level-up that has nothing left to offer shows Limit Break cards instead of gold and a chicken: each card raises one
    /// stat of one held weapon beyond its max level. The stat is drawn by the wiki's rarity from those the weapon can use:
    /// Base Damage +1 (10), Area +2% (10, up to +500%), Speed +5% (10, up to +200%), Duration +0.1 s (1, up to +2 s),
    /// Amount +1 (1, up to +10), Pierce +1 (5, up to +10), Crit Chance +2.5% (5, up to 100%), Chance +5% (10, up to 100%).
    /// Owner's choice (2026-10-01, "wiki ke saaf numbers lagao"): the wiki's row "Might +0.3%" is noted as "Meant to be Base
    /// Damage", so it is Base Damage +1; where a row lists weapon-specific values the plain one is used. Treasure chests
    /// are not affected (VS). Bonuses belong to the live weapon object: an evolution starts again (VS: extra stats reset on
    /// evolution). Shadow Kael's mirrored copies do not carry them.
    /// </summary>
    public static class LimitBreak
    {
        public enum Stat { BaseDamage, Area, Speed, Duration, Amount, Pierce, CritChance, Chance }

        /// <summary>One row of the wiki table: what a card adds, its rarity, and the most it can add in total (0 = none).</summary>
        public readonly struct Row
        {
            public readonly Stat Stat;
            public readonly float Step;
            public readonly int Rarity;
            public readonly float MaxTotal;
            public Row(Stat stat, float step, int rarity, float maxTotal) { Stat = stat; Step = step; Rarity = rarity; MaxTotal = maxTotal; }
        }

        public static readonly Row[] Table =
        {
            new Row(Stat.BaseDamage, 1f, 10, 0f),
            new Row(Stat.Area, 0.02f, 10, 5f),
            new Row(Stat.Speed, 0.05f, 10, 2f),
            new Row(Stat.Duration, 0.1f, 1, 2f),
            new Row(Stat.Amount, 1f, 1, 10f),
            new Row(Stat.Pierce, 1f, 5, 10f),
            new Row(Stat.CritChance, 0.025f, 5, 1f),
            new Row(Stat.Chance, 0.05f, 10, 1f),
        };

        public static Row RowFor(Stat stat) { foreach (var row in Table) if (row.Stat == stat) return row; return Table[0]; }

        private sealed class Bonus
        {
            public readonly float[] Totals = new float[System.Enum.GetValues(typeof(Stat)).Length];
            public int Cards;
        }

        private static readonly Dictionary<AutoAttackWeapon, Bonus> Bonuses = new Dictionary<AutoAttackWeapon, Bonus>();
        private static readonly Dictionary<UpgradeData, (AutoAttackWeapon Weapon, Stat Stat)> Offers = new Dictionary<UpgradeData, (AutoAttackWeapon, Stat)>();

        /// <summary>Limit Break cards taken for this weapon in this run.</summary>
        public static int CardsFor(AutoAttackWeapon weapon) => weapon != null && Bonuses.TryGetValue(weapon, out var b) ? b.Cards : 0;

        /// <summary>The total a weapon's cards add to <paramref name="stat"/>.</summary>
        public static float Total(AutoAttackWeapon weapon, Stat stat) =>
            weapon != null && Bonuses.Count > 0 && Bonuses.TryGetValue(weapon, out var b) ? b.Totals[(int)stat] : 0f;

        public static float BaseDamageBonus(AutoAttackWeapon weapon) => Total(weapon, Stat.BaseDamage);
        public static float AreaBonus(AutoAttackWeapon weapon) => Total(weapon, Stat.Area);
        public static float SpeedBonus(AutoAttackWeapon weapon) => Total(weapon, Stat.Speed);
        public static float DurationSeconds(AutoAttackWeapon weapon) => Total(weapon, Stat.Duration);
        public static int AmountBonus(AutoAttackWeapon weapon) => Mathf.RoundToInt(Total(weapon, Stat.Amount));
        public static int PierceBonus(AutoAttackWeapon weapon) => Mathf.RoundToInt(Total(weapon, Stat.Pierce));
        public static float CritChanceBonus(AutoAttackWeapon weapon) => Total(weapon, Stat.CritChance);
        public static float ChanceBonus(AutoAttackWeapon weapon) => Total(weapon, Stat.Chance);

        /// <summary>Whether <paramref name="weapon"/> can take a card for <paramref name="stat"/> (it uses the stat and is under the cap).</summary>
        public static bool CanRaise(AutoAttackWeapon weapon, Stat stat)
        {
            if (weapon == null) return false;
            bool uses = stat switch
            {
                Stat.Duration => weapon.LimitBreakBaseDuration > 0f,
                Stat.Pierce => weapon.UsesPierce,
                Stat.CritChance => weapon.CanCrit,
                Stat.Chance => weapon.HasChance,
                _ => true,
            };
            var row = RowFor(stat);
            return uses && (row.MaxTotal <= 0f || Total(weapon, stat) + row.Step <= row.MaxTotal + 1e-4f);
        }

        /// <summary>Takes one card: <paramref name="stat"/> of <paramref name="weapon"/> rises by its step.</summary>
        public static void Apply(AutoAttackWeapon weapon, Stat stat)
        {
            if (weapon == null) return;
            if (!Bonuses.TryGetValue(weapon, out var bonus)) Bonuses[weapon] = bonus = new Bonus();
            var row = RowFor(stat);
            float total = bonus.Totals[(int)stat] + row.Step;
            if (row.MaxTotal > 0f) total = Mathf.Min(total, row.MaxTotal);
            bonus.Totals[(int)stat] = total;
            bonus.Cards++;
            Debug.Log($"[LimitBreak] {weapon.name}: {stat} +{row.Step} (total {total}).");
        }

        /// <summary>Draws a stat for <paramref name="weapon"/> by the wiki rarities; false when none can rise.</summary>
        public static bool DrawStat(AutoAttackWeapon weapon, float roll01, out Stat stat)
        {
            stat = Stat.BaseDamage;
            int pool = 0;
            foreach (var row in Table) if (CanRaise(weapon, row.Stat)) pool += row.Rarity;
            if (pool <= 0) return false;
            float pick = Mathf.Clamp01(roll01) * pool;
            foreach (var row in Table)
            {
                if (!CanRaise(weapon, row.Stat)) continue;
                if (pick < row.Rarity) { stat = row.Stat; return true; }
                pick -= row.Rarity;
            }
            foreach (var row in Table) if (CanRaise(weapon, row.Stat)) stat = row.Stat;
            return true;
        }

        /// <summary>Clears every bonus and pending card (a new run).</summary>
        public static void Reset()
        {
            Bonuses.Clear();
            ClearOffers();
        }

        /// <summary>True for a card made by <see cref="BuildCards"/>.</summary>
        public static bool IsCard(UpgradeData upgrade) => upgrade != null && Offers.ContainsKey(upgrade);

        /// <summary>The weapon a Limit Break card strengthens.</summary>
        public static AutoAttackWeapon WeaponOf(UpgradeData card) => card != null && Offers.TryGetValue(card, out var offer) ? offer.Weapon : null;

        /// <summary>The stat a Limit Break card raises.</summary>
        public static Stat StatOf(UpgradeData card) => card != null && Offers.TryGetValue(card, out var offer) ? offer.Stat : Stat.BaseDamage;

        /// <summary>Applies a Limit Break card. False when <paramref name="card"/> is not one (or its weapon is gone).</summary>
        public static bool TryApplyCard(UpgradeData card)
        {
            if (card == null || !Offers.TryGetValue(card, out var offer) || offer.Weapon == null) return false;
            Apply(offer.Weapon, offer.Stat);
            return true;
        }

        /// <summary>
        /// Builds up to <paramref name="count"/> Limit Break cards, each for a different weapon the player really uses
        /// (evolved and union weapons included), chosen at random, each with a stat drawn by rarity. Cards from the
        /// previous offer are thrown away. <paramref name="iconFor"/> finds a weapon's level-up icon by its type (may be null).
        /// </summary>
        public static List<UpgradeData> BuildCards(WeaponManager weapons, int count,
                                                   System.Func<UpgradeData.UpgradeType, Sprite> iconFor = null)
        {
            ClearOffers();
            var result = new List<UpgradeData>();
            if (weapons == null || count <= 0) return result;

            var equipped = weapons.GetEquippedWeapons();
            while (result.Count < count && equipped.Count > 0)
            {
                int index = Random.Range(0, equipped.Count);
                var pair = equipped[index];
                equipped.RemoveAt(index);
                var weapon = pair.Value;
                if (weapon == null || !DrawStat(weapon, Random.value, out var stat)) continue;

                var type = TypeOf(weapons, pair.Key, out bool known);
                var card = ScriptableObject.CreateInstance<UpgradeData>();
                card.name = "LimitBreak_" + pair.Key.name;
                card.UpgradeName = "Limit Break: " + ReadableName(pair.Key.name);
                card.Type = type;
                card.Level = weapons.GetWeaponLevel(type) + 1;
                card.Icon = known && iconFor != null ? iconFor(type) : null;
                card.Description = Describe(stat) + " beyond max level.";
                Offers[card] = (weapon, stat);
                result.Add(card);
            }
            return result;
        }

        /// <summary>The card text for a stat (Soul Hunter's wording).</summary>
        public static string Describe(Stat stat) => stat switch
        {
            Stat.BaseDamage => "Base damage +1",
            Stat.Area => "Area +2%",
            Stat.Speed => "Speed +5%",
            Stat.Duration => "Duration +0.1 s",
            Stat.Amount => "Fires 1 more",
            Stat.Pierce => "Pierces 1 more enemy",
            Stat.CritChance => "Critical chance +2.5%",
            _ => "Chance +5%",
        };

        private static void ClearOffers()
        {
            foreach (var card in Offers.Keys)
            {
                if (card == null) continue;
                if (Application.isPlaying) Object.Destroy(card);
                else Object.DestroyImmediate(card);
            }
            Offers.Clear();
        }

        private static UpgradeData.UpgradeType TypeOf(WeaponManager weapons, GameObject prefab, out bool known)
        {
            foreach (UpgradeData.UpgradeType type in System.Enum.GetValues(typeof(UpgradeData.UpgradeType)))
                if (weapons.GetWeaponObject(type) == prefab) { known = true; return type; }
            known = false;
            return default;
        }

        private static readonly Regex Suffix = new Regex(@"(_Weapon|Weapon|\(Clone\))\s*$");
        private static readonly Regex CamelSplit = new Regex(@"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])");

        /// <summary>"Whip_Weapon" -> "Whip", "UnholyVespers" -> "Unholy Vespers".</summary>
        public static string ReadableName(string objectName)
        {
            string name = Suffix.Replace(objectName ?? "", "").Replace('_', ' ').Trim();
            return CamelSplit.Replace(name, " ").Trim();
        }
    }
}
