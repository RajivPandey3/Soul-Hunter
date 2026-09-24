using NUnit.Framework;
using SoulHunter.Gameplay.AI;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: CurseTests verifies VS-style Curse scaling of enemy health: 1.0 changes nothing,
    /// higher Curse makes enemies tougher, and health never drops below 1.
    /// </summary>
    public class CurseTests
    {
        [TestCase(100, 1.0f, 100)]  // default Curse: unchanged
        [TestCase(100, 1.1f, 110)]  // one Skull O'Maniac level
        [TestCase(85, 1.5f, 128)]   // rounds to nearest
        [TestCase(1, 0.1f, 1)]      // never below 1
        [TestCase(10, 0f, 1)]       // Curse is clamped to 0.1 minimum
        public void ScaleHealthByCurse_MultipliesEnemyHealth(int baseHealth, float curse, int expected)
        {
            Assert.That(EnemySpawner.ScaleHealthByCurse(baseHealth, curse), Is.EqualTo(expected));
        }
    }
}
