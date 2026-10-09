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

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// In the real game: a chest elite is tougher than a normal elite and drops a chest (and a reused
    /// one does not keep dropping chests), and the stage boss's health follows the player's level.
    /// </summary>
    public class ChestEliteAndBossPlayModeTests : GameplayPlayModeTestBase
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        [UnityTest]
        public IEnumerator ChestElite_IsTougher_AndDropsAChest()
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            Assert.IsNotNull(spawner, "No EnemySpawner in Main_Gameplay.");
            spawner.enabled = false; // nothing else spawns or pops the pools during the test
            float curse = (float)typeof(EnemySpawner).GetMethod("CurrentCurse", Private).Invoke(spawner, null);

            var elite = (GameObject)typeof(EnemySpawner).GetMethod("SpawnChestElite", Private).Invoke(spawner, null);
            Assert.IsNotNull(elite, "SpawnChestElite produced nothing. " + Errors());
            var data = elite.GetComponent<EnemyController>().Data;
            var health = elite.GetComponent<HealthController>();
            int level = PlayerController.Instance.GetComponent<PlayerExperience>().CurrentLevel;
            Assert.AreEqual(EnemySpawner.ScaleHealthByCurse(EnemySpawner.ChestEliteHealth(0f, level), curse), health.MaxHealth,
                "A chest elite has the VS chest-boss health (50 x level outside the 5/10/15/20-minute bosses).");

            health.KillSilently();
            yield return WaitUntilRealtime(() => ActiveChests() == 1, "the chest elite to drop a chest");
            // Owner's story: Kael harvests souls, so the elite leaves its soul gem too.
            Assert.That(SoulHunter.Gameplay.Pickups.XPGem.ActiveGems.Any(g => g != null && g.isActiveAndEnabled && g.XPValue == data.ExperienceValue),
                Is.True, $"The chest elite should also drop its {data.ExperienceValue} XP soul gem. " + Errors());

            // Pooled reuse: the same object spawned as an ordinary enemy must not drop a chest.
            yield return WaitUntilRealtime(() => !elite.activeInHierarchy, "the dead elite to return to its pool");
            var elitePrefab = GetPrivateField<GameObject>(spawner, "_stageElitePrefab") ?? GetPrivateField<GameObject>(spawner, "_stageEnemyPrefab");
            var reused = (GameObject)typeof(EnemySpawner).GetMethod("SpawnEnemy", Private).Invoke(spawner, new object[] { elitePrefab });
            Assert.AreSame(elite, reused, "The pool should hand back the same elite.");
            reused.GetComponent<HealthController>().KillSilently();
            for (int i = 0; i < 5; i++) yield return null;
            Assert.AreEqual(1, ActiveChests(), "A reused chest elite dropped a second chest. " + Errors());
        }

        [UnityTest]
        public IEnumerator ChestElite_CarriesItsMinutesTiers()
        {
            // P1-14 (vampire.survivors.wiki/w/Treasure_Chest: "individually configured per boss enemy"; Mad_Forest "Waves"):
            // the chest's config, and so its evolution flag and bronze/silver look, belong to the boss's minute, not to the
            // stage clock when it dies. Before, a 9:00 elite killed after 10:00 dropped a silver chest that could evolve, and
            // a 1:00 elite killed after 2:00 a bronze one that could not.
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            PlayerController.Instance.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            var progression = LevelProgressionManager.Instance;

            var first = (GameObject)typeof(EnemySpawner).GetMethod("SpawnChestEliteAt", Private).Invoke(spawner, new object[] { 1f });
            progression.SkipTo(2f * 60f + 30f);
            yield return null;
            ChestPickup silver = null;
            yield return KillAndFindChest(first, c => silver = c);
            Assert.That(silver.Tiers, Is.EqualTo(ChestTiers.Minute1), "The 1:00 Glowing Bat config: evo=1, 0/0/30.");
            Assert.That(silver.CanEvolve, Is.True, "The 1:00 chest can evolve although its carrier died at 2:30.");
            Assert.That(silver.Kind, Is.EqualTo(ChestKind.Silver));
            silver.gameObject.SetActive(false);

            var third = (GameObject)typeof(EnemySpawner).GetMethod("SpawnChestEliteAt", Private).Invoke(spawner, new object[] { 3f });
            ChestPickup threeMinute = null;
            yield return KillAndFindChest(third, c => threeMinute = c);
            Assert.That(threeMinute.Tiers, Is.EqualTo(ChestTiers.Minute3));
            for (int i = 0; i < 200; i++)
                Assert.That(ChestLogicController.RollItemCount(threeMinute.Tiers, 3f, () => Random.value), Is.Not.EqualTo(5),
                    "The 3:00 config has tier3=0: never 5 items, whatever the Luck.");
            threeMinute.gameObject.SetActive(false);

            var ninth = (GameObject)typeof(EnemySpawner).GetMethod("SpawnChestEliteAt", Private).Invoke(spawner, new object[] { 9f });
            progression.SkipTo(10f * 60f + 5f);
            yield return null;
            ChestPickup bronze = null;
            yield return KillAndFindChest(ninth, c => bronze = c);
            Assert.That(bronze.Tiers, Is.EqualTo(ChestTiers.Minute9));
            Assert.That(bronze.CanEvolve, Is.False, "The 9:00 chest cannot evolve although its carrier died after 10:00.");
            Assert.That(bronze.Kind, Is.EqualTo(ChestKind.Bronze));
            bronze.gameObject.SetActive(false);

            // The stage boss's chest uses the 25:00 Giant Blue Venus config.
            var bossPrefab = GetPrivateField<GameObject>(spawner, "_stageBossPrefab");
            Assert.IsTrue((bool)typeof(EnemySpawner).GetMethod("SpawnBoss", Private).Invoke(spawner, new object[] { bossPrefab }));
            var boss = EnemyController.ActiveEnemies.Last(e => e != null && spawner.IsBoss(e));
            Assert.That(boss.GetComponent<EnemyDrop>().ChestTiersAtDeath, Is.EqualTo(ChestTiers.StageBoss));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private IEnumerator KillAndFindChest(GameObject carrier, System.Action<ChestPickup> found)
        {
            Assert.IsNotNull(carrier, "SpawnChestEliteAt produced nothing. " + Errors());
            var before = Object.FindObjectsByType<ChestPickup>(FindObjectsSortMode.None).Where(c => c.gameObject.activeInHierarchy).ToList();
            carrier.GetComponent<HealthController>().KillSilently();
            ChestPickup chest = null;
            yield return WaitUntilRealtime(() => (chest = Object.FindObjectsByType<ChestPickup>(FindObjectsSortMode.None)
                .FirstOrDefault(c => c.gameObject.activeInHierarchy && !before.Contains(c))) != null, "the chest elite to drop its chest");
            found(chest);
        }

        [UnityTest]
        public IEnumerator StageBoss_HealthFollowsPlayerLevel()
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            var bossPrefab = GetPrivateField<GameObject>(spawner, "_stageBossPrefab");
            Assert.IsNotNull(bossPrefab, "No stage boss prefab for stage 1.");
            float curse = (float)typeof(EnemySpawner).GetMethod("CurrentCurse", Private).Invoke(spawner, null);
            int level = PlayerController.Instance.GetComponent<PlayerExperience>().CurrentLevel;

            Assert.IsTrue((bool)typeof(EnemySpawner).GetMethod("SpawnBoss", Private).Invoke(spawner, new object[] { bossPrefab }));
            var boss = EnemyController.ActiveEnemies.Last();
            var data = bossPrefab.GetComponent<EnemyController>().Data;
            Assert.AreEqual(EnemySpawner.ScaleHealthByCurse(data.HealthPerPlayerLevel * level, curse),
                boss.GetComponent<HealthController>().MaxHealth, "VS HP x Level: per-level health x player level.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator TheHorde_KeepsComing_DuringABossFight()
        {
            // VS: the minute waves go on while the boss lives (Mad Forest keeps a minimum of 100 at 25:00). Found
            // 2026-09-28: the spawner stopped while a boss lived, so the boss stood alone on screen for minutes.
            yield return BootIntoGameplay();
            PlayerController.Instance.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            LevelProgressionManager.Instance.SkipToBoss();
            yield return WaitUntilRealtime(() => EnemyController.ActiveEnemies.Any(e => e != null && spawner.IsBoss(e)), "the stage boss to appear");
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null && !spawner.IsBoss(stray)) stray.gameObject.SetActive(false);
            yield return WaitUntilRealtime(() => EnemyController.ActiveEnemies.Count(e => e != null && !spawner.IsBoss(e)) >= 20,
                "the horde to refill while the boss is alive");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator ABossLeftBehind_IsBroughtBackToTheScreen()
        {
            // VS wiki (Enemies, Bosses): "Bosses do not despawn when the player moves away from them, but get teleported
            // back to the screen." Before 2026-09-28 a boss left far behind stayed there.
            yield return BootIntoGameplay();
            PlayerController.Instance.GetComponent<HealthController>().IsInvincible = true;
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            LevelProgressionManager.Instance.SkipToBoss();
            yield return WaitUntilRealtime(() => EnemyController.ActiveEnemies.Any(e => e != null && spawner.IsBoss(e)), "the stage boss to appear");
            var boss = EnemyController.ActiveEnemies.First(e => e != null && spawner.IsBoss(e));
            var player = PlayerController.Instance.transform.position;
            boss.transform.position = player + new Vector3(100f, 0f, 0f);
            yield return WaitUntilRealtime(() => boss == null || Vector3.Distance(boss.transform.position, PlayerController.Instance.transform.position) < 40f,
                "the boss to be brought back near the player");
            Assert.That(boss != null && boss.isActiveAndEnabled, Is.True, "Still the same, living boss.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static int ActiveChests() =>
            Object.FindObjectsByType<ChestPickup>(FindObjectsSortMode.None).Count(c => c.gameObject.activeInHierarchy);
    }
}
