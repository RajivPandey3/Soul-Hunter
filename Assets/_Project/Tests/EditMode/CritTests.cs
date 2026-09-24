using NUnit.Framework;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: CritTests verifies the VS-style critical hit roll: weapons without a crit chance never
    /// crit, Luck multiplies the chance, and the chance is capped at 100%.
    /// </summary>
    public class CritTests
    {
        [TestCase(0f, 5f, 0f, false)]      // weapon cannot crit, whatever the Luck
        [TestCase(0.1f, 1f, 0.09f, true)]  // 10% at Luck 1
        [TestCase(0.1f, 1f, 0.10f, false)]
        [TestCase(0.1f, 1.5f, 0.14f, true)] // Luck 1.5 -> 15%
        [TestCase(0.1f, 1.5f, 0.15f, false)]
        [TestCase(0.5f, 3f, 0.999f, true)] // capped at 100%
        public void IsCrit_UsesBaseChanceTimesLuck(float baseChance, float luck, float roll, bool expected)
        {
            Assert.That(AutoAttackWeapon.IsCrit(baseChance, luck, roll), Is.EqualTo(expected));
        }

        [Test]
        public void CritsDoubleDamage()
        {
            Assert.That(AutoAttackWeapon.CritDamageMultiplier, Is.EqualTo(2f));
        }
    }
}
