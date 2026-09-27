using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Learning Comment: VS Limit Break (vampire.survivors.wiki/w/Limit_Break). With the mode on, a level-up that has
    /// nothing left to offer shows Limit Break cards instead of paying gold and a chicken: each card strengthens one
    /// weapon the player holds, beyond its max level, and the bonus stacks without end. VS picks a random stat per card
    /// (Might, Area, Speed, Amount, ...); Soul Hunter's first version gives +10% damage per card (provisional), read by
    /// AutoAttackWeapon.ScaledDamage through <see cref="DamageMultiplier"/>. Treasure chests are not affected (VS).
    /// Bonuses belong to the live weapon object: an evolution spawns a new object, so its bonus starts again (VS: extra
    /// stats reset on evolution). Shadow Kael's mirrored copies are separate objects and do not carry them.
    /// </summary>
    public static class LimitBreak
    {
        /// <summary>Damage added by one Limit Break card (+10%, provisional balance).</summary>
        public const float DamagePerCard = 0.1f;

        private static readonly Dictionary<AutoAttackWeapon, int> Cards = new Dictionary<AutoAttackWeapon, int>();
        private static readonly Dictionary<UpgradeData, AutoAttackWeapon> Offers = new Dictionary<UpgradeData, AutoAttackWeapon>();

        /// <summary>Limit Break cards taken for this weapon in this run.</summary>
        public static int CardsFor(AutoAttackWeapon weapon) =>
            weapon != null && Cards.TryGetValue(weapon, out int count) ? count : 0;

        /// <summary>1 + 10% per Limit Break card taken for this weapon.</summary>
        public static float DamageMultiplier(AutoAttackWeapon weapon)
        {
            if (Cards.Count == 0 || weapon == null) return 1f;
            return 1f + DamagePerCard * CardsFor(weapon);
        }

        /// <summary>Takes one Limit Break card for this weapon.</summary>
        public static void Apply(AutoAttackWeapon weapon)
        {
            if (weapon == null) return;
            Cards[weapon] = CardsFor(weapon) + 1;
            Debug.Log($"[LimitBreak] {weapon.name}: {Cards[weapon]} card(s), damage x{DamageMultiplier(weapon):0.00}.");
        }

        /// <summary>Clears every bonus and pending card (a new run).</summary>
        public static void Reset()
        {
            Cards.Clear();
            ClearOffers();
        }

        /// <summary>True for a card made by <see cref="BuildCards"/>.</summary>
        public static bool IsCard(UpgradeData upgrade) => upgrade != null && Offers.ContainsKey(upgrade);

        /// <summary>The weapon a Limit Break card strengthens.</summary>
        public static AutoAttackWeapon WeaponOf(UpgradeData card) =>
            card != null && Offers.TryGetValue(card, out var weapon) ? weapon : null;

        /// <summary>Applies a Limit Break card. False when <paramref name="card"/> is not one (or its weapon is gone).</summary>
        public static bool TryApplyCard(UpgradeData card)
        {
            var weapon = WeaponOf(card);
            if (weapon == null) return false;
            Apply(weapon);
            return true;
        }

        /// <summary>
        /// Builds up to <paramref name="count"/> Limit Break cards, each for a different weapon the player really uses
        /// (evolved and union weapons included), chosen at random. Cards from the previous offer are thrown away.
        /// <paramref name="iconFor"/> finds a weapon's level-up icon by its type (may be null).
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

                var type = TypeOf(weapons, pair.Key, out bool known);
                var card = ScriptableObject.CreateInstance<UpgradeData>();
                card.name = "LimitBreak_" + pair.Key.name;
                card.UpgradeName = "Limit Break: " + ReadableName(pair.Key.name);
                card.Type = type;
                card.Level = weapons.GetWeaponLevel(type) + 1;
                card.Icon = known && iconFor != null ? iconFor(type) : null;
                int next = CardsFor(pair.Value) + 1;
                card.Description = $"+{Mathf.RoundToInt(DamagePerCard * 100f)}% damage beyond max level " +
                                   $"(Limit Break {next}: x{1f + DamagePerCard * next:0.0} damage).";
                Offers[card] = pair.Value;
                result.Add(card);
            }
            return result;
        }

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
