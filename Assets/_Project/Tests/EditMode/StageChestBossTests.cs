using System.Linq;
using NUnit.Framework;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Each level's chest bosses follow its source stage's VS "Waves" table (raw pages fetched 2026-10-27; the parsed
    /// rows are in Docs/VS_Audit_20261027). Arcana chests (11:00, 21:00) and the 25:00 stage boss are separate.
    /// </summary>
    public class StageChestBossTests
    {
        [TearDown]
        public void TearDown() => CampaignLevelCatalog.CurrentStage = 0;

        [Test]
        public void EveryLevel_HasItsSourceStagesMinutes()
        {
            var expected = new[]
            {
                new float[] { 1, 3, 5, 7, 9, 10, 12, 14, 15, 16, 18, 20, 22, 23, 27, 29 },          // Mad Forest
                new float[] { 3, 5, 8, 10, 11, 12, 13, 14, 15, 16, 18, 20, 22, 23, 26, 27, 28 },   // Inlaid Library
                new float[] { 1, 3, 5, 7, 9, 10, 12, 16, 18, 20, 22, 23, 27 },                     // Dairy Plant
                new float[] { 3, 5, 8, 10, 12, 13, 14, 15, 16, 18, 20, 22, 23, 26, 27, 28 },       // Gallo Tower
                new float[] { 1, 3, 5, 7, 9, 10, 12, 16, 18, 20, 22, 23, 27 },                     // Abyss Foscari
                new float[] { 1, 3, 5, 7, 9, 10, 12, 14, 15, 16, 18, 20, 22, 23, 27, 29 },         // Mt.Moonspell
                new float[] { 2, 3, 5, 8, 10, 11, 12, 13, 14, 15, 16, 18, 20, 22, 23, 26, 27, 28 }, // Lake Foscari
                new float[] { 1, 3, 5, 7, 9, 10, 12, 15, 18, 20, 22, 23, 24, 27 },                 // Cappella Magna
            };
            for (int stage = 1; stage <= 10; stage++)
                Assert.That(StageChestBosses.MinutesFor(stage), Is.EqualTo(expected[System.Math.Min(stage, 8) - 1]), $"Level {stage}");
            Assert.That(SoulHunter.Gameplay.AI.EnemySpawner.DefaultChestEliteMinutes, Is.EqualTo(expected[0]));
        }

        [Test]
        public void Configs_AreTheWikiRows()
        {
            Assert.That(ChestTiers.ForStageMinute(2, 8f), Is.EqualTo(new ChestTiers(2, false, 3f, 10f, 50f, ChestTiers.Slot(1))),
                "Inlaid Library 8:00 Colossal Musc Musc.");
            Assert.That(ChestTiers.ForStageMinute(4, 10f), Is.EqualTo(new ChestTiers(1, true, 3f, 10f, 100f, ChestTiers.Slot(4) | ChestTiers.Slot(5))
                .WithEvolutionSlots(ChestTiers.Slot(2) | ChestTiers.Slot(3))), "Gallo Tower 10:00 Colossal Harpy.");
            Assert.That(ChestTiers.ForStageMinute(9, 20f), Is.EqualTo(new ChestTiers(1, true, 3f, 2f, 100f)
                .WithEvolutionSlots(ChestTiers.Slot(2) | ChestTiers.Slot(3))), "Level 9 (Cappella Magna) 20:00 Colossal Archdemon.");
            Assert.That(ChestTiers.ForStageMinute(7, 3f).CanEvolve, Is.False, "Lake Foscari 3:00 gives no evo.");
            Assert.That(ChestTiers.ForStageMinute(3, 1f), Is.EqualTo(new ChestTiers(1, true, 0f, 0f, 30f)), "Dairy Plant 1:00 Lizard Rook.");
        }

        [Test]
        public void ArcanaMinutes_AndTheStageBoss_AreSeparate()
        {
            for (int stage = 1; stage <= 10; stage++)
            {
                if (stage != 2 && stage != 7) Assert.That(ChestTiers.ForStageMinute(stage, 11f), Is.EqualTo(ChestTiers.ArcanaMinutes), $"Level {stage} 11:00");
                Assert.That(ChestTiers.ForStageMinute(stage, 21f), Is.EqualTo(ChestTiers.ArcanaMinutes), $"Level {stage} 21:00");
                Assert.That(ChestTiers.ForStageMinute(stage, 25f), Is.EqualTo(StageChestBosses.StageBossFor(stage)), $"Level {stage} 25:00");
                Assert.That(StageChestBosses.StageBossFor(stage).CanEvolve, Is.True);
            }
            Assert.That(StageChestBosses.StageBossFor(1), Is.EqualTo(ChestTiers.StageBoss), "Level 1: Giant Blue Venus.");
            Assert.That(StageChestBosses.StageBossFor(4).IsEvolutionSlot(4), Is.True, "Giant Enemy Crab: reward2-5 = evo.");
        }

        [Test]
        public void EveryChestBoss_IsAKnownTypeOfItsLevel()
        {
            for (int stage = 1; stage <= 10; stage++)
            {
                var ids = new System.Collections.Generic.List<string> { StageChestBosses.ArcanaCarrierFor(stage) };
                foreach (var row in StageChestBosses.For(stage)) ids.Add(row.TypeId);
                foreach (var id in ids)
                {
                    Assert.That(id, Is.Not.Null.And.Not.Empty, $"Level {stage}: every row names its boss type.");
                    if (stage == 1) Assert.DoesNotThrow(() => MadForestEnemies.Find(id), id);
                    else Assert.That(System.Array.Exists(StageEnemyTables.For(stage).Types, t => t.Id == id), Is.True, $"Level {stage}: {id}");
                }
            }
            Assert.That(StageChestBosses.TypeFor(1, 10f), Is.EqualTo("GreatMantisWraith"), "Mad Forest 10:00 Giant Mantichana.");
            Assert.That(MadForestEnemies.Find("GreatMantisWraith").FreezeResistance, Is.EqualTo(1.1f).Within(1e-5));
            Assert.That(StageChestBosses.TypeFor(2, 8f), Is.EqualTo("SporeColossus"), "Inlaid Library 8:00 Colossal Musc Musc.");
        }

        [Test]
        public void StartingSpawns_AreEachSourceStagesInfobox()
        {
            Assert.That(Enumerable.Range(1, 10).Select(s => WaveTables.StartingSpawns(s, false)),
                Is.EqualTo(new[] { 10, 50, 40, 50, 40, 50, 50, 40, 40, 40 }));
            Assert.That(Enumerable.Range(1, 10).Select(s => WaveTables.StartingSpawns(s, true)),
                Is.EqualTo(new[] { 15, 60, 60, 60, 60, 70, 70, 60, 60, 60 }));
        }

        [Test]
        public void ForMinute_UsesTheStageBeingPlayed()
        {
            CampaignLevelCatalog.CurrentStage = 2;
            Assert.That(ChestTiers.ForMinute(8f), Is.EqualTo(ChestTiers.ForStageMinute(2, 8f)));
            CampaignLevelCatalog.CurrentStage = 0;
            Assert.That(ChestTiers.ForMinute(1f), Is.EqualTo(ChestTiers.Minute1), "Outside a run: Level 1.");
        }
    }
}
