using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
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
            Assert.AreEqual(EnemySpawner.ScaleHealthByCurse(Mathf.RoundToInt(data.MaxHealth * 5f), curse), health.MaxHealth,
                "Chest elite should have 5x its EnemyData health.");

            health.KillSilently();
            yield return WaitUntilRealtime(() => ActiveChests() == 1, "the chest elite to drop a chest");

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
                boss.GetComponent<HealthController>().MaxHealth, "Boss health should be per-level health x player level.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static int ActiveChests() =>
            Object.FindObjectsByType<ChestPickup>(FindObjectsSortMode.None).Count(c => c.gameObject.activeInHierarchy);
    }
}
