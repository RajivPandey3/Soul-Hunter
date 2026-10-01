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

        /// <summary>
        /// VS DOT Phase 1 row 7 (2026-10-01, wiki Critical_Hit): without Slash only Bloody Tear (10%, 2x) and Heaven Sword
        /// (10%, 2.5x) crit; Whip (20%, 2x), Knife (30%, 3x) and Axe (30%, 2x) crit only with Slash, which doubles the
        /// multiplier; Thousand Edge (30%, 3x) and Death Spiral (30%, 2x) too with Slash (their own wiki pages); the Cross never crits.
        /// </summary>
        [TestCase("Assets/Prefabs/Weapons/Whip_Weapon.prefab", 0f, 0.2f, 2f)]
        [TestCase("Assets/Prefabs/Weapons/Generated/Knife.prefab", 0f, 0.3f, 3f)]
        [TestCase("Assets/Prefabs/Weapons/Axe_Weapon.prefab", 0f, 0.3f, 2f)]
        [TestCase("Assets/Prefabs/Weapons/Cross_Weapon.prefab", 0f, 0f, 0f)]
        [TestCase("Assets/Prefabs/Weapons/Generated/ThousandEdge.prefab", 0f, 0.3f, 3f)]
        [TestCase("Assets/Prefabs/Weapons/Generated/DeathSpiral.prefab", 0f, 0.3f, 2f)]
        [TestCase("Assets/Prefabs/Weapons/Generated/BloodyTear.prefab", 0.1f, 0f, 2f)]
        [TestCase("Assets/Prefabs/Weapons/Generated/HeavenSword.prefab", 0.1f, 0f, 2.5f)]
        public void EachWeapon_HasTheVsCritChanceAndMultiplier(string path, float chance, float slashChance, float multiplier)
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            var weapon = prefab.GetComponent<AutoAttackWeapon>();
            Assert.That(weapon, Is.Not.Null, path);
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            float Get(string name) => (float)typeof(AutoAttackWeapon).GetProperty(name, flags).GetValue(weapon);
            Assert.That(Get("BaseCritChance"), Is.EqualTo(chance).Within(0.0001f), "Chance without Slash");
            Assert.That(Get("SlashCritChance"), Is.EqualTo(slashChance).Within(0.0001f), "Chance with Slash");

            AutoAttackWeapon.CritFor(Get("BaseCritChance"), Get("WeaponCritMultiplier"), Get("SlashCritChance"), Get("SlashCritMultiplier"), false, out float c0, out float m0);
            AutoAttackWeapon.CritFor(Get("BaseCritChance"), Get("WeaponCritMultiplier"), Get("SlashCritChance"), Get("SlashCritMultiplier"), true, out float c1, out float m1);
            Assert.That(c0, Is.EqualTo(chance).Within(0.0001f));
            if (chance > 0f) Assert.That(m0, Is.EqualTo(multiplier).Within(0.0001f), "Multiplier without Slash");
            if (chance > 0f || slashChance > 0f) Assert.That(m1, Is.EqualTo(multiplier * 2f).Within(0.0001f), "Slash doubles the multiplier");
            else Assert.That(c1, Is.EqualTo(0f), "Never crits, even with Slash");
        }

        /// <summary>VS Limit Break (2026-10-01): each weapon offers only the stats it uses.</summary>
        [Test]
        public void RealWeapons_TellLimitBreakWhatTheyUse()
        {
            AutoAttackWeapon W(string path) => UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path).GetComponent<AutoAttackWeapon>();
            Assert.That(W("Assets/Prefabs/Weapons/Generated/Knife.prefab").UsesPierce, Is.True, "Knife: pierce");
            Assert.That(W("Assets/Prefabs/Weapons/Whip_Weapon.prefab").UsesPierce, Is.False, "Whip: no pierce");
            Assert.That(W("Assets/Prefabs/Weapons/Generated/CherryBomb.prefab").HasChance, Is.True, "Cherry Bomb: explode chance");
            Assert.That(W("Assets/Prefabs/Weapons/Generated/Pentagram.prefab").HasChance, Is.True, "Pentagram: keep chance");
            Assert.That(W("Assets/Prefabs/Weapons/Bible_Weapon.prefab").LimitBreakBaseDuration, Is.EqualTo(3f), "King Bible: 3 s");
            Assert.That(W("Assets/Prefabs/Weapons/Generated/BloodyTear.prefab").CanCrit, Is.True, "Bloody Tear crits");
            Assert.That(W("Assets/Prefabs/Weapons/Whip_Weapon.prefab").CanCrit, Is.False, "The Whip crits only with Slash");
        }

        [Test]
        public void CritsDoubleDamage()
        {
            Assert.That(AutoAttackWeapon.CritDamageMultiplier, Is.EqualTo(2f));
        }
    }
}
