using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS DOT Phase 1 row 9 (2026-10-01). vampire.survivors.wiki/w/Enemies: "Enemies generally spawn just outside the
    /// screen." They spawned at 20 units, which is inside a 16:9 view's sides (about 21 units).
    /// </summary>
    public class OffScreenSpawnPlayModeTests : GameplayPlayModeTestBase
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator NewEnemies_AppearJustOutsideTheScreen()
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>()) weapon.enabled = false;
            yield return null;
            var camera = Camera.main;
            var player = PlayerController.Instance.transform;
            var prefab = (GameObject)typeof(EnemySpawner).GetField("_stageEnemyPrefab", Private).GetValue(spawner);
            var spawn = typeof(EnemySpawner).GetMethod("SpawnEnemy", Private);
            Assert.That(prefab, Is.Not.Null);

            for (int i = 0; i < 40; i++)
            {
                var enemy = (GameObject)spawn.Invoke(spawner, new object[] { prefab });
                if (enemy == null) continue;
                Vector3 at = enemy.transform.position;
                Vector3 v = camera.WorldToViewportPoint(at);
                bool onScreen = v.z > 0f && v.x > 0f && v.x < 1f && v.y > 0f && v.y < 1f;
                Assert.That(onScreen, Is.False, $"Enemy {i} appeared on screen at viewport {v}.");
                Vector3 dir = at - player.position; dir.y = 0f;
                float edge = EnemySpawner.DistanceToScreenEdge(camera, player.position, dir, 20f);
                float past = dir.magnitude - edge;
                // Obstacles may push a spawn to another direction; it must still be close to that direction's edge.
                Assert.That(past, Is.InRange(0f, EnemySpawner.OffScreenMargin + EnemySpawner.OffScreenSpread + 0.5f),
                    $"Enemy {i} is just past the edge ({past:0.0} units beyond it).");
            }
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        /// <summary>VS (wiki Curse): "Total enemy quantity allowed on screen is capped at 500, regardless of Curse and Charm".</summary>
        [UnityTest]
        public IEnumerator MapEvents_StopAt500Enemies()
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>()) weapon.enabled = false;
            var prefab = (GameObject)typeof(EnemySpawner).GetField("_stageEnemyPrefab", Private).GetValue(spawner);
            var spawn = typeof(EnemySpawner).GetMethod("SpawnEnemy", Private);
            int guard = 0;
            while (EnemyController.ActiveEnemies.Count < EnemySpawner.HardCap - 5 && guard++ < 1000) spawn.Invoke(spawner, new object[] { prefab });
            yield return null;
            int before = EnemyController.ActiveEnemies.Count;
            var members = spawner.TriggerStageEvent(SoulHunter.Gameplay.Data.StageEventType.Swarm, 30);
            Assert.That(members.Count, Is.LessThanOrEqualTo(EnemySpawner.HardCap - before), "The swarm stops at the cap.");
            Assert.That(EnemyController.ActiveEnemies.Count, Is.LessThanOrEqualTo(EnemySpawner.HardCap), "At most 500 enemies.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
