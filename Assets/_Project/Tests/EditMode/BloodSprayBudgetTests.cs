using NUnit.Framework;
using SoulHunter.Gameplay.VFX;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Owner (2026-09-29, option 2): a normal kill sprays 300 blood particles from the neck; when many enemies die at once,
    /// new sprays get fewer so the frame rate holds.
    /// </summary>
    public class BloodSprayBudgetTests
    {
        [Test]
        public void ThePulses_Add_Up_To300()
        {
            int total = 0;
            foreach (var pulse in VFXPoolManager.SprayPulses) total += pulse.Count * VFXPoolManager.SprayStepsPerPulse;
            Assert.That(total, Is.EqualTo(VFXPoolManager.SprayParticles));
        }

        [Test]
        public void AFewSprays_Get300Each_ManyShareABudget_NeverBelowTheFloor()
        {
            for (int playing = 0; playing < VFXPoolManager.FullSprays; playing++)
                Assert.That(VFXPoolManager.SprayParticlesFor(playing), Is.EqualTo(300), $"{playing} sprays in the air");

            int previous = 300;
            for (int playing = VFXPoolManager.FullSprays; playing <= 64; playing++)
            {
                int particles = VFXPoolManager.SprayParticlesFor(playing);
                Assert.That(particles, Is.LessThan(300), $"{playing} sprays in the air");
                Assert.That(particles, Is.LessThanOrEqualTo(previous), "More sprays in the air never means more particles each.");
                Assert.That(particles, Is.GreaterThanOrEqualTo(VFXPoolManager.MinSprayParticles), "A spray always shows.");
                previous = particles;
            }
            Assert.That(VFXPoolManager.SprayParticlesFor(31), Is.LessThanOrEqualTo(100), "A full pool of 32 shares the budget.");
        }
    }
}
