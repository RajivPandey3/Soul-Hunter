using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    /// Part 4 in the real game: the Ruined Castle's enemies come mostly from the left and right, the Frozen Peaks'
    /// from above and below, nothing spawns inside the village houses, and the Crimson Swamp's swarms fly fast.
    /// </summary>
    public class StagePatternPlayModeTests : GameplayPlayModeTestBase
    {
        private IEnumerator EnterStage(int stage)
        {
            yield return BootIntoGameplay();
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            LevelProgressionManager.Instance.StartStage(stage);
            yield return null;
            yield return null;
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null) stray.gameObject.SetActive(false);
        }

        /// <summary>Where the next <paramref name="count"/> spawns appear, relative to the (still) player.</summary>
        private static IEnumerator CollectSpawns(int count, List<Vector3> offsets)
        {
            var seen = new HashSet<EnemyController>(EnemyController.ActiveEnemies);
            Vector3 player = PlayerController.Instance.transform.position;
            float deadline = Time.realtimeSinceStartup + 20f;
            while (offsets.Count < count && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                foreach (var enemy in EnemyController.ActiveEnemies.ToList())
                {
                    if (enemy == null || !seen.Add(enemy) || enemy.IsFlying) continue;
                    Vector3 offset = enemy.transform.position - player;
                    offset.y = 0f;
                    offsets.Add(offset);
                }
            }
        }

        private static float ShareNear(List<Vector3> offsets, Vector3 axis) =>
            offsets.Count(o => Mathf.Abs(Vector3.Dot(o.normalized, axis)) > Mathf.Cos(45f * Mathf.Deg2Rad)) / (float)offsets.Count;

        [UnityTest]
        public IEnumerator RuinedCastle_EnemiesComeMostlyFromLeftAndRight()
        {
            yield return EnterStage(4);
            var offsets = new List<Vector3>();
            yield return CollectSpawns(40, offsets);
            Assert.That(offsets.Count, Is.GreaterThanOrEqualTo(30), "The castle horde spawns. " + Errors());
            Assert.That(ShareNear(offsets, Vector3.right), Is.GreaterThan(0.65f), "Mostly from the left and right.");

            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator FrozenPeaks_EnemiesComeMostlyFromAboveAndBelow_AndAreFast()
        {
            yield return EnterStage(6);
            var offsets = new List<Vector3>();
            yield return CollectSpawns(40, offsets);
            Assert.That(offsets.Count, Is.GreaterThanOrEqualTo(30), "The peaks horde spawns. " + Errors());
            Assert.That(ShareNear(offsets, Vector3.forward), Is.GreaterThan(0.65f), "Mostly from above and below.");
            var enemy = EnemyController.ActiveEnemies.First(e => e != null && !e.IsFlying);
            Assert.That(enemy.CampaignSpeedMultiplier, Is.GreaterThanOrEqualTo(1.4f), "Fast enemies.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator ForgottenVillage_NothingSpawnsInsideTheHouses()
        {
            yield return EnterStage(3);
            var offsets = new List<Vector3>();
            yield return CollectSpawns(60, offsets);
            Vector3 player = PlayerController.Instance.transform.position;
            Assert.That(offsets.Count, Is.GreaterThanOrEqualTo(40));
            var blocked = offsets.Where(o => !GroundCheck.IsOpen(player + o, 0.3f)).ToList();
            Assert.That(blocked, Is.Empty, $"{blocked.Count} enemies spawned inside a stage obstacle.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator CrimsonSwamp_IsSlow_AndItsSwarmsFlyFast()
        {
            yield return EnterStage(5);
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            var wave = GetPrivateField<WaveData>(spawner, "_currentWave");
            var swarm = wave.Events.First();
            Assert.That(swarm.Type, Is.EqualTo(StageEventType.Swarm));
            var fliers = spawner.TriggerStageEvent(swarm.Type, swarm.Count);
            // TriggerStageEvent uses default speed; run the authored entry the way the stage clock does.
            foreach (var f in fliers) if (f != null) f.gameObject.SetActive(false);
            var run = typeof(EnemySpawner).GetMethod("RunStageEvent", BindingFlags.NonPublic | BindingFlags.Instance);
            var level = LevelProgressionManager.Instance.CurrentLevel;
            var members = (List<EnemyController>)run.Invoke(spawner, new object[] { swarm, level, wave.GetPhase(0f) });
            Assert.That(members.Count, Is.GreaterThan(0));
            for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
            var member = members.First(m => m != null && m.isActiveAndEnabled);
            float flight = member.Rigidbody.linearVelocity.magnitude;
            Assert.That(member.CampaignSpeedMultiplier, Is.LessThan(1f), "The swamp horde is slow (owner-approved pace).");
            // Since 2026-10-28 Level 5 plays Abyss Foscari's own rows: the first is a Swarm Rotting Ghoul swarm (Fixed
            // Direction, VS speed 600), which flies at its own speed, far faster than the stage's walkers (VS 100-140).
            float expected = 600f * EnemyAbilityDefaults.VsSpeedToUnits * member.CampaignSpeedMultiplier;
            Assert.That(flight, Is.EqualTo(expected).Within(expected * 0.2f), $"Swamp swarms fly at their own speed ({flight:0.0}).");
            Assert.That(flight, Is.GreaterThan(140f * EnemyAbilityDefaults.VsSpeedToUnits * 2f), "and fast.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
