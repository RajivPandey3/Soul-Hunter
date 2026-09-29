using NUnit.Framework;
using SoulHunter.Core.Persistence;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Heal balance pass (owner, 2026-09-29), from vampire.survivors.wiki: the Max Health PowerUp (+10% per rank, 3 ranks,
    /// multiplied) and Recovery PowerUp (+0.1 HP/s per rank, 5 ranks), both starting at 200 gold (PowerUps page), and
    /// Recovery boosting pickup healing by 10% per 0.1 (Recovery page).
    /// </summary>
    public class HealBalanceTests
    {
        [Test]
        public void MaxHealthPowerUp_Multiplies_ByTenPercentPerRank_UpToThree()
        {
            Assert.That(MetaPowerUps.MaxHealthMultiplier(0), Is.EqualTo(1f));
            Assert.That(MetaPowerUps.MaxHealthMultiplier(1), Is.EqualTo(1.1f).Within(1e-5));
            Assert.That(MetaPowerUps.MaxHealthMultiplier(2), Is.EqualTo(1.21f).Within(1e-5));
            Assert.That(MetaPowerUps.MaxHealthMultiplier(3), Is.EqualTo(1.331f).Within(1e-5), "VS: +33.1% at rank 3.");
            Assert.That(MetaPowerUps.MaxHealthMultiplier(9), Is.EqualTo(1.331f).Within(1e-5), "Never past rank 3.");
            Assert.That(MetaPowerUps.MaxHealthPrice, Is.EqualTo(200));
        }

        [Test]
        public void RecoveryPowerUp_GivesATenthPerRank_UpToHalf()
        {
            Assert.That(MetaPowerUps.Recovery(0), Is.EqualTo(0f));
            Assert.That(MetaPowerUps.Recovery(3), Is.EqualTo(0.3f).Within(1e-5));
            Assert.That(MetaPowerUps.Recovery(5), Is.EqualTo(0.5f).Within(1e-5));
            Assert.That(MetaPowerUps.Recovery(8), Is.EqualTo(0.5f).Within(1e-5), "VS: max 0.5.");
            Assert.That(MetaPowerUps.RecoveryPrice, Is.EqualTo(200));
        }

        [Test]
        public void Recovery_BoostsPickupHealing_TenPercentPerTenth()
        {
            Assert.That(HealthPickup.HealWithRecovery(30, 0f), Is.EqualTo(30));
            Assert.That(HealthPickup.HealWithRecovery(30, 0.5f), Is.EqualTo(45));
            Assert.That(HealthPickup.HealWithRecovery(30, 1f), Is.EqualTo(60));
        }

        [Test]
        public void TheSelectScreen_ShowsTheHealingPowerUps()
        {
            var sheet = CharacterStatSheet.Build(null, metaMaxHealth: 3, metaRecovery: 5);
            Assert.That(sheet.MaxHealth, Is.EqualTo(133));
            Assert.That(sheet.Recovery, Is.EqualTo(0.5f).Within(1e-5));
        }
    }
}
