using System.Linq;
using NUnit.Framework;
using UnityEditor;
using SoulHunter.Gameplay.Data;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// VS minute waves: every level plays its VS source stage's 30-minute table (Level 1 Mad Forest; P2-02 for Levels 2-10).
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

        /// <summary>
        /// Bug MIS B01 (2026-09-30): the table covered 0-24 and minutes 25-29 reused minute 24. Stage 1 is VS's Mad Forest
        /// (wiki Mad_Forest, "Waves"), 30 minutes.
        /// </summary>
        [Test]
        public void Stage1_IsVsMadForest()
        {
            var stage1 = WaveTables.Build(1, 1f);
            Assert.That(stage1.Count, Is.EqualTo(30), "Every stage lasts 30 minutes.");
            int[] vsMinimum = { 15, 30, 50, 40, 30, 10, 20, 80, 100, 30, 10, 300, 20, 150, 20, 100, 100, 20, 60, 100, 100, 300, 200, 300, 300, 100, 150, 300, 300, 300 };
            float[] vsInterval = { 1f, 1f, 0.5f, 0.25f, 1f, 1f, 0.5f, 0.5f, 1.5f, 0.5f, 0.5f, 0.1f, 0.25f, 0.5f, 0.1f, 0.1f, 0.1f, 1f, 0.5f, 0.5f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f };
            for (int m = 0; m < 30; m++)
            {
                Assert.That(stage1[m].Minimum, Is.EqualTo(vsMinimum[m]), $"Mad Forest minute {m} minimum");
                Assert.That(stage1[m].Interval, Is.EqualTo(vsInterval[m]).Within(1e-4), $"Mad Forest minute {m} interval");
                Assert.That(stage1[m].EliteBonus, Is.EqualTo(0f), "No elite bonus: not a VS wave field.");
            }
        }

        /// <summary>
        /// P2-02 (owner, 2026-10-09): Levels 2-10 play their VS source stage's minute table (raw wiki "Waves" tables, fetched
        /// 2026-10-09), pinned here row by row, with no elite bonus. Lake Foscari's broken 22:00 cell ("0.8") is 80 and its
        /// 25:00 "1 (150)" is 150 (LakeFoscariEnemies).
        /// </summary>
        [TestCase(2, new[] { 50, 80, 100, 60, 110, 100, 30, 80, 80, 200, 100, 120, 80, 120, 300, 100, 100, 200, 60, 120, 100, 100, 80, 120, 300, 300, 100, 300, 250, 250 },
                     new[] { 3f, 3f, 4f, 4f, 4f, 4f, 2f, 2f, 1f, 0.5f, 0.5f, 2f, 1f, 0.5f, 0.1f, 0.1f, 1f, 1f, 0.5f, 0.5f, 0.1f, 0.1f, 1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f })] // Inlaid Library
        [TestCase(3, new[] { 20, 30, 40, 40, 25, 10, 30, 60, 50, 30, 20, 100, 20, 200, 150, 50, 80, 40, 80, 80, 100, 200, 200, 200, 200, 100, 150, 150, 10, 10 },
                     new[] { 1f, 1f, 0.5f, 0.25f, 0.25f, 1f, 0.5f, 0.5f, 1.5f, 0.5f, 0.5f, 0.5f, 1f, 0.1f, 0.5f, 0.5f, 0.5f, 0.1f, 0.5f, 0.5f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f })] // Dairy Plant
        [TestCase(4, new[] { 50, 80, 100, 60, 110, 100, 60, 80, 80, 200, 100, 60, 80, 120, 300, 100, 100, 100, 60, 200, 100, 100, 80, 100, 60, 300, 100, 100, 100, 50 },
                     new[] { 3f, 3f, 4f, 4f, 4f, 4f, 2f, 2f, 1f, 0.5f, 0.5f, 2f, 1f, 0.5f, 0.1f, 0.1f, 1f, 0.8f, 0.1f, 0.5f, 0.5f, 0.1f, 1f, 0.1f, 0.1f, 0.1f, 0.1f, 1f, 1f, 1f })] // Gallo Tower
        [TestCase(5, new[] { 100, 30, 40, 40, 25, 10, 30, 60, 50, 30, 20, 100, 20, 200, 150, 50, 80, 40, 90, 80, 100, 200, 200, 200, 200, 100, 200, 150, 50, 30 },
                     new[] { 1f, 1f, 0.5f, 0.25f, 0.25f, 1f, 0.5f, 0.5f, 1.5f, 0.5f, 0.5f, 0.5f, 1f, 0.1f, 0.5f, 0.5f, 0.5f, 0.1f, 0.5f, 0.5f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f })] // Abyss Foscari
        [TestCase(6, new[] { 50, 80, 50, 40, 30, 100, 20, 80, 100, 40, 80, 300, 40, 150, 20, 50, 100, 100, 20, 100, 100, 150, 150, 200, 200, 50, 150, 300, 200, 200 },
                     new[] { 3f, 3f, 0.5f, 0.25f, 0.5f, 4f, 0.5f, 0.5f, 1.5f, 0.5f, 0.5f, 0.1f, 0.25f, 0.5f, 0.1f, 0.1f, 1f, 0.8f, 1f, 0.5f, 0.1f, 0.1f, 1f, 0.1f, 0.1f, 0.1f, 0.1f, 1f, 1f, 1f })] // Mt.Moonspell
        [TestCase(7, new[] { 50, 80, 100, 60, 110, 100, 30, 80, 80, 200, 100, 120, 80, 120, 300, 100, 100, 200, 60, 120, 100, 100, 80, 120, 200, 150, 100, 100, 100, 200 },
                     new[] { 3f, 3f, 4f, 4f, 4f, 4f, 2f, 2f, 1f, 0.5f, 0.5f, 2f, 1f, 0.5f, 0.1f, 1f, 0.1f, 1f, 0.5f, 0.5f, 0.1f, 0.1f, 1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f })] // Lake Foscari
        [TestCase(8, new[] { 20, 30, 40, 40, 25, 10, 30, 60, 50, 30, 20, 100, 20, 200, 150, 50, 80, 40, 80, 80, 100, 60, 200, 200, 200, 100, 250, 150, 10, 40 },
                     new[] { 1f, 1f, 0.5f, 0.25f, 0.25f, 1f, 0.5f, 0.5f, 1.5f, 0.5f, 0.5f, 0.5f, 1f, 0.1f, 0.5f, 0.5f, 0.5f, 0.1f, 0.5f, 0.5f, 0.1f, 0.02f, 0.1f, 0.1f, 0.1f, 0.1f, 0.02f, 0.1f, 0.1f, 0.1f })] // Cappella Magna
        public void LevelsTwoToTen_PlayTheirVsSourceStagesTable(int stage, int[] minimum, float[] interval)
        {
            int[] stages = stage == 8 ? new[] { 8, 9, 10 } : new[] { stage }; // Cappella Magna serves Levels 8-10
            foreach (int level in stages)
            {
                var table = WaveTables.Build(level, 0.7f);
                Assert.That(table.Count, Is.EqualTo(30), $"Level {level}");
                for (int m = 0; m < 30; m++)
                {
                    Assert.That(table[m].Minute, Is.EqualTo(m));
                    Assert.That(table[m].Minimum, Is.EqualTo(minimum[m]), $"Level {level} minute {m} minimum");
                    Assert.That(table[m].Interval, Is.EqualTo(interval[m]).Within(1e-4), $"Level {level} minute {m} interval (absolute, not x the asset's)");
                    Assert.That(table[m].EliteBonus, Is.EqualTo(0f), $"Level {level} minute {m}: no elite bonus");
                }
            }
        }

        [Test]
        public void PastTheTable_TheLastMinuteHolds_ForLevelTensEndlessFight()
        {
            // Level 10 has no time limit (owner's ending): VS stops at 29:00 (30:00+ is the Reaper, which Soul Hunter has
            // not), so later minutes hold the 29:00 row (balance method).
            Assert.That(WaveTables.VsMinimum(10, 45), Is.EqualTo(WaveTables.VsMinimum(10, 29)));
            Assert.That(WaveTables.VsInterval(10, 45), Is.EqualTo(WaveTables.VsInterval(10, 29)));
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
