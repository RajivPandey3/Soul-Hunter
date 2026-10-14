using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS enemy minimum in the real game: the stage opens with a full horde instead of one
    /// enemy per spawn interval, and killing the whole horde refills it straight away.
    /// </summary>
    public class EnemyMinimumPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator Swarm_IsKeptAtTheWaveMinimum_AndRefilledAfterAWipe()
        {
            yield return BootIntoGameplay();

            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            Assert.IsNotNull(spawner, "No EnemySpawner in Main_Gameplay.");
            var wave = GetPrivateField<WaveData>(spawner, "_currentWave");
            Assert.IsNotNull(wave, "EnemySpawner has no wave for the first stage.");
            int minimum = wave.GetPhase(LevelProgressionManager.Instance.StageElapsedTime).MinimumEnemies;
            Assert.That(minimum, Is.GreaterThan(0), "The opening phase has no enemy minimum.");

            // One enemy per interval would take far longer than this to reach the minimum.
            yield return WaitUntilRealtime(() => EnemyController.ActiveEnemies.Count >= minimum,
                $"the opening horde to reach the minimum of {minimum}");

            foreach (var enemy in EnemyController.ActiveEnemies.ToArray())
            {
                var health = enemy != null ? enemy.GetComponent<HealthController>() : null;
                if (health != null) health.KillSilently();
            }
            yield return WaitUntilRealtime(() => EnemyController.ActiveEnemies.Count >= minimum,
                $"the horde to refill to {minimum} after every enemy was killed");

            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        /// <summary>
        /// Memory (CTO MIS M6/M19, 2026-10-13): a 30-minute build run grew by 311 MB because pooled bodies of enemy types
        /// that no longer spawn were kept for ever. A pool nobody has spawned from for PoolIdleSeconds is emptied, a few
        /// bodies per frame; a pool still in use is left alone.
        /// </summary>
        [UnityTest]
        public IEnumerator PooledBodies_OfATypeThatNoLongerSpawns_AreFreed_AFewPerFrame()
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            float idle = EnemySpawner.PoolIdleSeconds;
            try
            {
                // A type the Level 1 waves do not use now: the level's elite, spawned ten times and then put back.
                var spawn = typeof(EnemySpawner).GetMethod("SpawnEnemy", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var bodies = new System.Collections.Generic.List<GameObject>();
                for (int i = 0; i < 10; i++) bodies.Add((GameObject)spawn.Invoke(spawner, new object[] { spawner.StageElitePrefab }));
                Assert.That(bodies, Has.All.Not.Null);
                foreach (var body in bodies) body.SetActive(false);
                int pooled = spawner.PooledEnemies;
                Assert.That(pooled, Is.GreaterThanOrEqualTo(10), "The ten bodies went back to their pool.");
                yield return null;
                Assert.That(bodies.TrueForAll(b => b != null), Is.True, "A pool used a moment ago is kept.");

                EnemySpawner.PoolIdleSeconds = 0.2f;
                // Real time: a level-up from the opening horde can hold game time while this waits.
                yield return WaitUntilRealtime(() => bodies.Exists(b => b == null), "an idle pool to start being freed");
                yield return WaitUntilRealtime(() => bodies.TrueForAll(b => b == null), "every idle pooled body to be freed");
                Assert.That(EnemyController.ActiveEnemies.Count, Is.GreaterThan(0), "The horde that is still spawning is untouched.");
            }
            finally { EnemySpawner.PoolIdleSeconds = idle; }
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
