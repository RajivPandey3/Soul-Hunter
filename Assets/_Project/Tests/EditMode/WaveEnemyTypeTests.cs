using System.Collections.Generic;
using NUnit.Framework;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Data;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// VS wiki (Enemies, Waves): "Each wave specifies a minimum amount and a spawn interval ... If more enemies than the
    /// minimum amount is present, one of each type of enemies in the wave will be spawned." Soul Hunter's minute waves
    /// now carry their enemy types (MinuteWave.EnemyTypes, authored in WaveTables.EnemyTypesByStage).
    /// </summary>
    public class WaveEnemyTypeTests
    {
        [TearDown]
        public void TearDown() => WaveTables.EnemyTypesByStage.Remove(10);

        [Test]
        public void AMinutesEnemyTypes_ReachItsPhase()
        {
            var wave = ScriptableObject.CreateInstance<WaveData>();
            var a = new GameObject("TypeA");
            var b = new GameObject("TypeB");
            try
            {
                wave.Minutes = new List<MinuteWave>
                {
                    new MinuteWave { Minute = 0, Minimum = 5, Interval = 1f },
                    new MinuteWave { Minute = 1, Minimum = 5, Interval = 1f, EnemyTypes = new List<GameObject> { a, b, null } },
                };
                Assert.That(wave.GetPhase(10f).EnemyTypes, Is.Empty, "No types: the stage's own enemy, as before.");
                Assert.That(wave.GetPhase(70f).EnemyTypes, Is.EqualTo(new[] { a, b }), "Minute 1's types, without the empty slot.");
            }
            finally
            {
                Object.DestroyImmediate(wave);
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
            }
        }

        [Test]
        public void EnemyTypePaths_FollowTheirFirstMinute()
        {
            WaveTables.EnemyTypesByStage[10] = new List<(int, string[])> { (0, new[] { "A" }), (10, new[] { "A", "B" }) };
            Assert.That(WaveTables.EnemyTypePaths(10, 3), Is.EqualTo(new[] { "A" }));
            Assert.That(WaveTables.EnemyTypePaths(10, 12), Is.EqualTo(new[] { "A", "B" }));
            Assert.That(WaveTables.EnemyTypePaths(2, 12), Is.Empty, "A stage with no entries keeps its own enemy.");
        }

        [Test]
        public void EveryAuthoredEnemyType_IsAnEnemyWithData()
        {
            for (int i = 0; i < WaveTables.StageWaveAssets.Length; i++)
            {
                var wave = AssetDatabase.LoadAssetAtPath<WaveData>(WaveTables.StageWaveAssets[i]);
                foreach (var minute in wave.Minutes)
                    foreach (var type in minute.EnemyTypes)
                    {
                        Assert.That(type, Is.Not.Null, $"Stage {i + 1} minute {minute.Minute}: empty enemy type.");
                        var controller = type.GetComponent<EnemyController>();
                        Assert.That(controller != null && controller.Data != null, Is.True, $"{type.name} is an enemy with EnemyData.");
                    }
            }
        }
    }
}
