using System.Reflection;
using NUnit.Framework;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// VS Hyper mode per stage (2026-10-09). Every number is the Hyper column of the stage's wiki infobox (raw pages
    /// Mad_Forest, Inlaid_Library, Dairy_Plant, Gallo_Tower, Cappella_Magna, fetched 2026-10-09), on Soul Hunter's stage
    /// mapping L1 Mad Forest, L2 Inlaid Library, L3 Dairy Plant, L4 Gallo Tower, L5-L10 Cappella Magna. The Stages page:
    /// "The overall bonuses granted by hyper mode differ with each stage".
    /// </summary>
    public class HyperModeTests
    {
        // Index 0 = Level 1.
        private static readonly float[] NormalPlayerSpeed = { 1.1f, 1.25f, 1.25f, 1.25f, 1.4f, 1.4f, 1.4f, 1.4f, 1.4f, 1.4f };
        private static readonly float[] HyperPlayerSpeed = { 2f, 1.9f, 1.9f, 1.9f, 2f, 2f, 2f, 2f, 2f, 2f };
        private static readonly float[] HyperProjectile = { 1.25f, 1.15f, 1.15f, 1.15f, 1.15f, 1.15f, 1.15f, 1.15f, 1.15f, 1.15f };
        private static readonly float[] HyperLuck = { 0f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f };
        // Hyper / normal enemy-speed. Level 2 (Inlaid Library: x1.25 in both columns) contradicts the Enemies page
        // ("increases ... the movement speed of enemies"); balance method: Dairy Plant's and Gallo Tower's 1.9 / 1.25.
        private static readonly float[] HyperEnemyRatio =
        {
            2f / 1.1f, 1.9f / 1.25f, 1.9f / 1.25f, 1.9f / 1.25f, 2f / 1.4f, 2f / 1.4f, 2f / 1.4f, 2f / 1.4f, 2f / 1.4f, 2f / 1.4f
        };
        // Stage gold + 0.5. L6-L10 use Soul Hunter's DLC 1.5, and the DLC stages go +50% -> +100% in Hyper (wiki Stages:
        // Mt.Moonspell, Lake Foscari, Polus Replica), so 2.0 there.
        private static readonly float[] HyperGold = { 1.5f, 1.5f, 1.7f, 1.8f, 1.9f, 2f, 2f, 2f, 2f, 2f };

        private GameObject _object;

        [SetUp]
        public void SetUp() => RunModes.Reset();

        [TearDown]
        public void TearDown()
        {
            if (_object != null) Object.DestroyImmediate(_object);
            RunModes.Reset();
            CampaignLevelCatalog.CurrentStage = 0;
        }

        [Test]
        public void Hyper_PlayerSpeed_FollowsEachStagesInfobox()
        {
            for (int stage = 1; stage <= 10; stage++)
            {
                CampaignLevelCatalog.CurrentStage = stage;
                RunModes.Hyper = false;
                Assert.That(CampaignLevelCatalog.CurrentPlayerSpeed, Is.EqualTo(NormalPlayerSpeed[stage - 1]).Within(1e-4f), $"Level {stage} normal");
                RunModes.Hyper = true;
                Assert.That(CampaignLevelCatalog.CurrentPlayerSpeed, Is.EqualTo(HyperPlayerSpeed[stage - 1]).Within(1e-4f), $"Level {stage} Hyper");
                Assert.That(CampaignLevelCatalog.HyperPlayerSpeedFor(stage), Is.EqualTo(HyperPlayerSpeed[stage - 1]).Within(1e-4f));
            }
            CampaignLevelCatalog.CurrentStage = 0;
            Assert.That(CampaignLevelCatalog.CurrentPlayerSpeed, Is.EqualTo(1f), "Outside a run there is no stage, so no bonus.");
        }

        [Test]
        public void Hyper_EnemySpeedRatio_PerStage_AndInverseStacksOnTop()
        {
            for (int stage = 1; stage <= 10; stage++)
            {
                CampaignLevelCatalog.CurrentStage = stage;
                RunModes.Hyper = false; RunModes.Inverse = false;
                Assert.That(RunModes.EnemySpeedMultiplier(10f), Is.EqualTo(1f), $"Level {stage}: no mode, no change");
                RunModes.Hyper = true;
                Assert.That(RunModes.EnemySpeedMultiplier(10f), Is.EqualTo(HyperEnemyRatio[stage - 1]).Within(1e-4f), $"Level {stage} Hyper");
                RunModes.Inverse = true;
                Assert.That(RunModes.EnemySpeedMultiplier(10f), Is.EqualTo(HyperEnemyRatio[stage - 1] * 1.05f).Within(1e-4f),
                    $"Level {stage}: Inverse +0.5% per minute multiplies on top");
            }
            Assert.That(CampaignLevelCatalog.HyperEnemySpeedFor(1), Is.EqualTo(1.818f).Within(0.001f));
            Assert.That(CampaignLevelCatalog.HyperEnemySpeedFor(3), Is.EqualTo(1.52f).Within(0.001f));
            Assert.That(CampaignLevelCatalog.HyperEnemySpeedFor(5), Is.EqualTo(1.4286f).Within(0.001f));
        }

        [Test]
        public void Hyper_ProjectileSpeed_Is1_25OnLevel1_And1_15Elsewhere()
        {
            for (int stage = 1; stage <= 10; stage++)
            {
                CampaignLevelCatalog.CurrentStage = stage;
                RunModes.Hyper = false;
                Assert.That(RunModes.ProjectileSpeedMultiplier, Is.EqualTo(1f), $"Level {stage} normal");
                RunModes.Hyper = true;
                Assert.That(RunModes.ProjectileSpeedMultiplier, Is.EqualTo(HyperProjectile[stage - 1]).Within(1e-4f), $"Level {stage} Hyper");
            }
        }

        [Test]
        public void Hyper_Luck_PlusTenPercentExceptLevel1()
        {
            for (int stage = 1; stage <= 10; stage++)
            {
                RunModes.Hyper = false;
                Assert.That(RunModes.HyperLuckBonus(stage), Is.EqualTo(0f), $"Level {stage} normal");
                RunModes.Hyper = true;
                Assert.That(RunModes.HyperLuckBonus(stage), Is.EqualTo(HyperLuck[stage - 1]).Within(1e-4f), $"Level {stage} Hyper");
            }
            RunModes.Inverse = true;
            Assert.That(RunModes.LuckBonus, Is.EqualTo(0.2f).Within(1e-4f), "Inverse's +20% is separate and stays.");
        }

        [Test]
        public void Hyper_LuckFollowsTheStage_AndNeverStacks()
        {
            RunModes.Hyper = true;
            _object = new GameObject("HyperLuck");
            var stats = _object.AddComponent<PlayerStats>();
            var progression = _object.AddComponent<LevelProgressionManager>();
            var apply = typeof(LevelProgressionManager).GetMethod("ApplyHyperLuck", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(apply);
            float baseLuck = stats.Luck;

            void Enter(int stage)
            {
                progression.StartStage(stage); // no PlayerController here, so apply to the stats directly
                apply.Invoke(progression, new object[] { stats });
            }

            Enter(1);
            Assert.That(stats.Luck, Is.EqualTo(baseLuck).Within(1e-4f), "Mad Forest gives no Hyper Luck.");
            Enter(2);
            Assert.That(stats.Luck, Is.EqualTo(baseLuck + 0.1f).Within(1e-4f), "Level 2: +10% Luck.");
            Enter(3);
            Enter(7);
            Assert.That(stats.Luck, Is.EqualTo(baseLuck + 0.1f).Within(1e-4f), "Later stages do not stack it.");
            Assert.That(progression.HyperLuckApplied, Is.EqualTo(0.1f).Within(1e-4f));
            Enter(1);
            Assert.That(stats.Luck, Is.EqualTo(baseLuck).Within(1e-4f), "Back on Level 1 (Endless) it is taken back.");
        }

        [Test]
        public void Hyper_GoldIsTheStagesGoldPlusHalf_AndInverseAddsTwo()
        {
            for (int stage = 1; stage <= 10; stage++)
            {
                CampaignLevelCatalog.CurrentStage = stage;
                RunModes.Hyper = true; RunModes.Inverse = false;
                Assert.That(CampaignLevelCatalog.CurrentGold, Is.EqualTo(HyperGold[stage - 1]).Within(1e-4f), $"Level {stage} Hyper");
                RunModes.Inverse = true;
                Assert.That(CampaignLevelCatalog.CurrentGold, Is.EqualTo(HyperGold[stage - 1] + 2f).Within(1e-4f), $"Level {stage} Hyper + Inverse");
            }
        }

        [Test]
        public void Hyper_RaisesTheMinimumOnly_NotTheSpawnFrequency_AndEndlessRaisesBoth()
        {
            Assert.That(RunModes.MinimumMultiplier, Is.EqualTo(1f));
            Assert.That(RunModes.FrequencyMultiplier, Is.EqualTo(1f));
            RunModes.Hyper = true;
            Assert.That(RunModes.MinimumMultiplier, Is.EqualTo(1.25f).Within(1e-4f), "Every infobox: enemy-min2 x1.25.");
            Assert.That(RunModes.FrequencyMultiplier, Is.EqualTo(1f), "No wiki page gives Hyper a spawn-interval change.");
            RunModes.EndlessCycle = 2;
            Assert.That(RunModes.MinimumMultiplier, Is.EqualTo(1.25f * 2f).Within(1e-4f), "Endless: +50% quantity per cycle.");
            Assert.That(RunModes.FrequencyMultiplier, Is.EqualTo(2f).Within(1e-4f), "Endless: +50% frequency per cycle.");
        }

        [Test]
        public void InverseAndEndlessHealth_Add()
        {
            // Endless: +100% of base HP per cycle; Inverse: x(3 + 0.05 per minute), "additive with other enemy bonus modifiers".
            try
            {
                RunModes.Reset();
                RunModes.Inverse = true;
                Assert.That(RunModes.EnemyHealthMultiplier(10f), Is.EqualTo(3.5f).Within(1e-4f));
                RunModes.Endless = true;
                RunModes.EndlessCycle = 1;
                Assert.That(RunModes.EnemyHealthMultiplier(10f), Is.EqualTo(4.5f).Within(1e-4f), "3 + 0.5 + 1, not 3.5 x 2.");
                RunModes.Inverse = false;
                Assert.That(RunModes.EnemyHealthMultiplier(10f), Is.EqualTo(2f).Within(1e-4f));
            }
            finally { RunModes.Reset(); }
        }
    }
}
