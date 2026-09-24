using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: ChestRewardTests verifies VS-style chests: 1, 3 or 5 items with Luck raising the
    /// 3/5 chances, one reward per item, and opening a chest never touching the level-up queue or pause state.
    /// </summary>
    public class ChestRewardTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        private GameObject _object;
        private WeaponManager _weapons;
        private LevelUpManager _levelUps;
        private ChestLogicController _chest;
        private readonly List<UpgradeData> _pool = new List<UpgradeData>();

        [SetUp]
        public void SetUp()
        {
            _object = new GameObject("Chest");
            _weapons = _object.AddComponent<WeaponManager>();
            _levelUps = _object.AddComponent<LevelUpManager>();
            _chest = _object.AddComponent<ChestLogicController>();
            typeof(LevelUpManager).GetField("_weaponManager", Private).SetValue(_levelUps, _weapons);
        }

        [TearDown]
        public void TearDown()
        {
            SoulHunter.Core.Services.GameTime.ResetAll();
            foreach (var upgrade in _pool) Object.DestroyImmediate(upgrade);
            _pool.Clear();
            Object.DestroyImmediate(_object);
        }

        private void UsePool(UpgradeData.UpgradeType type, int maxLevel)
        {
            for (int level = 1; level <= maxLevel; level++)
            {
                var upgrade = ScriptableObject.CreateInstance<UpgradeData>();
                upgrade.Type = type;
                upgrade.Level = level;
                upgrade.UpgradeName = type.ToString();
                _pool.Add(upgrade);
            }
            typeof(LevelUpManager).GetField("_allUpgrades", Private).SetValue(_levelUps, new List<UpgradeData>(_pool));
        }

        [Test]
        public void RollItemCount_UsesThresholds_AndLuckRaisesThem()
        {
            // Learning Comment: Luck 1 par 3% (5 items) aur 10% (3 items); Luck 2 par dono double.
            Assert.That(_chest.RollItemCount(1f, 0.02f), Is.EqualTo(5));
            Assert.That(_chest.RollItemCount(1f, 0.10f), Is.EqualTo(3));
            Assert.That(_chest.RollItemCount(1f, 0.20f), Is.EqualTo(1));

            Assert.That(_chest.RollItemCount(2f, 0.05f), Is.EqualTo(5), "Luck 2 par 5-item chance 6% ho.");
            Assert.That(_chest.RollItemCount(2f, 0.20f), Is.EqualTo(3), "Luck 2 par 3-item chance 20% tak ho.");
            Assert.That(_chest.RollItemCount(2f, 0.30f), Is.EqualTo(1));
        }

        [Test]
        public void ThreeItemChest_AppliesThreeUpgrades()
        {
            UsePool(UpgradeData.UpgradeType.Spinach, 8);

            string message = _chest.ProcessChest(_weapons, _levelUps, 3);

            Assert.That(_weapons.GetWeaponLevel(UpgradeData.UpgradeType.Spinach), Is.EqualTo(3));
            Assert.That(message, Does.Contain("x3"));
        }

        [Test]
        public void FiveItemChest_StopsWhenNothingIsLeft()
        {
            // Learning Comment: Sirf 2 upgrades bache hon toh 5-item chest 2 hi de, crash ya loop na kare.
            UsePool(UpgradeData.UpgradeType.Spinach, 2);

            _chest.ProcessChest(_weapons, _levelUps, 5);

            Assert.That(_weapons.GetWeaponLevel(UpgradeData.UpgradeType.Spinach), Is.EqualTo(2));
        }

        [Test]
        public void OpeningChest_DoesNotTouchLevelUpQueueOrPauseState()
        {
            // Learning Comment: Chest ko level-up queue ya Time.timeScale nahi chhedna chahiye; pause ChestUI sambhalta hai.
            UsePool(UpgradeData.UpgradeType.Spinach, 8);
            Time.timeScale = 0f;

            _chest.ProcessChest(_weapons, _levelUps, 3);

            Assert.That(Time.timeScale, Is.EqualTo(0f));
            Assert.That(_levelUps.IsLevelUpActive, Is.False);
        }
    }
}
