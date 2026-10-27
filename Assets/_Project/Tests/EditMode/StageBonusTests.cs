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
        public void Bonuses_AreEachLevelsSourceStageInfobox()
        {
            // Raw infoboxes (2026-10-27): Mad Forest, Inlaid Library, Dairy Plant, Gallo Tower, Abyss Foscari,
            // Mt.Moonspell, Lake Foscari, then Cappella Magna on Levels 8-10.
            var speed = new[] { 1.1f, 1.25f, 1.25f, 1.25f, 1.4f, 1.4f, 1.4f, 1.4f, 1.4f, 1.4f };
            var gold = new[] { 1f, 1f, 1.2f, 1.3f, 1.5f, 1.5f, 1.5f, 1.4f, 1.4f, 1.4f };
            for (int stage = 1; stage <= 10; stage++)
            {
                Assert.That(CampaignLevelCatalog.PlayerSpeedFor(stage), Is.EqualTo(speed[stage - 1]).Within(0.001f), $"Level {stage} move speed");
                Assert.That(CampaignLevelCatalog.GoldFor(stage), Is.EqualTo(gold[stage - 1]).Within(0.001f), $"Level {stage} gold");
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
            CampaignLevelCatalog.CurrentStage = 8; // Cappella Magna x1.4 gold
            Assert.That(GoldRewards.ApplyGreed(100, null), Is.EqualTo(140));
        }
    }
}
