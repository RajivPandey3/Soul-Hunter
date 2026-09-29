using NUnit.Framework;
using SoulHunter.Gameplay.Core;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: BossStageAdvanceTests verifies the owner decisions that a stage runs the full 30 minutes (VS;
    /// 2026-09-27, killing its boss no longer ends it), that a stage clears only once, and that the Level 10 boss wins the run.
    /// </summary>
    public class BossStageAdvanceTests
    {
        private GameObject _object;
        private LevelProgressionManager _progression;

        [SetUp]
        public void SetUp()
        {
            _object = new GameObject("Progression");
            _progression = _object.AddComponent<LevelProgressionManager>();
        }

        [TearDown]
        public void TearDown()
        {
            SoulHunter.Core.Services.GameTime.ResetAll();
            Object.DestroyImmediate(_object);
        }

        private void SurviveThirtyMinutes() =>
            typeof(LevelProgressionManager).GetMethod("CompleteStageAfterSurvival",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(_progression, null);

        [Test]
        public void BossKill_DoesNotEndTheStage_ItRunsToThirtyMinutes()
        {
            // Owner (2026-09-27): every stage lasts the full 30 minutes, as in VS.
            int clears = 0;
            _progression.OnStageCleared += _ => clears++;

            _progression.ReportBossDefeated();

            Assert.That(_progression.IsBossDefeated, Is.True);
            Assert.That(clears, Is.EqualTo(0), "Killing the 25:00 boss no longer clears the stage.");
            Assert.That(_progression.IsStageTransitioning, Is.False);

            SurviveThirtyMinutes();
            Assert.That(clears, Is.EqualTo(1), "30:00 clears the stage.");
            Assert.That(_progression.IsStageTransitioning, Is.True, "Agla stage shuru hone tak transition chalna chahiye.");
        }

        [Test]
        public void ClearingAStage_PaysVsGold()
        {
            // VS wiki (The_Reaper, Revival): 500 gold, then +100, +200, +300, +400 per unused revival, then +100 each.
            Assert.That(LevelProgressionManager.StageClearGold(0), Is.EqualTo(500));
            Assert.That(LevelProgressionManager.StageClearGold(1), Is.EqualTo(600));
            Assert.That(LevelProgressionManager.StageClearGold(3), Is.EqualTo(1100), "The wiki's own example: 3 revives = 1,100.");
            Assert.That(LevelProgressionManager.StageClearGold(4), Is.EqualTo(1500));
            Assert.That(LevelProgressionManager.StageClearGold(6), Is.EqualTo(1700), "Past +400, each revival gives +100.");
            SurviveThirtyMinutes();
            Assert.That(_progression.LastStageClearGold, Is.EqualTo(500), "No player here: Greed 1, and stage 1 has no gold bonus.");
        }

        [Test]
        public void StageClears_OnlyOnce()
        {
            int clears = 0;
            _progression.OnStageCleared += _ => clears++;

            SurviveThirtyMinutes();
            SurviveThirtyMinutes();

            Assert.That(clears, Is.EqualTo(1));
        }

        [Test]
        public void StartingNextStage_EndsTransition()
        {
            SurviveThirtyMinutes();

            _progression.StartStage(2);

            Assert.That(_progression.CurrentStage, Is.EqualTo(2));
            Assert.That(_progression.IsStageTransitioning, Is.False);
            Assert.That(_progression.IsBossDefeated, Is.False, "A new stage has a new boss.");
        }

        [Test]
        public void FinalBossKill_WinsTheRun()
        {
            // Learning Comment: Level 10 ka boss (Shadow Kael) marne par poora run jeet liya jaye.
            bool won = false;
            _progression.OnGameWon += () => won = true;
            _progression.StartStage(LevelProgressionManager.MaxStages);

            _progression.ReportBossDefeated();

            Assert.That(won, Is.True);
        }
    }
}
