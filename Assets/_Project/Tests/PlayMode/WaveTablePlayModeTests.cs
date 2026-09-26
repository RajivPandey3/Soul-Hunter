using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS minute waves in the real game (Part 3): the spawner follows the stage's minute table, so the horde swells to
    /// a busy minute's minimum and only refills to a quiet minute's smaller one.
    /// </summary>
    public class WaveTablePlayModeTests : GameplayPlayModeTestBase
    {
        private static void SetStageTime(float seconds)
        {
            typeof(LevelProgressionManager).GetField("<StageElapsedTime>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(LevelProgressionManager.Instance, seconds);
        }

        private static void KillAll()
        {
            foreach (var enemy in EnemyController.ActiveEnemies.ToArray())
            {
                var health = enemy != null ? enemy.GetComponent<HealthController>() : null;
                if (health != null) health.KillSilently();
            }
        }

        [UnityTest]
        public IEnumerator Spawner_FollowsTheMinuteTable_BusyAndQuietMinutes()
        {
            yield return BootIntoGameplay();
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            var wave = GetPrivateField<WaveData>(spawner, "_currentWave");
            Assert.That(wave.HasMinuteTable, Is.True, "Stage 1 plays its minute table.");

            // A busy minute: the horde swells to that minute's minimum.
            int busy = Enumerable.Range(10, 5).First(m => !WaveTables.IsDip(1, m));
            SetStageTime(busy * 60f + 5f);
            int busyMinimum = wave.Minutes[busy].Minimum;
            Assert.That(wave.GetPhase(busy * 60f + 5f).MinimumEnemies, Is.EqualTo(busyMinimum));
            yield return WaitUntilRealtime(() => EnemyController.ActiveEnemies.Count >= busyMinimum,
                $"the horde to reach minute {busy}'s minimum of {busyMinimum}");

            // A quiet minute: after a wipe it refills only to the smaller minimum, and elites are likelier.
            int quiet = Enumerable.Range(busy + 1, 6).First(m => WaveTables.IsDip(1, m));
            SetStageTime(quiet * 60f + 5f);
            var phase = wave.GetPhase(quiet * 60f + 5f);
            Assert.That(phase.MinimumEnemies, Is.LessThan(busyMinimum / 2), $"Minute {quiet} is quiet.");
            Assert.That(phase.EliteChanceBonus, Is.GreaterThan(0f), "It sends tougher enemies.");
            KillAll();
            yield return WaitUntilRealtime(() => EnemyController.ActiveEnemies.Count >= phase.MinimumEnemies,
                $"the horde to refill to minute {quiet}'s minimum of {phase.MinimumEnemies}");
            yield return null;
            Assert.That(EnemyController.ActiveEnemies.Count, Is.LessThan(busyMinimum),
                "A quiet minute does not refill to the busy minute's horde.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
