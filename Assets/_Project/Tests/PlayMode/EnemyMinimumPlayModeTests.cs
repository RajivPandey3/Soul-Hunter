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
    }
}
