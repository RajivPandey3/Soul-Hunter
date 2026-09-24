using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Stage events in the real game: the default schedule fires on the stage clock, every
    /// formation flies its fixed path instead of chasing, and a flier returned to the pool stays
    /// a trigger (the old fly state made pooled enemies solid).
    /// </summary>
    public class StageEventPlayModeTests : GameplayPlayModeTestBase
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        [UnityTest]
        public IEnumerator ScheduledSwarm_FiresOnTheStageClock()
        {
            yield return BootIntoGameplay();
            var progression = LevelProgressionManager.Instance;
            int fliersBefore = Fliers();

            // Jump the stage clock to just after the first default event (2:30).
            typeof(LevelProgressionManager).GetProperty("StageElapsedTime").GetSetMethod(true).Invoke(progression, new object[] { 151f });
            yield return WaitUntilRealtime(() => Fliers() >= fliersBefore + 20, "the 2:30 swarm to spawn");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator EveryFormation_FliesItsPath_AndPooledFliersStayTriggers()
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false; // only the events under test spawn
            var level = LevelProgressionManager.Instance.CurrentLevel;
            var phase = GetPrivateField<WaveData>(spawner, "_currentWave").GetPhase(0f);
            var run = typeof(EnemySpawner).GetMethod("RunStageEvent", Private);

            foreach (StageEventType type in System.Enum.GetValues(typeof(StageEventType)))
            {
                var entry = new StageEventEntry { Type = type, Count = 8 };
                var spawned = (List<EnemyController>)run.Invoke(spawner, new object[] { entry, level, phase });
                Assert.AreEqual(8, spawned.Count, $"{type} should spawn its full formation. " + Errors());
                Assert.That(spawned.All(e => e.IsFlying), $"{type} members should fly, not chase.");

                var starts = spawned.Select(e => e.transform.position).ToArray();
                for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
                for (int i = 0; i < spawned.Count; i++)
                {
                    var moved = spawned[i].transform.position - starts[i];
                    moved.y = 0f;
                    Assert.That(moved.magnitude, Is.GreaterThan(0.5f), $"{type} member {i} did not move.");
                }

                // Pooled reuse must not turn the enemy into a solid wall.
                var flier = spawned[0];
                flier.gameObject.SetActive(false);
                Assert.IsTrue(flier.GetComponent<Collider>().isTrigger, $"{type} flier became solid after returning to the pool.");
                foreach (var e in spawned) e.gameObject.SetActive(false);
            }
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static int Fliers() => EnemyController.ActiveEnemies.Count(e => e != null && e.IsFlying);
    }
}
