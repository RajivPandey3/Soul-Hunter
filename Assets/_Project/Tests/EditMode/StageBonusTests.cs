using NUnit.Framework;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Pickups;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// VS stage bonuses (vampire.survivors.wiki/w/Stages): each stage has its own player move speed and gold multiplier,
    /// growing along the stage list. Soul Hunter's ten stages follow that climb in story order.
    /// </summary>
    public class StageBonusTests
    {
        [TearDown]
        public void TearDown() => CampaignLevelCatalog.CurrentStage = 0;

        [Test]
        public void Bonuses_StartLikeMadForest_AndNeverFallAlongTheStory()
        {
            Assert.That(CampaignLevelCatalog.PlayerSpeedFor(1), Is.EqualTo(1.1f).Within(0.001f), "Mad Forest: x1.1 move speed.");
            Assert.That(CampaignLevelCatalog.GoldFor(1), Is.EqualTo(1f).Within(0.001f));
            for (int stage = 2; stage <= 10; stage++)
            {
                Assert.That(CampaignLevelCatalog.PlayerSpeedFor(stage), Is.GreaterThanOrEqualTo(CampaignLevelCatalog.PlayerSpeedFor(stage - 1)), $"Stage {stage}");
                Assert.That(CampaignLevelCatalog.GoldFor(stage), Is.GreaterThanOrEqualTo(CampaignLevelCatalog.GoldFor(stage - 1)), $"Stage {stage}");
                Assert.That(CampaignLevelCatalog.PlayerSpeedFor(stage), Is.LessThanOrEqualTo(1.4f + 0.001f), "No more than the VS stages give.");
                Assert.That(CampaignLevelCatalog.GoldFor(stage), Is.LessThanOrEqualTo(1.5f + 0.001f));
            }
            Assert.That(CampaignLevelCatalog.Get(4).GoldMultiplier, Is.EqualTo(CampaignLevelCatalog.GoldFor(4)));
        }

        [Test]
        public void OutsideARun_ThereIsNoBonus()
        {
            CampaignLevelCatalog.CurrentStage = 0;
            Assert.That(CampaignLevelCatalog.CurrentPlayerSpeed, Is.EqualTo(1f));
            Assert.That(CampaignLevelCatalog.CurrentGold, Is.EqualTo(1f));
            Assert.That(GoldRewards.ApplyGreed(100, null), Is.EqualTo(100));
        }

        [Test]
        public void StageGold_MultipliesSeparatelyFromGreed()
        {
            CampaignLevelCatalog.CurrentStage = 5; // x1.4 gold
            Assert.That(GoldRewards.ApplyGreed(100, null), Is.EqualTo(140));
        }
    }
}
