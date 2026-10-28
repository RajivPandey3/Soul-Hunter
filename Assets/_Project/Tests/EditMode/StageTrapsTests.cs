using System.Linq;
using NUnit.Framework;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Each VS stage's own trap events (raw stage pages, "Traps", 2026-10-28): "chosen at random between 9 / 7 / 11 / 9
    /// predetermined options" on Dairy Plant, Gallo Tower, Abyss Foscari and Lake Foscari; the other source stages have none.
    /// </summary>
    public class StageTrapsTests
    {
        [Test]
        public void EachTrapStage_HasItsWikiOptions_AndTheOthersHaveNone()
        {
            Assert.That(StageTraps.For(3), Has.Count.EqualTo(9), "Dairy Plant: 9 options.");
            Assert.That(StageTraps.For(4), Has.Count.EqualTo(7), "Gallo Tower: 7 options.");
            Assert.That(StageTraps.For(5), Has.Count.EqualTo(11), "Abyss Foscari: 11 options.");
            Assert.That(StageTraps.For(7), Has.Count.EqualTo(9), "Lake Foscari: 9 options.");
            foreach (int level in new[] { 1, 2, 6, 8, 9, 10 })
                Assert.That(StageTraps.For(level), Is.Null, $"Level {level}'s source stage has no traps.");
        }

        [Test]
        public void EveryTrapUnit_IsOneOfItsLevelsTypes()
        {
            foreach (int level in new[] { 3, 4, 5, 7 })
            {
                var table = StageEnemyTables.For(level);
                foreach (var row in StageTraps.For(level))
                    Assert.That(table.Find(row.EnemyTypeId), Is.Not.Null, $"Level {level}: {row.EnemyTypeId}");
            }
        }

        [Test]
        public void TheTrapRows_HaveTheirWikiAmountsDurationsAndRanges()
        {
            var dairy = StageTraps.For(3);
            Assert.That(dairy.Where(r => r.Type == StageEventType.PileRing).Select(r => (r.EnemyTypeId, r.Count, r.DurationSeconds, r.Range)),
                Is.EqualTo(new[] { ("WellSerpents", 40, 5f, 0.7f), ("BellImps", 30, 5f, 0.7f), ("ShrineIdol", 20, 5f, 0.7f) }));
            Assert.That(dairy.Where(r => r.Type == StageEventType.PulsingRing).Select(r => r.Count), Is.EqualTo(new[] { 50, 40 }));
            Assert.That(dairy.Single(r => r.EnemyTypeId == "SmokeClump").Count, Is.EqualTo(200), "Musc Musc Swarm: 200 for 20 s.");

            var gallo = StageTraps.For(4);
            Assert.That(gallo.Count(r => r.EnemyTypeId == "StillCandle"), Is.EqualTo(4), "Poltergeist (dest): 4/7.");
            Assert.That(gallo.Count(r => r.EnemyTypeId == "RestlessCandle"), Is.EqualTo(2), "Poltergeist: 2/7.");
            Assert.That(gallo.All(r => r.DurationSeconds == 30f), Is.True);

            var abyss = StageTraps.For(5);
            Assert.That(abyss.Where(r => r.Type == StageEventType.Swarm).All(r => r.Straight), Is.True,
                "Abyss Foscari's trap swarms 'sweep in a straight line across the map'.");
            Assert.That(abyss.Where(r => r.Type == StageEventType.Swarm).Select(r => r.Count), Is.EqualTo(new[] { 40, 20, 200, 50, 50, 50 }));

            var lake = StageTraps.For(7);
            Assert.That(lake.Count(r => r.Type == StageEventType.TrapRing && r.Count == 100 && r.DurationSeconds == 60f), Is.EqualTo(3));
            Assert.That(lake.Count(r => r.EnemyTypeId == "TideMantis" && r.Count == 8 && r.Type == StageEventType.Circle), Is.EqualTo(3));
            Assert.That(lake.Count(r => r.EnemyTypeId == "DrownedUrn" && r.Count == 50 && r.Range == 0.5f), Is.EqualTo(3));
        }

        [Test]
        public void TheNewTrapUnits_HaveTheirStatboxes()
        {
            var big = DairyPlantEnemies.Table.Find("SmokeClump");
            Assert.That((big.Health, big.Damage, big.VsSpeed, big.Knockback, big.MaxKnockback, big.VsXp), Is.EqualTo((13f, 4, 160f, 1f, 3f, 1f)));
            var still = GalloTowerEnemies.Table.Find("StillCandle");
            Assert.That((still.Health, still.HealthTimesLevel, still.VsSpeed, still.Damage, still.SelfDestructs), Is.EqualTo((10f, true, 0f, 3, true)));
            var moving = GalloTowerEnemies.Table.Find("RestlessCandle");
            Assert.That((moving.Health, moving.HealthTimesLevel, moving.VsSpeed, moving.Knockback, moving.SelfDestructs), Is.EqualTo((5f, true, 50f, 0f, true)));
            var lake = LakeFoscariEnemies.Table;
            Assert.That((lake.Find("TideThorn").Health, lake.Find("TideThorn").VsSpeed, lake.Find("TideThorn").Damage), Is.EqualTo((20f, 140f, 5)));
            Assert.That((lake.Find("TideMantis").Health, lake.Find("TideMantis").HealthTimesLevel, lake.Find("TideMantis").Damage), Is.EqualTo((40f, true, 20)));
            Assert.That((lake.Find("DrownedUrn").VsSpeed, lake.Find("DrownedUrn").Damage, lake.Find("DrownedUrn").VsXp), Is.EqualTo((0f, 0, 5f)));
        }

        [Test]
        public void EveryOption_IsEquallyLikely()
        {
            var options = StageTraps.For(4);
            var counts = new int[options.Count];
            for (int i = 0; i < 7000; i++) counts[options.IndexOf(StageTraps.Pick(options, i / 7000f))]++;
            Assert.That(counts.All(c => c == 1000), Is.True, string.Join(",", counts));
            Assert.That(StageTraps.Pick(options, 0.99999f), Is.SameAs(options[6]));
        }
    }
}
