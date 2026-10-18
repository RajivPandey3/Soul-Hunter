using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using UpgradeType = SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS Pentagram (vampire.survivors.wiki/w/Pentagram, checked 2026-09-27): it erases enemies and their drops, the
    /// 25-minute bosses are immune, and "if a boss that would have dropped [a chest] is erased, the Treasure Chest is
    /// then dropped by a different enemy". Found in editor play (autoplay-editor-32): Pentagram erased the Stage 1 boss
    /// in 0.2 s and the 5:00 chest elite, losing both chests, and left EnemyDrop switched off on pooled enemies.
    /// </summary>
    public class PentagramPlayModeTests : GameplayPlayModeTestBase
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private EnemySpawner _spawner;
        private PentagramWeapon _pentagram;

        private IEnumerator Setup()
        {
            yield return BootIntoGameplay();
            _spawner = Object.FindFirstObjectByType<EnemySpawner>();
            _spawner.enabled = false;
            EnemyDrop.PendingChests.Clear();
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null) stray.gameObject.SetActive(false);
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            WeaponManager.Instance.GiveWeapon(UpgradeType.Pentagram, 1);
            yield return null;
            _pentagram = player.GetComponentsInChildren<PentagramWeapon>(true).First();
            _pentagram.enabled = false; // fired by the test, not by its cooldown
            // Below any roll: GroundItemEraser rounds the roll to 0.01, and a roll under 0.005 is 0, which is not > 0, so a
            // 0 threshold still kept everything 0.5% of the time (2 failures in 108 runs, 2026-10-08 and 2026-10-09).
            typeof(PentagramWeapon).GetField("_chanceToKeepGems", Private).SetValue(_pentagram, -1f);
        }

        private void Fire() => typeof(PentagramWeapon).GetMethod("Attack", Private).Invoke(_pentagram, null);

        private GameObject Spawn(GameObject prefab, Vector3 offset)
        {
            var enemy = (GameObject)typeof(EnemySpawner).GetMethod("SpawnEnemy", Private).Invoke(_spawner, new object[] { prefab });
            enemy.transform.position = PlayerController.Instance.transform.position + offset;
            var drop = enemy.GetComponent<EnemyDrop>();
            if (drop != null) drop.ChickenDropChance = 0f; // a gem every time
            return enemy;
        }

        private static int Chests() => Object.FindObjectsByType<ChestPickup>(FindObjectsSortMode.None).Count(c => c.gameObject.activeInHierarchy);
        private static int Gems() => XPGem.ActiveGems.Count(g => g != null && g.isActiveAndEnabled);

        [UnityTest]
        public IEnumerator Bosses_AreImmune_AndAnErasedEliteHandsItsChestOn()
        {
            yield return Setup();
            var bossPrefab = GetPrivateField<GameObject>(_spawner, "_stageBossPrefab");
            Assert.IsTrue((bool)typeof(EnemySpawner).GetMethod("SpawnBoss", Private).Invoke(_spawner, new object[] { bossPrefab }));
            var boss = EnemyController.ActiveEnemies.Last(e => e != null && e.IsStageBoss);
            var elite = (GameObject)typeof(EnemySpawner).GetMethod("SpawnChestElite", Private).Invoke(_spawner, null);
            elite.transform.position = PlayerController.Instance.transform.position + Vector3.right * 6f;
            var enemyPrefab = GetPrivateField<GameObject>(_spawner, "_stageEnemyPrefab");
            var ordinary = Spawn(enemyPrefab, Vector3.left * 6f);
            int kills = RunStatsTracker.Instance.TotalKills, chests = Chests(), gems = Gems();

            Fire();
            yield return null;

            Assert.That(boss.isActiveAndEnabled && boss.GetComponent<HealthController>().CurrentHealth > 0, Is.True, "VS: the stage boss is immune to Pentagram.");
            Assert.That(elite.activeInHierarchy && elite.GetComponent<HealthController>().CurrentHealth > 0, Is.False, "The chest elite is erased.");
            Assert.That(ordinary.activeInHierarchy && ordinary.GetComponent<HealthController>().CurrentHealth > 0, Is.False, "The ordinary enemy is erased.");
            Assert.That(RunStatsTracker.Instance.TotalKills, Is.EqualTo(kills + 2), "Erased enemies still count as kills.");
            Assert.That(Chests(), Is.EqualTo(chests), "The erased elite's chest is not dropped where it died...");
            Assert.That(Gems(), Is.EqualTo(gems), "...and erased enemies leave no gems (chance to keep set to 0).");
            Assert.That(EnemyDrop.PendingChests.Count, Is.EqualTo(1), "...but its chest passes on to another enemy.");

            // The next ordinary enemy of the horde carries it.
            var carrier = Spawn(enemyPrefab, Vector3.forward * 6f);
            typeof(EnemySpawner).GetMethod("GivePendingChest", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { carrier });
            Assert.That(carrier.GetComponent<EnemyDrop>().WillDropChest, Is.True);
            Assert.That(EnemyDrop.PendingChests.Count, Is.EqualTo(0));
            carrier.GetComponent<HealthController>().TakeDamage(new DamagePacket(1000000, carrier.transform.position, Vector3.zero, DamageType.Normal));
            yield return WaitUntilRealtime(() => Chests() == chests + 1, "the carrier to drop the passed-on chest");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator AnErasedCarrier_PassesOnItsBossesChestTiers()
        {
            // P1-14: the passed-on chest keeps the erased boss's config (vampire.survivors.wiki/w/Mad_Forest, 5:00 Mantichana:
            // {{treasure|level=1|evo=|tier3=1|tier2=5|tier1=100|reward1=weapon}}), so it stays a bronze chest that cannot
            // evolve, even when the next carrier dies after 10:00.
            yield return Setup();
            var elite = (GameObject)typeof(EnemySpawner).GetMethod("SpawnChestEliteAt", Private).Invoke(_spawner, new object[] { 5f });
            elite.transform.position = PlayerController.Instance.transform.position + Vector3.right * 6f;
            Fire();
            yield return null;
            Assert.That(EnemyDrop.PendingChests.Count, Is.EqualTo(1), "The erased 5:00 carrier's chest waits for another enemy.");
            Assert.That(EnemyDrop.PendingChests.Peek(), Is.EqualTo(ChestTiers.Minute5));

            LevelProgressionManager.Instance.SkipTo(10f * 60f + 30f);
            yield return null;
            var enemyPrefab = GetPrivateField<GameObject>(_spawner, "_stageEnemyPrefab");
            var carrier = Spawn(enemyPrefab, Vector3.forward * 6f);
            typeof(EnemySpawner).GetMethod("GivePendingChest", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { carrier });
            Assert.That(carrier.GetComponent<EnemyDrop>().ChestTiersAtDeath, Is.EqualTo(ChestTiers.Minute5));
            var before = Object.FindObjectsByType<ChestPickup>(FindObjectsSortMode.None).Where(c => c.gameObject.activeInHierarchy).ToList();
            carrier.GetComponent<HealthController>().TakeDamage(new DamagePacket(1000000, carrier.transform.position, Vector3.zero, DamageType.Normal));
            ChestPickup chest = null;
            yield return WaitUntilRealtime(() => (chest = Object.FindObjectsByType<ChestPickup>(FindObjectsSortMode.None)
                .FirstOrDefault(c => c.gameObject.activeInHierarchy && !before.Contains(c))) != null, "the carrier to drop the passed-on chest");
            Assert.That(chest.Tiers, Is.EqualTo(ChestTiers.Minute5));
            Assert.That(chest.CanEvolve, Is.False, "Still the 5:00 chest: no evolution after 10:00.");
            Assert.That(chest.Kind, Is.EqualTo(ChestKind.Bronze));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator AnErasedEnemy_DropsAgain_WhenThePoolReusesIt()
        {
            yield return Setup();
            var enemyPrefab = GetPrivateField<GameObject>(_spawner, "_stageEnemyPrefab");
            var enemy = Spawn(enemyPrefab, Vector3.right * 5f);
            Fire();
            yield return WaitUntilRealtime(() => !enemy.activeInHierarchy, "the erased enemy to return to its pool");

            var reused = Spawn(enemyPrefab, Vector3.right * 5f);
            Assert.AreSame(enemy, reused, "The pool should hand back the same enemy.");
            Assert.That(reused.GetComponent<EnemyDrop>().enabled, Is.True, "Pentagram used to leave EnemyDrop switched off.");
            int kills = RunStatsTracker.Instance.TotalKills, gems = Gems();
            reused.GetComponent<HealthController>().TakeDamage(new DamagePacket(1000000, reused.transform.position, Vector3.zero, DamageType.Normal));
            yield return null;
            Assert.That(Gems(), Is.EqualTo(gems + 1), "A reused enemy drops its gem again.");
            Assert.That(RunStatsTracker.Instance.TotalKills, Is.EqualTo(kills + 1));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        /// <summary>
        /// CMB-02 (2026-10-19). VS wiki Pentagram: "instantly erasing every enemy, their drops, and all items on the
        /// screen"; level 1 "Erases everything in sight". Before, every active enemy on the map was erased.
        /// </summary>
        [UnityTest]
        public IEnumerator OnlyEnemiesOnTheScreen_AreErased()
        {
            yield return Setup();
            var enemyPrefab = GetPrivateField<GameObject>(_spawner, "_stageEnemyPrefab");
            var near = Spawn(enemyPrefab, Vector3.right * 5f);
            var far = Spawn(enemyPrefab, Vector3.right * 80f);
            var camera = Camera.main;
            Assert.IsNotNull(camera);
            Vector3 v = camera.WorldToViewportPoint(far.transform.position);
            Assert.That(v.x > 1f || v.x < 0f || v.y > 1f || v.y < 0f, Is.True, "Precondition: the far enemy is off the screen.");
            Fire();
            yield return WaitUntilRealtime(() => !near.activeInHierarchy, "the enemy on the screen to be erased");
            Assert.That(far.activeInHierarchy, Is.True, "An enemy off the screen is not erased.");
            Assert.That(far.GetComponent<HealthController>().CurrentHealth, Is.GreaterThan(0));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
