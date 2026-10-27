using System.Linq;
using NUnit.Framework;
using SoulHunter.Gameplay.Data;
using UnityEditor;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// VS map events (raw stage "Waves" tables and Template:Map_event, 2026-10-27): Level 1 plays Mad Forest's rows.
    /// </summary>
    public class MapEventsTests
    {
        [Test]
        public void LevelOne_PlaysMadForestsMapEventRows()
        {
            var events = MapEvents.For(1);
            Assert.That(events, Has.Count.EqualTo(21), "21 {{map event}} rows on Mad Forest.");
            var bats = events.First(e => e.EnemyTypeId == "SwarmBat");
            Assert.That(bats.TimeSeconds, Is.EqualTo(125f), "2:00 Bat Swarm, delay 5.");
            Assert.That(bats.Repeat, Is.EqualTo(3), "repeat=2: the template shows x(repeat+1).");
            Assert.That(bats.Count, Is.EqualTo(50), "Bat Swarm: 'summons 50 Pipeestrellos'.");
            Assert.That(bats.ChancePercent, Is.EqualTo(100f), "No chance listed: guaranteed.");
            var ghosts = events.Where(e => e.EnemyTypeId == "SwarmWisp").ToArray();
            Assert.That(ghosts.All(g => g.Count == 20), Is.True, "Ghost Swarm: 20.");
            var walls = events.Where(e => e.Type == StageEventType.TrapRing).ToArray();
            Assert.That(walls.Select(w => w.TimeSeconds), Is.EqualTo(new[] { 300f, 600f, 900f, 1510f }));
            Assert.That(walls.Select(w => w.DurationSeconds), Is.EqualTo(new[] { 30f, 60f, 60f, 10f }));
            Assert.That(walls[2].ChancePercent, Is.EqualTo(80f));
            Assert.That(walls[3].Repeat, Is.EqualTo(6), "25:10 Flower Wall, repeat=5.");
            Assert.That(WaveTables.Events(1), Has.Count.EqualTo(21), "Level 1's schedule is the VS table.");
        }

        [Test]
        public void LevelTwo_PlaysInlaidLibrarysMapEventRows()
        {
            var events = MapEvents.For(2);
            Assert.That(events, Has.Count.EqualTo(36), "36 {{map event}} rows on Inlaid Library.");
            Assert.That(WaveTables.Events(2), Has.Count.EqualTo(36));
            var first = events[0];
            Assert.That((first.Type, first.EnemyTypeId, first.Count, first.TimeSeconds), Is.EqualTo((StageEventType.Chasers, "CinderShade", 1, 0.1f)));
            var medusa = events.Where(e => e.EnemyTypeId.Contains("Gloom")).ToArray();
            Assert.That(medusa.All(e => e.Horizontal), Is.True, "Medusa events travel horizontally.");
            var skulls = events.Where(e => e.EnemyTypeId == "SwarmSkull" && e.TimeSeconds < 1260f).Select(e => e.Count);
            Assert.That(skulls, Is.EqualTo(new[] { 1, 2, 4, 8, 16, 32, 32 }), "Each wave double in size from 1 to 32, then one more 32.");
            var last = events.Last();
            Assert.That((last.Repeat, last.Count, last.ChancePercent), Is.EqualTo((51, 4, 60f)), "29:00 Shade Bomb repeat=50, amount 4, 60%.");
        }

        [Test]
        public void LevelTwo_EventUnits_HaveTheirWikiStatboxes()
        {
            var t = InlaidLibraryEnemies.Table;
            var shade = t.Find("CinderShade");
            Assert.That((shade.Health, shade.HealthTimesLevel, shade.Damage, shade.VsSpeed, shade.SelfDestructs), Is.EqualTo((11f, true, 3, 330f, true)));
            Assert.That(t.MovementOf(shade), Is.EqualTo(EnemyMovement.Homing), "Sig.ra Rossi has no Floaty skill.");
            Assert.That(t.MovementOf(t.Find("GloomMote")), Is.EqualTo(EnemyMovement.FloatyFixedDirection));
            Assert.That((t.Find("SwiftGloomMote").VsSpeed, t.Find("SwiftGloomMoth").Health), Is.EqualTo((600f, 25f)));
            var skull = t.Find("SwarmSkull");
            Assert.That((skull.Health, skull.Damage, skull.VsSpeed, skull.Knockback, t.MovementOf(skull)), Is.EqualTo((25f, 3, 700f, 0f, EnemyMovement.FixedDirection)));
            Assert.That(t.Find("SwarmNightBat").DefangResistance, Is.True);
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(t.DataPath("CinderShade"));
            Assert.That(data, Is.Not.Null);
            Assert.That(data.Abilities.Any(a => a.Kind == EnemyAbilityKind.SelfDestruct), Is.True);
            var wave = AssetDatabase.LoadAssetAtPath<WaveData>(WaveTables.StageWaveAssets[1]);
            foreach (var id in new[] { "CinderShade", "GloomMote", "SwiftGloomMote", "SwiftGloomMoth", "SwarmSkull", "SwarmNightBat" })
                Assert.That(wave.BossTypes.Select(p => p.name), Does.Contain(t.PrefabName(id)), id);
        }

        [Test]
        public void TheSwarmUnits_HaveTheirWikiStatboxes()
        {
            var bat = MadForestEnemies.Find("SwarmBat");
            Assert.That((bat.Health, bat.Damage, bat.VsSpeed, bat.FixedDirection, bat.DefangResistance), Is.EqualTo((1, 1, 700f, true, true)));
            var wisp = MadForestEnemies.Find("SwarmWisp");
            Assert.That((wisp.Health, wisp.Damage, wisp.VsSpeed, wisp.Knockback, wisp.FixedDirection), Is.EqualTo((10, 5, 700f, 0f, true)));
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(MadForestEnemies.DataPath("SwarmBat"));
            Assert.That(data, Is.Not.Null);
            Assert.That(data.Movement, Is.EqualTo(EnemyMovement.FixedDirection));
            Assert.That(data.DefangResistance, Is.True);
            var wave = AssetDatabase.LoadAssetAtPath<WaveData>(WaveTables.StageWaveAssets[0]);
            Assert.That(wave.Events, Has.Count.EqualTo(21), "The Level 1 wave asset holds the VS rows.");
            Assert.That(wave.BossTypes.Select(p => p.name), Does.Contain(MadForestEnemies.PrefabName("SwarmBat")));
        }
    }
}
