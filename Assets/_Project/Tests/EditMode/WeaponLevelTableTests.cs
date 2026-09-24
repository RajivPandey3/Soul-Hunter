using NUnit.Framework;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Weapons;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: WeaponLevelTableTests verifies that Whip, Garlic, Axe, Bible and Cross level from 1 to 8
    /// through WeaponManager's AutoAttackWeapon path, gain their table bonuses, and stop at level 8.
    /// </summary>
    public class WeaponLevelTableTests
    {
        private GameObject _object;

        [TearDown]
        public void TearDown()
        {
            if (_object != null) Object.DestroyImmediate(_object);
        }

        private T CreateAtMaxLevel<T>(out float levelOneDamage) where T : AutoAttackWeapon
        {
            _object = new GameObject(typeof(T).Name);
            var weapon = _object.AddComponent<T>();
            levelOneDamage = weapon.DamageAmount;
            for (int i = 0; i < 10; i++) weapon.LevelUp(); // two extra calls must be ignored
            return weapon;
        }

        [Test]
        public void Whip_LevelsToEight_AddsSecondWhipDamageAndArea()
        {
            var whip = CreateAtMaxLevel<WhipWeapon>(out float baseDamage);
            Assert.That(whip.MaxLevel, Is.EqualTo(8));
            Assert.That(whip.CurrentLevel, Is.EqualTo(8));
            Assert.That(whip.LevelWhipCount, Is.EqualTo(2));
            Assert.That(whip.DamageAmount, Is.EqualTo(baseDamage + 30f));
            Assert.That(whip.LevelAreaMultiplier, Is.EqualTo(1.2f).Within(0.001f));
        }

        [Test]
        public void Garlic_LevelsToEight_GrowsAreaDamageAndSpeed()
        {
            _object = new GameObject("Garlic");
            var garlic = _object.AddComponent<GarlicWeapon>();
            float baseDamage = garlic.DamageAmount;
            float baseCooldown = garlic.AttackCooldown;
            for (int i = 0; i < 10; i++) garlic.LevelUp();

            Assert.That(garlic.MaxLevel, Is.EqualTo(8));
            Assert.That(garlic.CurrentLevel, Is.EqualTo(8));
            Assert.That(garlic.DamageAmount, Is.EqualTo(baseDamage + 9f));
            Assert.That(garlic.LevelAreaMultiplier, Is.EqualTo(2f).Within(0.001f));
            Assert.That(garlic.AttackCooldown, Is.EqualTo(baseCooldown * 0.729f).Within(0.001f));
        }

        [Test]
        public void Axe_LevelsToEight_AddsTwoAxesDamageAndArea()
        {
            var axe = CreateAtMaxLevel<AxeWeapon>(out float baseDamage);
            Assert.That(axe.CurrentLevel, Is.EqualTo(8));
            Assert.That(axe.LevelAxeCount, Is.EqualTo(3));
            Assert.That(axe.DamageAmount, Is.EqualTo(baseDamage + 45f));
            Assert.That(axe.LevelAreaMultiplier, Is.EqualTo(1.4f).Within(0.001f));
        }

        [Test]
        public void Bible_LevelsToEight_AddsThreeBooksDamageAreaAndSpeed()
        {
            var bible = CreateAtMaxLevel<BibleWeapon>(out float baseDamage);
            Assert.That(bible.CurrentLevel, Is.EqualTo(8));
            Assert.That(bible.LevelBookCount, Is.EqualTo(3));
            Assert.That(bible.DamageAmount, Is.EqualTo(baseDamage + 12f));
            Assert.That(bible.LevelAreaMultiplier, Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(bible.LevelSpeedMultiplier, Is.EqualTo(1.6f).Within(0.001f));
        }

        [Test]
        public void Cross_LevelsToEight_AddsTwoCrossesDamageAreaAndSpeed()
        {
            var cross = CreateAtMaxLevel<CrossWeapon>(out float baseDamage);
            Assert.That(cross.CurrentLevel, Is.EqualTo(8));
            Assert.That(cross.LevelCrossCount, Is.EqualTo(3));
            Assert.That(cross.DamageAmount, Is.EqualTo(baseDamage + 30f));
            Assert.That(cross.LevelAreaMultiplier, Is.EqualTo(1.2f).Within(0.001f));
            Assert.That(cross.LevelSpeedMultiplier, Is.EqualTo(1.5f).Within(0.001f));
        }
    }
}
