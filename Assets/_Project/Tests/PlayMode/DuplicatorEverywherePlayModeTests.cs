using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using UpgradeType = SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Owner (2026-10-03): "duplicater ko check karo kya wo har bar kaam karta hai". vampire.survivors.wiki/w/Duplicator
    /// (+1 Amount per level, 2 levels) and /w/Amount ("how many extra projectiles most weapons have"); each weapon's own page
    /// says whether Amount affects it (fetched 2026-10-03). DuplicatorPlayModeTests covers the 14 base weapons; this covers
    /// the evolutions that use Amount, the weapons whose pages say they ignore it, and a weapon gained after the Duplicator.
    /// </summary>
    public class DuplicatorEverywherePlayModeTests : GameplayPlayModeTestBase
    {
        // Wiki "amount=1" (affected): Thousand Edge, Bloody Tear, Holy Wand, Death Spiral, Heaven Sword, Unholy Vespers,
        // La Borra, Hellfire, NO FUTURE, Thunder Loop.
        private static readonly string[] AmountEvolutions =
        {
            "Evo_ThousandEdge", "Evo_BloodyTear", "Evo_HolyWand", "Evo_DeathSpiral", "Evo_HeavenSword", "Evo_UnholyVespers",
            "Evo_DrowningChalice", "Evo_HellmawComet", "Evo_SeveredFate", "Evo_StormboundHalo",
        };

        // Wiki: Garlic, Song of Mana, Pentagram, Clock Lancet and Laurel ignore Amount.
        private static readonly UpgradeType[] IgnoringWeapons =
        {
            UpgradeType.Garlic, UpgradeType.SongOfMana, UpgradeType.Pentagram, UpgradeType.ClockLancet, UpgradeType.Laurel,
        };

        // Wiki: Soul Eater, Mannajja and Gorgeous Moon ignore Amount.
        private static readonly string[] IgnoringEvolutions = { "Evo_SoulEater", "Evo_RequiemOfSouls", "Evo_EclipseSigil" };

        [UnityTest]
        public IEnumerator AnEvolutionThatUsesAmount_FiresTwoMore([ValueSource(nameof(AmountEvolutions))] string evolutionAsset)
        {
            yield return Prepare();
            AutoAttackWeapon weapon = null;
            yield return Evolve(evolutionAsset, w => weapon = w);
            yield return CompareShots(weapon, evolutionAsset, expectMore: true);
        }

        [UnityTest]
        public IEnumerator AWeaponThatIgnoresAmount_FiresTheSame([ValueSource(nameof(IgnoringWeapons))] UpgradeType type)
        {
            yield return Prepare();
            var weapons = WeaponManager.Instance;
            weapons.GiveWeapon(type, 1);
            yield return null;
            var weapon = FindWeapon(weapons.GetWeaponObject(type).name);
            Assert.IsNotNull(weapon, $"{type} is equipped.");
            weapon.gameObject.SetActive(true);
            yield return CompareShots(weapon, type.ToString(), expectMore: false);
        }

        [UnityTest]
        public IEnumerator AnEvolutionThatIgnoresAmount_FiresTheSame([ValueSource(nameof(IgnoringEvolutions))] string evolutionAsset)
        {
            yield return Prepare();
            AutoAttackWeapon weapon = null;
            yield return Evolve(evolutionAsset, w => weapon = w);
            yield return CompareShots(weapon, evolutionAsset, expectMore: false);
        }

        [UnityTest]
        public IEnumerator AWeaponGainedAfterTheDuplicator_FiresTwoMoreAtOnce()
        {
            yield return Prepare();
            GiveDuplicator();
            Assert.That(PlayerController.Instance.GetComponent<PlayerStats>().Amount, Is.EqualTo(2));
            var weapons = WeaponManager.Instance;
            weapons.GiveWeapon(UpgradeType.Knife, 1);
            yield return null;
            var knife = FindWeapon(weapons.GetWeaponObject(UpgradeType.Knife).name);
            Assert.IsNotNull(knife);
            yield return new WaitForSeconds(1.2f);
            var shots = new List<DuplicatorPlayModeTests.Shot>();
            yield return DuplicatorPlayModeTests.MeasureOneAttack(knife, shots);
            Assert.That(shots.Count, Is.GreaterThanOrEqualTo(3), $"Knife level 1 (1) + Duplicator 2 = 3 knives; saw {shots.Count}.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        // ------------------------------------------------------------------ helpers

        // Long enough for a whole volley: Drowning Chalice drops 4 + Amount pools 0.3 s apart (6 pools = 1.5 s), which a
        // 1 s window cut at 4. Below every cooldown that matters here (Drowning Chalice 4 s).
        private const float Window = 2.5f;

        private IEnumerator Prepare()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var existing in player.GetComponentsInChildren<AutoAttackWeapon>()) existing.gameObject.SetActive(false);
            // No kills and no gems during the test: a level-up screen would pause the game-time waits.
            foreach (var other in EnemyController.ActiveEnemies.ToList()) if (other != null) other.gameObject.SetActive(false);
            foreach (var gem in Object.FindObjectsByType<XPGem>(FindObjectsSortMode.None)) gem.gameObject.SetActive(false);
            yield return CloseOpenLevelUps();
            // Unkillable targets all round, so aimed weapons have something to fire at.
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            var enemyPrefab = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs")[0];
            for (int i = 0; i < 12; i++)
            {
                var enemy = Object.Instantiate(enemyPrefab, player.transform.position + Quaternion.Euler(0f, i * 30f, 0f) * Vector3.forward * 4f, Quaternion.identity);
                enemy.GetComponent<HealthController>().Initialize(1000000);
                // Stage bosses are immune to instant kills (VS Pentagram / Gorgeous Moon): Eclipse Sigil would erase the
                // targets, their gems would level the hero and the level-up screen would stop the game-time waits.
                enemy.GetComponent<EnemyController>().IsStageBoss = true;
                var touch = enemy.GetComponent<TouchDamage>();
                if (touch != null) touch.enabled = false;
                var body = enemy.GetComponent<Rigidbody>();
                if (body != null) body.isKinematic = true;
            }
            yield return null;
        }

        private IEnumerator Evolve(string evolutionAsset, System.Action<AutoAttackWeapon> found)
        {
            var weapons = WeaponManager.Instance;
            // The older evolutions are in the scene's list, the newer ones come from GameContent: WeaponManager holds both.
            var evolution = GetPrivateField<List<WeaponEvolutionData>>(weapons, "_availableEvolutions")
                .FirstOrDefault(e => e != null && e.name == evolutionAsset);
            Assert.IsNotNull(evolution, $"{evolutionAsset} is one of WeaponManager's evolutions.");
            weapons.GiveWeapon(evolution.BaseWeapon, evolution.BaseWeaponMaxLevel);
            weapons.GiveWeapon(evolution.RequiredPassive, Mathf.Max(1, evolution.RequiredPassiveMaxLevel));
            yield return null;
            Assert.IsTrue(weapons.TryEvolveWeapon(out string evolved), $"{evolutionAsset} evolves.");
            yield return null;
            var weapon = weapons.GetEquippedWeapons().Select(p => p.Value).LastOrDefault();
            Assert.IsNotNull(weapon, $"{evolved} is equipped.");
            // Only the evolution fires during the measurement.
            foreach (var other in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>())
                if (other != weapon) other.gameObject.SetActive(false);
            found(weapon);
        }

        private IEnumerator CompareShots(AutoAttackWeapon weapon, string label, bool expectMore)
        {
            yield return new WaitForSeconds(1.5f); // the shot fired on gain dies down
            bool books = weapon is SoulHunter.Gameplay.Weapons.BibleWeapon;
            if (books) { weapon.ResetCooldownAndFire(); yield return null; yield return null; }
            int booksBefore = DuplicatorPlayModeTests.LiveBooks();
            var without = new List<DuplicatorPlayModeTests.Shot>();
            yield return DuplicatorPlayModeTests.MeasureOneAttack(weapon, without, Window);

            GiveDuplicator();
            Assert.That(PlayerController.Instance.GetComponent<PlayerStats>().Amount, Is.EqualTo(2), "Duplicator level 2 gives +2 Amount.");
            yield return new WaitForSeconds(1.5f);
            // Let the first measurement's shots end: a Drowning Chalice pool lives 4 s, and one reused by the pool in the
            // same frame, at the same distance from the hero, was not seen as a new shot (2026-10-10, 5 counted of 6).
            float clear = Time.time + 6f;
            var timer = typeof(AutoAttackWeapon).GetField("_timer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            while (Time.time < clear && DuplicatorPlayModeTests.LiveShots().Any()) { timer.SetValue(weapon, 999f); yield return null; }

            if (books)
            {
                weapon.ResetCooldownAndFire();
                yield return null;
                yield return null;
                int booksAfter = DuplicatorPlayModeTests.LiveBooks();
                if (expectMore) Assert.That(booksAfter, Is.EqualTo(booksBefore + 2), $"{label}: two more books ({booksBefore} before).");
                else Assert.That(booksAfter, Is.EqualTo(booksBefore), $"{label}: the same books.");
                Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
                yield break;
            }

            var with = new List<DuplicatorPlayModeTests.Shot>();
            yield return DuplicatorPlayModeTests.MeasureOneAttack(weapon, with, Window);
            string detail = $"{label}: {without.Count} shots without, {with.Count} with Duplicator 2. With: " + string.Join("; ", with.Select(s => s.ToString()));
            Debug.Log("[DuplicatorEverywhere] " + detail);
            if (expectMore)
            {
                Assert.That(with.Count, Is.GreaterThanOrEqualTo(without.Count + 2), "Two more shots. " + detail);
                Assert.That(DuplicatorPlayModeTests.StackedPairs(with), Is.EqualTo(0), "Every extra shot visible on its own. " + detail);
            }
            else Assert.That(with.Count, Is.EqualTo(without.Count), "The wiki says this weapon ignores Amount. " + detail);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static void GiveDuplicator()
        {
            var dup = ScriptableObject.CreateInstance<UpgradeData>();
            dup.Type = UpgradeType.Duplicator;
            try
            {
                dup.Level = 1; WeaponManager.Instance.ApplyUpgrade(dup);
                dup.Level = 2; WeaponManager.Instance.ApplyUpgrade(dup);
            }
            finally { Object.Destroy(dup); }
        }

        private static AutoAttackWeapon FindWeapon(string prefabName) =>
            PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>(true).FirstOrDefault(w => w.gameObject.name.StartsWith(prefabName));
    }
}
