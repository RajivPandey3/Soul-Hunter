using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS wiki (Enemies, Waves): below a wave's minimum the quota is filled; above it, each spawn attempt sends one of each
    /// of the wave's enemy types. A wave listing two types must bring both, in the real game.
    /// </summary>
    public class WaveEnemyTypePlayModeTests : GameplayPlayModeTestBase
    {
        private static int CountOf(EnemyData data) =>
            EnemyController.ActiveEnemies.Count(e => e != null && e.isActiveAndEnabled && e.Data == data);

        [UnityTest]
        public IEnumerator AWaveWithTwoTypes_FillsItsMinimumWithBoth_ThenSendsOneOfEach()
        {
            yield return BootIntoGameplay();
            PlayerController.Instance.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            var enemies = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs");
            GameObject typeA = enemies[0], typeB = enemies[1];
            var dataA = typeA.GetComponent<EnemyController>().Data;
            var dataB = typeB.GetComponent<EnemyController>().Data;

            // A copy of the stage's wave, so the asset on disk is never changed.
            var wave = Object.Instantiate(GetPrivateField<WaveData>(spawner, "_currentWave"));
            wave.Phases = new List<WavePhase>();
            wave.Events = new List<StageEventEntry>();
            wave.Minutes = new List<MinuteWave>
            {
                new MinuteWave { Minute = 0, Minimum = 12, Interval = 0.2f, EnemyTypes = new List<GameObject> { typeA, typeB } },
            };
            typeof(EnemySpawner).GetField("_currentWave", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(spawner, wave);
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null) stray.gameObject.SetActive(false);

            yield return WaitUntilRealtime(() => CountOf(dataA) + CountOf(dataB) >= 12, "the wave's minimum to fill");
            Assert.That(CountOf(dataA), Is.GreaterThanOrEqualTo(5), "Type A fills the quota in turn.");
            Assert.That(CountOf(dataB), Is.GreaterThanOrEqualTo(5), "Type B fills the quota in turn.");

            // Above the minimum every spawn tick adds one of each type.
            int a = CountOf(dataA), b = CountOf(dataB);
            yield return new WaitForSeconds(1.5f);
            Assert.That(CountOf(dataA), Is.GreaterThan(a), "One of type A per tick above the minimum.");
            Assert.That(CountOf(dataB), Is.GreaterThan(b), "One of type B per tick above the minimum.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
            Object.Destroy(wave);
        }
    }
}
