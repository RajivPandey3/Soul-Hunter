using System.Linq;
using NUnit.Framework;
using SoulHunter.Gameplay.Data;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>Part 4: each stage's own pattern (owner-approved table, 2026-09-26): spawn sides, events and pace.</summary>
    public class StagePatternTests
    {
        private static float Share(System.Func<StageEventEntry, bool> match, int stage) =>
            WaveTables.Events(stage).Count(match) / (float)WaveTables.Events(stage).Count;

        [Test]
        public void SpawnSides_FollowTheApprovedTable()
        {
            for (int stage = 1; stage <= 10; stage++)
            {
                var expected = stage == 4 ? SpawnSide.LeftRight : stage == 6 ? SpawnSide.TopBottom : SpawnSide.AllSides;
                Assert.That(WaveTables.SideFor(stage), Is.EqualTo(expected), $"Stage {stage}");
            }
        }

        [Test]
        public void Events_FollowTheApprovedTable()
        {
            StageEventType S = StageEventType.Swarm, W = StageEventType.Wall, R = StageEventType.ClosingRing;
            // L1 plays Mad Forest's own rows since 2026-10-27 (MapEventsTests): swarms and Flower Walls.
            Assert.That(Share(e => e.Type == S || e.Type == StageEventType.TrapRing, 1), Is.EqualTo(1f), "L1: swarms and Flower Walls.");
            Assert.That(Share(e => e.Type == R, 2), Is.GreaterThan(0.5f), "L2: closing rings.");
            Assert.That(Share(e => e.Type == W, 3), Is.GreaterThan(0.5f), "L3: walls.");
            Assert.That(Share(e => e.Type == W, 4), Is.EqualTo(1f), "L4: walls.");
            Assert.That(WaveTables.Events(5).All(e => e.Type == S && e.Speed > 1.5f), Is.True, "L5: fast straight swarms.");
            Assert.That(Share(e => e.Type == S, 6), Is.GreaterThan(0.5f), "L6: swarms.");
            Assert.That(Share(e => e.Type == S, 7), Is.GreaterThan(0.5f), "L7: ghost swarms.");
            Assert.That(Share(e => e.Type == R, 8), Is.EqualTo(1f), "L8: rings.");
            Assert.That(WaveTables.Events(9).All(e => e.Type == W || e.Type == R) && WaveTables.Events(9).Any(e => e.Type == R)
                        && WaveTables.Events(9).Any(e => e.Type == W), Is.True, "L9: walls and rings.");
            var l10 = WaveTables.Events(10);
            Assert.That(l10.Select(e => e.Type).Distinct().Count(), Is.EqualTo(3), "L10: everything.");
            Assert.That(l10.Count, Is.GreaterThan(WaveTables.Events(9).Count), "L10: the busiest schedule.");
            for (int stage = 1; stage <= 10; stage++)
                Assert.That(WaveTables.Events(stage).All(e => e.TimeSeconds < 1800f), Is.True, $"Stage {stage}: within the 30 minutes.");
        }

        [Test]
        public void Events_AreVsDense_WithRepeatsAndChances()
        {
            // VS (Mad Forest wave table): events most minutes from 2:00, rings/walls at 5, 10, 15, 25, and guaranteed
            // repeated swarms at 27 and 29 (after the 25:00 boss). Chance is divided by Luck.
            for (int stage = 1; stage <= 10; stage++)
            {
                if (MapEvents.For(stage) != null) continue; // a level on its VS table (MapEventsTests)
                var events = WaveTables.Events(stage);
                Assert.That(events.Count, Is.GreaterThanOrEqualTo(15), $"Stage {stage}: an event most minutes.");
                Assert.That(events.Select(e => e.TimeSeconds).Min(), Is.EqualTo(120f), $"Stage {stage}: the first at 2:00.");
                Assert.That(events.Any(e => e.TimeSeconds == 1620f && e.Repeat > 1 && e.ChancePercent >= 100f), Is.True, $"Stage {stage}: 27:00.");
                Assert.That(events.Any(e => e.ChancePercent < 100f), Is.True, $"Stage {stage}: some events are a chance.");
            }
        }

        [Test]
        public void StageAssets_CarryTheirSidesAndEvents()
        {
            for (int i = 0; i < WaveTables.StageWaveAssets.Length; i++)
            {
                var wave = AssetDatabase.LoadAssetAtPath<WaveData>(WaveTables.StageWaveAssets[i]);
                Assert.That(wave.SpawnSide, Is.EqualTo(WaveTables.SideFor(i + 1)), $"Stage {i + 1}");
                var expected = WaveTables.Events(i + 1);
                Assert.That(wave.Events.Count, Is.EqualTo(expected.Count), $"Stage {i + 1}");
                for (int e = 0; e < expected.Count; e++)
                {
                    Assert.That(wave.Events[e].Type, Is.EqualTo(expected[e].Type));
                    Assert.That(wave.Events[e].TimeSeconds, Is.EqualTo(expected[e].TimeSeconds));
                    Assert.That(wave.Events[e].Speed, Is.EqualTo(expected[e].Speed).Within(1e-4));
                    Assert.That(wave.Events[e].Repeat, Is.EqualTo(expected[e].Repeat));
                    Assert.That(wave.Events[e].ChancePercent, Is.EqualTo(expected[e].ChancePercent).Within(1e-4));
                }
            }
        }

        [Test]
        public void SpawnDirection_KeepsMostSpawnsToTheStageSides()
        {
            Random.InitState(42);
            float Near(SpawnSide side, Vector3 axis)
            {
                int near = 0;
                for (int i = 0; i < 2000; i++)
                {
                    var d = WaveTables.SpawnDirection(side, 0.8f, Random.value, Random.value, Random.value);
                    Assert.That(d.y, Is.EqualTo(0f));
                    if (Mathf.Abs(Vector3.Dot(d, axis)) > Mathf.Cos(45f * Mathf.Deg2Rad)) near++;
                }
                return near / 2000f;
            }
            Assert.That(Near(SpawnSide.LeftRight, Vector3.right), Is.GreaterThan(0.75f), "Left/right stages.");
            Assert.That(Near(SpawnSide.TopBottom, Vector3.forward), Is.GreaterThan(0.75f), "Top/bottom stages.");
            Assert.That(Near(SpawnSide.AllSides, Vector3.right), Is.InRange(0.4f, 0.6f), "All sides: no bias.");
        }

        /// <summary>
        /// Pace stays the owner-approved stage table (crowd plan Part 4: slow, heavy swamp 0.9; fast peaks 1.45); the owner
        /// chose "VS waves + own rules" (2026-10-09). The VS minute intervals are used as they are (multiplier 1, P2-02).
        /// </summary>
        [Test]
        public void Pace_IsTheApprovedStageTable_AndIntervalsAreVs()
        {
            float[] vs = { 1f, 1.10f, 1.15f, 1.20f, 0.90f, 1.45f, 1.30f, 1.40f, 1.50f, 1.60f };
            for (int level = 1; level <= 10; level++)
            {
                Assert.That(CampaignLevelCatalog.Get(level).EnemySpeedMultiplier, Is.EqualTo(vs[level - 1]).Within(1e-5f), $"Level {level} pace");
                Assert.That(CampaignLevelCatalog.Get(level).SpawnIntervalMultiplier, Is.EqualTo(1f).Within(1e-5f), $"Level {level} interval");
            }
        }
    }
}
