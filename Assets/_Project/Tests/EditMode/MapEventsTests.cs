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
        public void LevelThree_PlaysDairyPlantsMapEventRows_AndTheStalker()
        {
            var events = MapEvents.For(3);
            Assert.That(events, Has.Count.EqualTo(25), "28 rows on Dairy Plant, 3 of them The Stalker (Death's Shadow).");
            Assert.That(WaveTables.Events(3), Has.Count.EqualTo(25));
            Assert.That(events.Count(e => e.Type == StageEventType.PileRing), Is.EqualTo(7));
            Assert.That(events.Count(e => e.Type == StageEventType.PulsingRing), Is.EqualTo(5));
            var jelly = events.First(e => e.Type == StageEventType.PulsingRing);
            Assert.That((jelly.TimeSeconds, jelly.Count, jelly.DurationSeconds), Is.EqualTo((180f, 80, 30f)), "3:00 Jellyfish Wall, amount 80.");
            var rush = events.Where(e => e.EnemyTypeId == "StampedeOxDemon").ToArray();
            Assert.That(rush, Has.Length.EqualTo(5));
            var shadow = SoulHunter.Gameplay.Hazards.StageHazardTable.For(3)
                .Where(h => h.Kind == SoulHunter.Gameplay.Hazards.StageHazardKind.DeathsShadow).Select(h => h.TimeSeconds);
            Assert.That(shadow, Is.EqualTo(new[] { 0f, 480f, 720f }), "The Stalker on its own stage.");
            var t = DairyPlantEnemies.Table;
            Assert.That((t.Find("RingEmber").Health, t.Find("RingEmber").Damage, t.Find("RingEmber").VsSpeed, t.Find("RingEmber").Knockback), Is.EqualTo((180f, 8, 200f, 0.2f)));
            Assert.That((t.Find("DriftEmber").Damage, t.Find("DriftEmber").VsSpeed), Is.EqualTo((10, 150f)));
            Assert.That((t.Find("StampedeOxDemon").Health, t.Find("StampedeOxDemon").Damage, t.Find("StampedeOxDemon").VsSpeed), Is.EqualTo((250f, 9, 600f)));
        }

        [Test]
        public void AJellyRing_StartsWide_ClosesIn_AndComesBack()
        {
            float period = SoulHunter.Gameplay.Hazards.StageHazardTable.JellyPulseSeconds;
            Assert.That(SoulHunter.Gameplay.Hazards.PulsingRing.ScaleAt(0f), Is.EqualTo(1f).Within(1e-4));
            Assert.That(SoulHunter.Gameplay.Hazards.PulsingRing.ScaleAt(period * 0.5f), Is.EqualTo(SoulHunter.Gameplay.Hazards.StageHazardTable.JellyInnerScale).Within(1e-4));
            Assert.That(SoulHunter.Gameplay.Hazards.PulsingRing.ScaleAt(period), Is.EqualTo(1f).Within(1e-4));
        }

        [Test]
        public void LevelFour_PlaysGalloTowersMapEventRows()
        {
            var events = MapEvents.For(4);
            Assert.That(events, Has.Count.EqualTo(26), "28 rows on Gallo Tower; the two 30:00 Shooting Star rows end with the stage.");
            Assert.That(WaveTables.Events(4), Has.Count.EqualTo(26));
            var first = events[0];
            Assert.That((first.Type, first.Count, first.TimeSeconds, first.IntervalSeconds, first.Range), Is.EqualTo((StageEventType.StarRain, 2, 5f, 1f, 1f)),
                "0:05 Shooting Star: amount 2, interval 1.0, range 1 (max).");
            var streams = events.Where(e => e.Type == StageEventType.Stream).ToArray();
            Assert.That(streams.Select(e => (e.TimeSeconds, e.Count, e.IntervalSeconds, e.DurationSeconds)),
                Is.EqualTo(new[] { (370f, 5, 1.33f, 60f), (425f, 5, 1.33f, 60f), (900f, 5, 1f, 60f) }));
            var pile = events.Where(e => e.Type == StageEventType.PileRing).ToArray();
            Assert.That(pile.All(e => e.EnemyTypeId == "GateSorcerer"), Is.True, "Undead Mages.");
            Assert.That(pile[0].Range, Is.EqualTo(0.4f));
            Assert.That(events.Where(e => e.Type == StageEventType.Swarm).All(e => e.Horizontal), Is.True, "Dragon Swarms travel horizontally.");
            var t = GalloTowerEnemies.Table;
            var ds1 = t.Find("EmberSerpent");
            Assert.That((ds1.Health, ds1.HealthTimesLevel, ds1.Damage, ds1.VsSpeed, ds1.Knockback), Is.EqualTo((5f, true, 3, 400f, 0f)));
            Assert.That((t.Find("BlazeSerpent").Health, t.Find("BlazeSerpent").Damage), Is.EqualTo((20f, 6)));
            Assert.That(SoulHunter.Gameplay.Hazards.StageHazardTable.PileRangeScale(0.7f), Is.EqualTo(1f).Within(1e-4));
            Assert.That(SoulHunter.Gameplay.Hazards.StageHazardTable.StarRange(1f), Is.EqualTo(SoulHunter.Gameplay.Hazards.StageHazardTable.MeteorMaxSpread));
        }

        [Test]
        public void LevelFive_PlaysAbyssFoscarisMapEventRows_AndTheBlinder()
        {
            var events = MapEvents.For(5);
            Assert.That(events, Has.Count.EqualTo(27), "30 rows on Abyss Foscari, 3 of them The Blinder.");
            Assert.That(WaveTables.Events(5), Has.Count.EqualTo(27));
            Assert.That(events.Count(e => e.Type == StageEventType.PileRing), Is.EqualTo(7));
            Assert.That(events.Count(e => e.Type == StageEventType.PulsingRing), Is.EqualTo(7));
            Assert.That(events.Where(e => e.EnemyTypeId == "StampedeGhoul").All(e => e.Type == StageEventType.Swarm), Is.True);
            var blinder = SoulHunter.Gameplay.Hazards.StageHazardTable.For(5).Where(h => h.Kind == SoulHunter.Gameplay.Hazards.StageHazardKind.Blinder).ToArray();
            Assert.That(blinder.Select(h => (h.TimeSeconds, h.ChancePercent, h.DurationSeconds)),
                Is.EqualTo(new[] { (0f, 1f, 120f), (480f, 30f, 60f), (720f, 99f, 60f) }), "The_Blinder: 0:00 1% 2 min; 8:00 30% 1 min; 12:00 99% 1 min.");
            var b = SoulHunter.Gameplay.Hazards.StageHazardTable.Blinder;
            Assert.That((b.HealthPerLevel, b.VsSpeed, b.ContactDamage, b.VsXp, b.FreezeResistance, b.DebuffResistance, b.Intangible),
                Is.EqualTo((500, 150f, 25, 30f, 2f, true, false)));
            Assert.That(SoulHunter.Gameplay.Hazards.StageHazardTable.ReaperChest(b, SoulHunter.Gameplay.Hazards.ShadowKiller.Pentagram).HasValue, Is.True);
            Assert.That(SoulHunter.Gameplay.Hazards.StageHazardTable.ReaperChest(SoulHunter.Gameplay.Hazards.StageHazardTable.StalkerXL, SoulHunter.Gameplay.Hazards.ShadowKiller.Pentagram).HasValue, Is.False);
            var t = AbyssFoscariEnemies.Table;
            Assert.That((t.Find("StampedeGhoul").Health, t.Find("StampedeGhoul").VsSpeed, t.Find("RingMire").Damage, t.Find("DriftMire").VsSpeed),
                Is.EqualTo((250f, 600f, 6, 150f)));
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
