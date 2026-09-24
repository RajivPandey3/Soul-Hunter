using NUnit.Framework;
using SoulHunter.Gameplay.Core;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: BossStageAdvanceTests verifies the owner decision that killing a stage boss clears the
    /// stage and moves the run on, that a stage clears only once, and that the Level 10 boss wins the run.
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

        [Test]
        public void BossKill_ClearsStage_AndStartsTransition()
        {
            // Learning Comment: Boss marne par stage clear ho jaye, 30 minute ka intezaar na ho.
            int clearedStage = 0;
            _progression.OnStageCleared += stage => clearedStage = stage;

            _progression.ReportBossDefeated();

            Assert.That(clearedStage, Is.EqualTo(1));
            Assert.That(_progression.IsStageTransitioning, Is.True, "Agla stage shuru hone tak transition chalna chahiye.");
        }

        [Test]
        public void StageClears_OnlyOnce()
        {
            int clears = 0;
            _progression.OnStageCleared += _ => clears++;

            _progression.ReportBossDefeated();
            _progression.ReportBossDefeated();

            Assert.That(clears, Is.EqualTo(1));
        }

        [Test]
        public void StartingNextStage_EndsTransition()
        {
            _progression.ReportBossDefeated();

            _progression.StartStage(2);

            Assert.That(_progression.CurrentStage, Is.EqualTo(2));
            Assert.That(_progression.IsStageTransitioning, Is.False);
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
