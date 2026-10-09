using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// A sample of the 18 Arcanas added by release item P2-01 in a real run, each with its own wiki page's number (raw,
    /// fetched 2026-10-09; quoted in ArcanaEffects.cs): Mending Wrath (Sarabande VI), Ironblood Oath (Divine Bloodline IX),
    /// First Blade (Beginning X), Empty Hands (Silent Old Sanctuary XX), Gathering Call (Mad Groove VIII), Soul Barrage
    /// (Game Killer 0), Shattering Frost (Out of Bounds XII) and Changing Fortunes (Wicked Season XIII). Every test plays on a
    /// throw-away save (GameplayPlayModeTestBase).
    /// </summary>
    public class ArcanaCardEffectsPlayModeTests : GameplayPlayModeTestBase
    {
        /// <summary>A quiet field: no spawner, the starting weapons off, no horde or gems, then the card is given.</summary>
        private IEnumerator QuietFieldWith(ArcanaData.ArcanaType type)
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var player = PlayerController.Instance;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            foreach (var enemy in EnemyController.ActiveEnemies.ToList()) if (enemy != null) enemy.gameObject.SetActive(false);
            foreach (var gem in Object.FindObjectsByType<XPGem>(FindObjectsSortMode.None)) gem.gameObject.SetActive(false);
            yield return CloseOpenLevelUps();
            var arcanas = ArcanaManager.Instance;
            var card = arcanas.AllArcanas.First(a => a.Type == type);
            arcanas.ApplyArcana(card);
            Assert.That(arcanas.HasArcana(type), Is.True);
            yield return null;
        }

        /// <summary>A still enemy that cannot hurt the hero, with <paramref name="health"/> health.</summary>
        private static HealthController Dummy(Vector3 at, int health = 100000)
        {
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            var prefab = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs")[0];
            var enemy = Object.Instantiate(prefab, at, Quaternion.identity);
            var hp = enemy.GetComponent<HealthController>();
            hp.Initialize(health);
            var touch = enemy.GetComponent<TouchDamage>();
            if (touch != null) touch.enabled = false;
            var body = enemy.GetComponent<Rigidbody>();
            if (body != null) body.isKinematic = true;
            var controller = enemy.GetComponent<EnemyController>();
            if (controller != null) controller.enabled = true;
            return hp;
        }

        private static XPGem NearestGem(Vector3 at) =>
            XPGem.ActiveGems.Where(g => g != null && g.isActiveAndEnabled).OrderBy(g => (g.transform.position - at).sqrMagnitude).FirstOrDefault();

        [UnityTest]
        public IEnumerator MendingWrath_DoublesHealing_AndTheHealStrikesNearbyEnemies()
        {
            yield return QuietFieldWith(ArcanaData.ArcanaType.SarabandeOfHealing);
            var player = PlayerController.Instance;
            var health = player.GetComponent<HealthController>();
            health.TakeDamage(new DamagePacket(50, player.transform.position, Vector3.zero) { IgnoresHitInvulnerability = true });
            int before = health.CurrentHealth;
            var near = Dummy(player.transform.position + Vector3.right * 1.5f);
            int nearBefore = near.CurrentHealth;
            health.Heal(10);
            Assert.That(health.CurrentHealth, Is.EqualTo(before + 20), "VS: 'Healing is doubled.'");
            yield return null;
            yield return null;
            Assert.That(nearBefore - near.CurrentHealth, Is.GreaterThanOrEqualTo(20), "VS: 'Recovering HP damages nearby enemies for the same amount' (no Might). " + Errors());
        }

        [UnityTest]
        public IEnumerator IronbloodOath_TouchingEnemiesTake10PerArmor()
        {
            yield return QuietFieldWith(ArcanaData.ArcanaType.DivineBloodline);
            var player = PlayerController.Instance;
            player.Stats.AddArmor(2 - player.Stats.Armor);
            var touching = Dummy(player.transform.position + Vector3.forward * 0.8f);
            var far = Dummy(player.transform.position + Vector3.forward * 6f);
            int before = touching.CurrentHealth;
            yield return new WaitForSeconds(0.6f);
            int taken = before - touching.CurrentHealth;
            Assert.That(taken, Is.GreaterThanOrEqualTo(20 - 0), "VS: 'Enemies that touch the character take 10 flat damage per Armor' (Armor 2).");
            Assert.That(taken % 20, Is.EqualTo(0), "Each touch is 20 (the dummy's own armor is 0). " + Errors());
            Assert.That(far.CurrentHealth, Is.EqualTo(100000), "Only enemies touching the hero.");
        }

        [UnityTest]
        public IEnumerator FirstBlade_GivesTheStartingWeaponPlusThreeAmount()
        {
            yield return QuietFieldWith(ArcanaData.ArcanaType.Beginning);
            var weapons = WeaponManager.Instance;
            var start = PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>(true)
                .First(w => ArcanaManager.BaseTypeOf(ArcanaManager.Identify(w)) == weapons.StartingWeapon);
            start.gameObject.SetActive(true);
            Assert.That(ArcanaManager.AmountBonusFor(start), Is.EqualTo(3), "VS: 'the character's starting weapon gains +3 Amount'.");
            Assert.That(start.ZoneAmount, Is.EqualTo(1 + PlayerController.Instance.Stats.Amount + 3));
            // Bone and Cherry Bomb: +1 when they are not the starting weapon.
            var bone = new GameObject("Bone_Test").AddComponent<SoulHunter.Gameplay.Combat.BoneWeapon>();
            Assert.That(ArcanaManager.AmountBonusFor(bone), Is.EqualTo(weapons.StartingWeapon == UpgradeData.UpgradeType.Bone ? 3 : 1));
            Object.Destroy(bone.gameObject);
        }

        [UnityTest]
        public IEnumerator EmptyHands_GivesChargesAndMightForEachEmptyWeaponSlot()
        {
            yield return BootIntoGameplay();
            var levelUp = Object.FindFirstObjectByType<LevelUpManager>();
            int rerolls = levelUp.RerollsLeft, skips = levelUp.SkipsLeft, banishes = levelUp.BanishesLeft;
            var stats = PlayerController.Instance.Stats;
            float might = stats.Might, cooldown = stats.Cooldown;
            var arcanas = ArcanaManager.Instance;
            arcanas.ApplyArcana(arcanas.AllArcanas.First(a => a.Type == ArcanaData.ArcanaType.SilentOldSanctuary));
            Assert.That(levelUp.RerollsLeft, Is.EqualTo(rerolls + 3));
            Assert.That(levelUp.SkipsLeft, Is.EqualTo(skips + 3));
            Assert.That(levelUp.BanishesLeft, Is.EqualTo(banishes + 3));
            int empty = ArcanaManager.SanctuaryEmptySlots(WeaponManager.Instance.MaxWeaponSlots, WeaponManager.Instance.GetActiveWeaponsCount());
            Assert.That(empty, Is.GreaterThan(0));
            Assert.That(stats.Might, Is.EqualTo(Mathf.Min(might + 0.2f * empty, PlayerStats.MaxMight)).Within(0.001f), "+20% Might per empty slot.");
            Assert.That(stats.Cooldown, Is.EqualTo(Mathf.Max(0.1f, cooldown - 0.08f * empty)).Within(0.001f), "-8% Cooldown per empty slot.");
        }

        [UnityTest]
        public IEnumerator GatheringCall_PullsGemsAndChestsIntoARingAroundTheHero()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var player = PlayerController.Instance;
            var pool = PickupPoolManager.Instance;
            Vector3 gemAt = player.transform.position + new Vector3(25f, 0f, 0f);
            pool.SpawnGem(gemAt, 10);
            var gem = NearestGem(gemAt);
            var chest = pool.SpawnChest(player.transform.position + new Vector3(0f, 0f, -30f));
            yield return null;
            var arcanas = ArcanaManager.Instance;
            arcanas.ApplyArcana(arcanas.AllArcanas.First(a => a.Type == ArcanaData.ArcanaType.MadGroove));
            Assert.That(arcanas.MadGrooveTriggers, Is.EqualTo(1), "VS: 'upon acquiring the Arcana' it triggers at once.");
            foreach (var t in new[] { gem != null ? gem.transform : null, chest != null ? chest.transform : null })
            {
                if (t == null) continue;
                Vector3 offset = t.position - player.transform.position;
                offset.y = 0f;
                Assert.That(offset.magnitude, Is.LessThan(ArcanaManager.MadGrooveRing + 0.5f), $"{t.name} is in the ring.");
            }
        }

        [UnityTest]
        public IEnumerator SoulBarrage_GemsGiveNoExperience_AndExplodeOnTheNearestEnemy()
        {
            yield return QuietFieldWith(ArcanaData.ArcanaType.GameKiller);
            var player = PlayerController.Instance;
            var experience = player.GetComponent<PlayerExperience>();
            int xp = experience.CurrentXP, level = experience.CurrentLevel;
            var target = Dummy(player.transform.position + Vector3.right * 3f);
            int hp = target.CurrentHealth;
            Vector3 gemAt = player.transform.position + Vector3.left * 1f;
            PickupPoolManager.Instance.SpawnGem(gemAt, 10);
            Assert.IsNotNull(NearestGem(gemAt), "A gem lies by the hero.");
            float end = Time.realtimeSinceStartup + 5f;
            while (target.CurrentHealth == hp && Time.realtimeSinceStartup < end) yield return null;
            Assert.That(experience.CurrentXP, Is.EqualTo(xp), "VS: 'Halts XP gain.'");
            Assert.That(experience.CurrentLevel, Is.EqualTo(level));
            Assert.That(hp - target.CurrentHealth, Is.GreaterThanOrEqualTo(10), "The gem flew at the enemy and exploded (10 x Growth x Might). " + Errors());
        }

        [UnityTest]
        public IEnumerator ShatteringFrost_AFrozenEnemyBursts_AndHurtsTheOnesAround()
        {
            yield return QuietFieldWith(ArcanaData.ArcanaType.OutOfBounds);
            var player = PlayerController.Instance;
            var frozen = Dummy(player.transform.position + Vector3.right * 5f);
            var beside = Dummy(player.transform.position + Vector3.right * 5.8f);
            yield return null; // both are in the live enemy list
            int before = beside.CurrentHealth;
            frozen.GetComponent<EnemyController>().Freeze(1f);
            yield return null;
            float seconds = LevelProgressionManager.Instance.StageElapsedTime;
            Assert.That(before - beside.CurrentHealth, Is.GreaterThanOrEqualTo(Mathf.FloorToInt(ArcanaManager.OutOfBoundsDamage(seconds) * 0.9f)),
                "VS: 'Freezing enemies generates explosions', 10 + seconds / 30 (x Might). " + Errors());
        }

        [UnityTest]
        public IEnumerator ChangingFortunes_DoublesGrowthFirst_ThenLuck()
        {
            yield return QuietFieldWith(ArcanaData.ArcanaType.WickedSeason);
            var stats = PlayerController.Instance.Stats;
            Assert.That(stats.ArcanaGrowthFactor, Is.EqualTo(2f), "Growth first ('in that order').");
            Assert.That(stats.ArcanaLuckFactor, Is.EqualTo(1f));
            float end = Time.realtimeSinceStartup + 15f;
            while (stats.ArcanaLuckFactor < 2f && Time.realtimeSinceStartup < end) yield return null;
            Assert.That(stats.ArcanaLuckFactor, Is.EqualTo(2f), "Then Luck, after 10 seconds.");
            Assert.That(stats.ArcanaGrowthFactor, Is.EqualTo(1f));
        }
    }
}
