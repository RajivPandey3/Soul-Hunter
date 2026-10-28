using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using SoulHunter.Gameplay.Audio;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Hazards;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Rows of Docs/VS_DOT_Table.md (raw wiki pages fetched 2026-10-13): the chest-less wave bosses of every level
    /// (stage pages, "Waves", "Bosses & Treasure" cells without a {{treasure}}), the Omens added from Random_Events and its
    /// 29:30 pick, and the Inverse mode track of a level (Stages, "Inverse Mode").
    /// </summary>
    public class ChestlessBossAndOmenTests
    {
        // ---------------------------------------------------------------- Chest-less wave bosses

        [Test]
        public void TheTable_IsTheWikisRows_LevelByLevel()
        {
            // Mad Forest 8:00 Giant Bat, 21:00 and 24:00 Venus.
            CollectionAssert.AreEqual(new[] { (8, "GreatGraveBat"), (21, "GraveLily"), (24, "GraveLily") }, ChestlessWaveBosses.For(1));
            // Inlaid Library: every boss cell has a {{treasure}}.
            Assert.That(ChestlessWaveBosses.For(2), Is.Empty);
            // Dairy Plant 24:00 Giant Armored Knight (the plain Sword Guardian statbox).
            CollectionAssert.AreEqual(new[] { (24, "RuinedKnight") }, ChestlessWaveBosses.For(3));
            // Gallo Tower 1:00 Scarleton, 4:00 Undead Mage.
            CollectionAssert.AreEqual(new[] { (1, "ThriceRisen"), (4, "GateSorcerer") }, ChestlessWaveBosses.For(4));
            // Abyss Foscari: Elatha with the stage boss (Soul Hunter's Level 5 guardian comes at 20:00), 24:00 Maronna Meea Boss.
            CollectionAssert.AreEqual(new[] { (20, "AbyssalConsort"), (24, "GreatBogMother") }, ChestlessWaveBosses.For(5));
            // Mt.Moonspell 8:00 Thunderous Oni, 10:00 Windy Oni (with the chest boss), 21:00 Tsuchigumo, 24:00 Tsuchigumo Boss.
            CollectionAssert.AreEqual(new[] { (8, "ThunderOgre"), (10, "GaleOgre"), (21, "GlacierSpider"), (24, "GreatGlacierSpider") }, ChestlessWaveBosses.For(6));
            // Lake Foscari 20:00 Missing Church Boss 2 (with the chest boss).
            CollectionAssert.AreEqual(new[] { (20, "GreatTollingBell") }, ChestlessWaveBosses.For(7));
            // Cappella Magna: none.
            for (int level = 8; level <= 10; level++) Assert.That(ChestlessWaveBosses.For(level), Is.Empty, $"Level {level}");
        }

        [Test]
        public void EachBoss_IsATypeOfItsLevel_AndStandsInThatLevelsWaveAsset()
        {
            for (int level = 1; level <= 10; level++)
            {
                var bosses = ChestlessWaveBosses.For(level);
                Assert.IsNotNull(bosses, $"Level {level}");
                var wave = AssetDatabase.LoadAssetAtPath<WaveData>(WaveTables.StageWaveAssets[level - 1]);
                Assert.IsNotNull(wave, $"Level {level} wave asset.");
                int last = -1;
                foreach (var (minute, id) in bosses)
                {
                    Assert.That(minute, Is.InRange(0, 29), $"Level {level} {id}");
                    Assert.That(minute, Is.Not.EqualTo(25), $"Level {level}: 25:00 is the stage boss, the owner's.");
                    Assert.That(minute, Is.GreaterThan(last), $"Level {level}: earliest first, one per minute (the spawner walks the list in order).");
                    last = minute;
                    if (level == 1) Assert.DoesNotThrow(() => MadForestEnemies.Find(id), id);
                    else Assert.DoesNotThrow(() => StageEnemyTables.For(level).Find(id), $"Level {level} {id}");

                    string prefab = ChestlessWaveBosses.PrefabName(level, id);
                    Assert.That(prefab, Is.EqualTo($"SH10_L{level:00}_{id}_Gameplay"));
                    bool inWave = wave.Minutes.Any(m => m != null && m.EnemyTypes != null && m.EnemyTypes.Any(p => p != null && p.name == prefab))
                               || (wave.BossTypes != null && wave.BossTypes.Any(p => p != null && p.name == prefab));
                    Assert.That(inWave, Is.True, $"Level {level}: {prefab} is one of the wave asset's types (a minute's or boss-only), so the spawner finds it.");
                }
            }
        }

        [Test]
        public void EachRow_NamesItsVsEnemy_AndTheTypeFollowsThatStatbox()
        {
            foreach (var row in ChestlessWaveBosses.Rows)
            {
                Assert.That(row.Level, Is.InRange(2, 10));
                Assert.That(string.IsNullOrEmpty(row.VsEnemy), Is.False);
                var type = StageEnemyTables.For(row.Level).Find(row.Id);
                Assert.That(row.VsEnemy, Does.StartWith(type.VsVariant), $"Level {row.Level} {row.Id}");
            }
            Assert.That(ChestlessWaveBosses.Rows.Count(r => r.VsEnemy.Contains("stand-in")), Is.EqualTo(0), "Every chest-less boss has its own type.");
        }

        [Test]
        public void LevelFivesBoss_FollowsItsOwnStatbox_AndComesOnlyAsABoss()
        {
            // Maronna_Meea, "Maronna Meea Boss": HP x Level 200, movespeed 180, damage 20, knockback 0 (cap 1), xp 30,
            // res-freeze 1.1, res-kill 1, res-debuff 1.
            var table = StageEnemyTables.For(5);
            var type = table.Find("GreatBogMother");
            Assert.That(type.HealthTimesLevel, Is.True);
            Assert.That(type.Health, Is.EqualTo(200f));
            Assert.That(type.VsSpeed, Is.EqualTo(180f));
            Assert.That(type.Damage, Is.EqualTo(20));
            Assert.That(type.Knockback, Is.EqualTo(0f));
            Assert.That(type.MaxKnockback, Is.EqualTo(1f));
            Assert.That(type.VsXp, Is.EqualTo(30f));
            Assert.That(type.FreezeResistance, Is.EqualTo(1.1f).Within(1e-5));
            Assert.That(type.InstantKillResistance, Is.True);
            Assert.That(type.DebuffResistance, Is.True);
            CollectionAssert.Contains(table.BossOnly, "GreatBogMother");
            Assert.That(table.ByMinute.Any(m => m.Contains("GreatBogMother")), Is.False, "The wiki's Enemies column never lists it.");

            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(table.DataPath("GreatBogMother"));
            Assert.IsNotNull(data, "Built asset.");
            Assert.That(data.HealthPerPlayerLevel, Is.EqualTo(200));
            Assert.That(data.FreezeResistance, Is.EqualTo(1.1f).Within(1e-5));
            Assert.That(data.InstantKillResistance, Is.True);
            Assert.That(data.DebuffResistance, Is.True);
            var wave = AssetDatabase.LoadAssetAtPath<WaveData>(WaveTables.StageWaveAssets[4]);
            CollectionAssert.AreEqual(table.BossOnly.Select(table.PrefabName).ToArray(), wave.BossTypes.Select(p => p.name).ToArray(),
                "Level 5's boss-only types: Great Bog Mother and its chest bosses (StageChestBosses).");
            CollectionAssert.Contains(wave.BossTypes.Select(p => p.name).ToArray(), table.PrefabName("GreatBogMother"));
        }

        // ---------------------------------------------------------------- Inverse mode music

        [Test]
        public void Inverse_PlaysTheMirrorLevelsTrack_UnlessATrackWasChosen()
        {
            Assert.That(MusicSelection.LevelTrackCount, Is.EqualTo(10));
            for (int level = 1; level <= 10; level++)
            {
                Assert.That(MusicSelection.StageTrackLevel(MusicSelection.LevelsOwn, level, false), Is.EqualTo(level), "Without Inverse: its own track.");
                int mirror = MusicSelection.StageTrackLevel(MusicSelection.LevelsOwn, level, true);
                Assert.That(mirror, Is.EqualTo(11 - level), $"Level {level} in Inverse.");
                Assert.That(mirror, Is.Not.EqualTo(level), "No level keeps its own track in Inverse.");
                Assert.That(MusicSelection.StageTrackLevel(MusicSelection.LevelsOwn, mirror, true), Is.EqualTo(level), "A mirror pair.");
                // A chosen track wins (ClipFor then ignores the stage anyway).
                Assert.That(MusicSelection.StageTrackLevel(MusicSelection.MainTheme, level, true), Is.EqualTo(level));
                Assert.That(MusicSelection.StageTrackLevel(MusicSelection.FirstLevelTrack + 3, level, true), Is.EqualTo(level));
            }
            Assert.That(MusicSelection.StageTrackLevel(MusicSelection.LevelsOwn, 0, true), Is.EqualTo(10), "Out-of-range stages are clamped.");
            Assert.That(MusicSelection.StageTrackLevel(MusicSelection.LevelsOwn, 99, true), Is.EqualTo(1));
        }

        // ---------------------------------------------------------------- Omens

        private static readonly OmenEvent[] Added =
            { OmenEvent.Stampede, OmenEvent.DreadCircles, OmenEvent.TramplingRanks, OmenEvent.CrossfireRows };

        [Test]
        public void TheAddedOmens_AreInEveryPool_UnderTheirOwnNames()
        {
            for (int stage = 1; stage <= 10; stage++)
                foreach (var e in Added)
                    Assert.That(OmenTable.Pool(stage, true).Contains(e), Is.True, $"Stage {stage}: {e}");
            CollectionAssert.AreEqual(new[] { "Stampede", "Dread Circles", "Trampling Ranks", "Crossfire Rows" }, Added.Select(OmenTable.Name).ToArray());
            var vsNames = new[] { "Mino Rush", "Minotaur Rush", "Panic Time", "Crab Fest", "Scorpion Lair", "Skull Pile Pile" };
            Assert.That(Added.Select(OmenTable.Name).Intersect(vsNames), Is.Empty, "No VS event name is shown.");
            // New values went at the end of the enum (the earlier ones keep their numbers).
            Assert.That((int)OmenEvent.MarchingLine, Is.EqualTo(15));
            Assert.That((int)OmenEvent.Stampede, Is.EqualTo(16));
        }

        [Test]
        public void TheWikiGivesNoWeights_SoEveryEventOfAPoolIsEquallyLikely()
        {
            foreach (int stage in new[] { 1, 5 })
            {
                var pool = OmenTable.Pool(stage, true);
                var hits = new Dictionary<OmenEvent, int>();
                const int rollsPerEvent = 50;
                int rolls = pool.Count * rollsPerEvent;
                for (int i = 0; i < rolls; i++)
                {
                    var e = OmenTable.Pick(pool, (i + 0.5f) / rolls);
                    hits[e] = hits.TryGetValue(e, out int n) ? n + 1 : 1;
                }
                Assert.That(hits.Count, Is.EqualTo(pool.Count), $"Stage {stage}: every event can be picked.");
                foreach (var pair in hits)
                    Assert.That(pair.Value, Is.EqualTo(rollsPerEvent), $"Stage {stage}: {pair.Key} has an equal share of the roll.");
            }
        }

        [Test]
        public void FormationOmens_StayOutOfABossFight_CrossfireRowsDoesNot()
        {
            foreach (var e in new[] { OmenEvent.Stampede, OmenEvent.DreadCircles, OmenEvent.TramplingRanks })
            {
                Assert.That(OmenTable.IsFormation(e), Is.True, e.ToString());
                Assert.That(OmenTable.Pool(1, false).Contains(e), Is.False, $"{e} is a formation: the spawner sends none while a stage boss lives.");
            }
            Assert.That(OmenTable.IsFormation(OmenEvent.CrossfireRows), Is.False, "Crossfire Rows is a hazard, as Gallows Ring is.");
            Assert.That(OmenTable.Pool(1, false).Contains(OmenEvent.CrossfireRows), Is.True);
        }

        [Test]
        public void The2930Pick_OfATimedStage_ChoosesAmongTheEventsThatOutlastTheStageClear()
        {
            for (int stage = 1; stage <= 10; stage++)
                foreach (bool formations in new[] { true, false })
                {
                    var pool = OmenTable.Pool(stage, formations, stageClearsAtFire: true);
                    CollectionAssert.AreEquivalent(
                        new[] { OmenEvent.HellfireBreath, OmenEvent.GildedFever, OmenEvent.FortunesFavour }, pool, $"Stage {stage}");
                }
            // The default is the ordinary pool.
            CollectionAssert.AreEqual(OmenTable.Pool(3, true), OmenTable.Pool(3, true, false));
            foreach (OmenEvent e in System.Enum.GetValues(typeof(OmenEvent)))
                if (OmenTable.OutlastsStageClear(e)) Assert.That(OmenTable.IsFormation(e), Is.False, e.ToString());
        }

        [Test]
        public void TheAddedOmens_Numbers_FollowTheWiki()
        {
            Assert.That(OmenTable.StampedeWaves * OmenTable.StampedeGapSeconds, Is.EqualTo(30f), "Mino Rush: \"for ~30 seconds\".");
            Assert.That(OmenTable.StampedeSpeed, Is.EqualTo(600f / 280f).Within(1e-5f), "Mignotaur (rush) movespeed 600 against Mignotaur's 280.");
            Assert.That(OmenTable.CrossfireSeconds, Is.EqualTo(60f), "Scorpion Lair: \"They disappear after 1 minute.\"");
            Assert.That(OmenTable.CrossfirePerRow, Is.EqualTo(12), "Skull Pile Pile: \"a pair of columns of 12\".");
            Assert.That(OmenTable.DreadRings, Is.GreaterThan(1), "Panic Time: \"several rings\".");
            Assert.That(OmenTable.TramplingRanksCount, Is.GreaterThan(1), "Crab Fest: \"hordes\".");
        }

        [Test]
        public void CrossfireRows_StandInTwoColumns_LeftAndRightOfThePlayer()
        {
            int per = StageHazardTable.PileColumnLength;
            var spots = Enumerable.Range(0, per * 2).Select(i => StageHazardTable.PileColumnOffset(i, per)).ToList();
            Assert.That(spots.Count(s => s.x < 0f), Is.EqualTo(per), "One column on the left.");
            Assert.That(spots.Count(s => s.x > 0f), Is.EqualTo(per), "One on the right.");
            Assert.That(spots.Select(s => Mathf.Abs(s.x)).Distinct().Count(), Is.EqualTo(1), "Both the same distance out.");
            Assert.That(spots.Distinct().Count(), Is.EqualTo(spots.Count), "No two shooters share a spot.");
            Assert.That(spots.All(s => s.y == 0f), Is.True);
            Assert.That(spots.Max(s => s.z), Is.EqualTo(StageHazardTable.PileRadiusZ).Within(1e-4f));
            Assert.That(spots.Min(s => s.z), Is.EqualTo(-StageHazardTable.PileRadiusZ).Within(1e-4f));
            Assert.That(StageHazardTable.PileColumnOffset(0, 1), Is.EqualTo(new Vector3(-StageHazardTable.PileRadiusX, 0f, 0f)), "A one-enemy column stands level with the player.");
        }
    }
}
