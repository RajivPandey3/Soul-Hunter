using System.Collections.Generic;
using NUnit.Framework;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Player;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: LevelUpCurveTests verifies the VS XP curve (+10 / +13 / +16 gems per level with the
    /// level 20 and 40 jumps) and that level-up choices are weighted by rarity.
    /// </summary>
    public class LevelUpCurveTests
    {
        private GameObject _object;
        private readonly List<UpgradeData> _upgrades = new List<UpgradeData>();

        [TearDown]
        public void TearDown()
        {
            foreach (var upgrade in _upgrades) Object.DestroyImmediate(upgrade);
            _upgrades.Clear();
            if (_object != null) Object.DestroyImmediate(_object);
        }

        [TestCase(1, 5)]
        [TestCase(2, 15)]
        [TestCase(19, 185)]
        [TestCase(20, 795)]   // 195 + 600 jump
        [TestCase(21, 208)]   // +13 per level after 20
        [TestCase(39, 442)]
        [TestCase(40, 2855)]  // 455 + 2400 jump
        [TestCase(41, 471)]   // +16 per level after 40
        public void GemsToNextLevel_FollowsVsCurve(int level, int gems)
        {
            Assert.That(PlayerExperience.GemsToNextLevel(level), Is.EqualTo(gems));
        }

        [Test]
        public void AddXP_LevelsUpOnTheCurve_ScaledByGemValue()
        {
            // Learning Comment: Soul Hunter gem 10 XP ka hai, isliye level 2 ke liye 5 gems = 50 XP.
            _object = new GameObject("Player");
            var experience = _object.AddComponent<PlayerExperience>();
            int levelUps = 0;
            experience.OnLevelUp += _ => levelUps++;

            Assert.That(experience.XPToNextLevel, Is.EqualTo(50));
            experience.AddXP(50);

            Assert.That(experience.CurrentLevel, Is.EqualTo(2));
            Assert.That(levelUps, Is.EqualTo(1));
            Assert.That(experience.XPToNextLevel, Is.EqualTo(150));
        }

        [Test]
        public void AddXP_LargePickup_QueuesSeveralLevelUps()
        {
            _object = new GameObject("Player");
            var experience = _object.AddComponent<PlayerExperience>();

            experience.AddXP(50 + 150 + 250); // exactly levels 1 -> 4

            Assert.That(experience.CurrentLevel, Is.EqualTo(4));
            Assert.That(experience.CurrentXP, Is.EqualTo(0));
        }

        [TestCase(1.0f, 0.0f, 3)]    // Luck 1: never a 4th choice
        [TestCase(0.5f, 0.0f, 3)]    // Luck below 1 never removes a choice
        [TestCase(1.1f, 0.09f, 4)]   // 1 - 1/1.1 = ~9.1%
        [TestCase(1.1f, 0.10f, 3)]
        [TestCase(2.0f, 0.49f, 4)]   // 1 - 1/2 = 50%
        [TestCase(2.0f, 0.50f, 3)]
        public void ChoiceCountForLuck_GivesFourthChoiceAtOneMinusInverseLuck(float luck, float roll, int expected)
        {
            // Learning Comment: VS rule: 4th choice ka chance 1 - 1/Luck hai.
            Assert.That(LevelUpManager.ChoiceCountForLuck(luck, roll), Is.EqualTo(expected));
        }

        private UpgradeData Upgrade(UpgradeData.UpgradeType type)
        {
            var upgrade = ScriptableObject.CreateInstance<UpgradeData>();
            upgrade.Type = type;
            upgrade.Level = 1;
            _upgrades.Add(upgrade);
            return upgrade;
        }

        [Test]
        public void PickWeightedIndex_UsesRarityWeights()
        {
            // Learning Comment: Tiragisu (30) aur Magic Wand (100): total 130, roll ka pehla 30/130 Tiragisu ko jaye.
            var choices = new List<UpgradeData>
            {
                Upgrade(UpgradeData.UpgradeType.Tiragisu),
                Upgrade(UpgradeData.UpgradeType.MagicWand)
            };

            Assert.That(UpgradeRarity.GetWeight(UpgradeData.UpgradeType.Tiragisu), Is.LessThan(UpgradeRarity.DefaultWeight));
            Assert.That(LevelUpManager.PickWeightedIndex(choices, 0.20f), Is.EqualTo(0));
            Assert.That(LevelUpManager.PickWeightedIndex(choices, 0.25f), Is.EqualTo(1));
            Assert.That(LevelUpManager.PickWeightedIndex(choices, 0.99f), Is.EqualTo(1));
        }
    }
}
