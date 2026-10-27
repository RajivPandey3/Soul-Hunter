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

            // Jump the stage clock to just after the stage's first event (2:30). Part 4 (2026-09-26): each stage
            // authors its own events, so read the expected size from the stage's wave instead of the old default 20.
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            var first = GetPrivateField<WaveData>(spawner, "_currentWave").GetEvents()[0];
            // VS Mad Forest (MapEvents, 2026-10-27): the first event is the 2:00 Bat Swarm, 5 s in, guaranteed.
            Assert.That(first.TimeSeconds, Is.EqualTo(125f));
            Assert.That(first.ChancePercent, Is.EqualTo(100f));
            Assert.That(first.Type, Is.EqualTo(StageEventType.Swarm), "The Cursed Graveyard opens with a swarm.");
            typeof(LevelProgressionManager).GetProperty("StageElapsedTime").GetSetMethod(true).Invoke(progression, new object[] { first.TimeSeconds + 1f });
            yield return WaitUntilRealtime(() => Fliers() >= fliersBefore + first.Count, "the 2:00 swarm to spawn");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator EveryFormation_FliesItsPath_AndPooledFliersStayTriggers()
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false; // only the events under test spawn
            DisablePlayerWeapons(); // a formation crossing the player was sometimes killed mid-check (flaky "did not move")
            var level = LevelProgressionManager.Instance.CurrentLevel;
            var phase = GetPrivateField<WaveData>(spawner, "_currentWave").GetPhase(0f);
            var run = typeof(EnemySpawner).GetMethod("RunStageEvent", Private);

            foreach (StageEventType type in System.Enum.GetValues(typeof(StageEventType)))
            {
                if (type == StageEventType.TrapRing) continue; // a standing ring, not a flight (MapEventsPlayModeTests)
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

        [UnityTest]
        public IEnumerator FrozenFliers_ResumeTheirPath_AfterTimeFreeze()
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            DisablePlayerWeapons();
            var level = LevelProgressionManager.Instance.CurrentLevel;
            var phase = GetPrivateField<WaveData>(spawner, "_currentWave").GetPhase(0f);
            var entry = new StageEventEntry { Type = StageEventType.Wall, Count = 6 };
            var spawned = (List<EnemyController>)typeof(EnemySpawner).GetMethod("RunStageEvent", Private)
                .Invoke(spawner, new object[] { entry, level, phase });
            var directions = spawned.Select(FlyDirection).ToArray();

            foreach (var e in spawned) e.Freeze(0.5f);
            var frozenAt = spawned.Select(e => e.transform.position).ToArray();
            for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
            for (int i = 0; i < spawned.Count; i++)
            {
                var drift = spawned[i].transform.position - frozenAt[i];
                drift.y = 0f;
                Assert.That(drift.magnitude, Is.LessThan(0.05f), $"Member {i} moved while frozen.");
                Assert.That(spawned[i].IsFlying, $"Member {i} is still a stage-event flier while frozen (not relocated).");
            }

            yield return WaitUntilRealtime(() => spawned.All(e => GetPrivateField<IEnemyState>(e, "_currentState") is EnemyFlyState),
                "the freeze to end");
            var resumedAt = spawned.Select(e => e.transform.position).ToArray();
            for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
            for (int i = 0; i < spawned.Count; i++)
            {
                Assert.That(Vector3.Angle(FlyDirection(spawned[i]), directions[i]), Is.LessThan(0.1f), $"Member {i} changed course.");
                var moved = spawned[i].transform.position - resumedAt[i];
                moved.y = 0f;
                Assert.That(Vector3.Angle(moved, directions[i]), Is.LessThan(5f), $"Member {i} chases instead of flying its path.");
                Assert.That(moved.magnitude, Is.GreaterThan(0.5f), $"Member {i} did not resume.");
            }
            foreach (var e in spawned) e.gameObject.SetActive(false);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static Vector3 FlyDirection(EnemyController enemy)
        {
            var state = GetPrivateField<IEnemyState>(enemy, "_currentState");
            if (state is EnemyFrozenState frozen) state = frozen.ResumeState;
            return ((EnemyFlyState)state).Direction;
        }

        private static void DisablePlayerWeapons()
        {
            foreach (var weapon in Object.FindObjectsByType<SoulHunter.Gameplay.Combat.AutoAttackWeapon>(FindObjectsSortMode.None))
                weapon.gameObject.SetActive(false);
        }

        private static int Fliers() => EnemyController.ActiveEnemies.Count(e => e != null && e.IsFlying);
    }
}
