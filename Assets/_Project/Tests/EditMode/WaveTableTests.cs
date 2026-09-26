using System.Linq;
using NUnit.Framework;
using UnityEditor;
using SoulHunter.Gameplay.Data;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// VS minute waves (Part 3): every stage has its own 25-minute table whose minimum rises and falls, with quiet
    /// minutes of tougher enemies, and the stages differ from each other.
    /// </summary>
    public class WaveTableTests
    {
        [Test]
        public void EveryStageWave_CarriesItsBuiltMinuteTable()
        {
            for (int i = 0; i < WaveTables.StageWaveAssets.Length; i++)
            {
                var wave = AssetDatabase.LoadAssetAtPath<WaveData>(WaveTables.StageWaveAssets[i]);
                Assert.That(wave, Is.Not.Null, WaveTables.StageWaveAssets[i]);
                Assert.That(wave.HasMinuteTable, Is.True, $"Stage {i + 1} uses a minute table (Soul Hunter/Content/Build Wave Tables).");
                var expected = WaveTables.Build(i + 1, wave.SpawnInterval);
                Assert.That(wave.Minutes.Count, Is.EqualTo(WaveTables.Minutes));
                for (int m = 0; m < expected.Count; m++)
                {
                    Assert.That(wave.Minutes[m].Minute, Is.EqualTo(m));
                    Assert.That(wave.Minutes[m].Minimum, Is.EqualTo(expected[m].Minimum), $"Stage {i + 1} minute {m}");
                    Assert.That(wave.Minutes[m].Interval, Is.EqualTo(expected[m].Interval).Within(1e-4));
                    Assert.That(wave.Minutes[m].EliteBonus, Is.EqualTo(expected[m].EliteBonus).Within(1e-4));
                }
            }
        }

        [Test]
        public void EveryTable_Rises_ButAlsoFalls_WithToughQuietMinutes()
        {
            for (int stage = 1; stage <= 10; stage++)
            {
                var table = WaveTables.Build(stage, 1f);
                double early = table.Take(5).Average(m => m.Minimum), late = table.Skip(20).Average(m => m.Minimum);
                Assert.That(late, Is.GreaterThan(early * 2), $"Stage {stage} grows over the stage.");
                var drops = Enumerable.Range(1, table.Count - 1).Where(m => table[m].Minimum < table[m - 1].Minimum * 0.6f).ToList();
                Assert.That(drops.Count, Is.GreaterThanOrEqualTo(3), $"Stage {stage} has quiet minutes, like VS's Mad Forest at 5:00.");
                foreach (int m in drops) Assert.That(table[m].EliteBonus, Is.GreaterThan(0f), "A quiet minute sends tougher enemies.");
                Assert.That(table.Last().Interval, Is.LessThan(table.First().Interval), "Spawns come faster later on.");
            }
        }

        [Test]
        public void Stages_HaveDifferentShapes()
        {
            var dips1 = Enumerable.Range(0, WaveTables.Minutes).Where(m => WaveTables.IsDip(1, m)).ToList();
            var dips2 = Enumerable.Range(0, WaveTables.Minutes).Where(m => WaveTables.IsDip(2, m)).ToList();
            Assert.That(dips1, Is.Not.EqualTo(dips2), "Quiet minutes fall at different times on different stages.");
            for (int stage = 2; stage <= 10; stage++)
            {
                Assert.That(WaveTables.OpeningMinimum(stage), Is.GreaterThan(WaveTables.OpeningMinimum(stage - 1)));
                Assert.That(WaveTables.PeakMinimum(stage), Is.GreaterThanOrEqualTo(WaveTables.PeakMinimum(stage - 1)));
            }
        }

        [Test]
        public void GetPhase_ReadsTheCurrentMinute()
        {
            var wave = ScriptableObject.CreateInstance<WaveData>();
            try
            {
                wave.SpawnInterval = 0.8f;
                wave.Minutes = WaveTables.Build(3, wave.SpawnInterval);
                for (int m = 0; m < WaveTables.Minutes; m++)
                {
                    var phase = wave.GetPhase(m * 60f + 30f);
                    Assert.That(phase.MinimumEnemies, Is.EqualTo(wave.Minutes[m].Minimum), $"minute {m}");
                    Assert.That(phase.SpawnIntervalMultiplier * wave.SpawnInterval, Is.EqualTo(wave.Minutes[m].Interval).Within(1e-4));
                    Assert.That(phase.EliteChanceBonus, Is.EqualTo(wave.Minutes[m].EliteBonus).Within(1e-4));
                }
                Assert.That(wave.GetPhase(40 * 60f).MinimumEnemies, Is.EqualTo(wave.Minutes.Last().Minimum), "After the table: its last minute.");
            }
            finally
            {
                Object.DestroyImmediate(wave);
            }
        }
    }
}
