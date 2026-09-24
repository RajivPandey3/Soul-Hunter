using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: LevelUpActionTests verifies the VS level-up actions: Reroll deals new choices,
    /// Skip gives up the level-up, and Banish removes an item from level-ups and chests for the rest of the run.
    /// Each action spends a charge and does nothing outside an open level-up.
    /// </summary>
    public class LevelUpActionTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        private GameObject _object;
        private WeaponManager _weapons;
        private LevelUpManager _manager;
        private readonly List<UpgradeData> _pool = new List<UpgradeData>();
        private List<UpgradeData> _lastRound;
        private int _rounds;

        [SetUp]
        public void SetUp()
        {
            _object = new GameObject("LevelUp");
            _weapons = _object.AddComponent<WeaponManager>();
            _manager = _object.AddComponent<LevelUpManager>();
            // EditMode does not run Awake; it loads the starting charges.
            typeof(LevelUpManager).GetMethod("Awake", Private).Invoke(_manager, null);
            typeof(LevelUpManager).GetField("_weaponManager", Private).SetValue(_manager, _weapons);
            _manager.OnLevelUpUIRound += choices => { _lastRound = choices; _rounds++; };
        }

        [TearDown]
        public void TearDown()
        {
            SoulHunter.Core.Services.GameTime.ResetAll();
            foreach (var upgrade in _pool) Object.DestroyImmediate(upgrade);
            _pool.Clear();
            Object.DestroyImmediate(_object);
        }

        private void UsePool(params UpgradeData.UpgradeType[] types)
        {
            foreach (var type in types)
            {
                var upgrade = ScriptableObject.CreateInstance<UpgradeData>();
                upgrade.Type = type;
                upgrade.Level = 1;
                upgrade.UpgradeName = type.ToString();
                _pool.Add(upgrade);
            }
            typeof(LevelUpManager).GetField("_allUpgrades", Private).SetValue(_manager, new List<UpgradeData>(_pool));
        }

        private void OpenLevelUp()
        {
            typeof(LevelUpManager).GetMethod("HandleLevelUp", Private).Invoke(_manager, new object[] { 2 });
        }

        [Test]
        public void Actions_DoNothing_WhenNoLevelUpIsOpen()
        {
            // Learning Comment: Level-up screen band ho toh koi charge kharch nahi hona chahiye.
            UsePool(UpgradeData.UpgradeType.MagicWand);
            int rerolls = _manager.RerollsLeft, skips = _manager.SkipsLeft, banishes = _manager.BanishesLeft;

            Assert.That(_manager.Reroll(), Is.False);
            Assert.That(_manager.Skip(), Is.False);
            Assert.That(_manager.Banish(_pool[0]), Is.False);
            Assert.That(_manager.RerollsLeft, Is.EqualTo(rerolls));
            Assert.That(_manager.SkipsLeft, Is.EqualTo(skips));
            Assert.That(_manager.BanishesLeft, Is.EqualTo(banishes));
        }

        [Test]
        public void Reroll_SpendsChargeAndDealsNewRound_UntilChargesRunOut()
        {
            UsePool(UpgradeData.UpgradeType.MagicWand, UpgradeData.UpgradeType.Whip,
                UpgradeData.UpgradeType.Garlic, UpgradeData.UpgradeType.Axe);
            OpenLevelUp();
            int startCharges = _manager.RerollsLeft;
            Assert.That(startCharges, Is.GreaterThan(0));

            for (int i = 0; i < startCharges; i++)
            {
                int roundsBefore = _rounds;
                Assert.That(_manager.Reroll(), Is.True);
                Assert.That(_rounds, Is.EqualTo(roundsBefore + 1), "Reroll naya round dikhaye.");
                Assert.That(_manager.IsLevelUpActive, Is.True, "Reroll ke baad bhi level-up khula rahe.");
            }

            Assert.That(_manager.RerollsLeft, Is.EqualTo(0));
            Assert.That(_manager.Reroll(), Is.False, "Charges khatam hone par reroll na ho.");
        }

        [Test]
        public void Skip_SpendsChargeAndClosesLevelUp_WithoutApplyingAnything()
        {
            UsePool(UpgradeData.UpgradeType.MagicWand, UpgradeData.UpgradeType.Whip);
            OpenLevelUp();
            int skips = _manager.SkipsLeft;

            Assert.That(_manager.Skip(), Is.True);

            Assert.That(_manager.SkipsLeft, Is.EqualTo(skips - 1));
            Assert.That(_manager.IsLevelUpActive, Is.False);
            Assert.That(_weapons.GetWeaponLevel(UpgradeData.UpgradeType.MagicWand), Is.EqualTo(0));
            Assert.That(_weapons.GetWeaponLevel(UpgradeData.UpgradeType.Whip), Is.EqualTo(0));
            Assert.That(Time.timeScale, Is.EqualTo(1f), "Skip ke baad game resume ho.");
        }

        [Test]
        public void Banish_RemovesItemFromLevelUpsAndChests_ForTheRestOfTheRun()
        {
            // Learning Comment: Banish kiya hua item na level-up mein aaye na chest se mile.
            UsePool(UpgradeData.UpgradeType.MagicWand, UpgradeData.UpgradeType.Whip);
            OpenLevelUp();
            UpgradeData banished = _lastRound[0];
            int banishes = _manager.BanishesLeft;

            Assert.That(_manager.Banish(banished), Is.True);

            Assert.That(_manager.BanishesLeft, Is.EqualTo(banishes - 1));
            Assert.That(_manager.IsBanished(banished.Type), Is.True);
            Assert.That(_manager.IsLevelUpActive, Is.True, "Banish ke baad naye choices dikhne chahiye.");
            Assert.That(_lastRound.Exists(u => u.Type == banished.Type), Is.False);
            Assert.That(_manager.GetRandomValidUpgradesForChest().Exists(u => u.Type == banished.Type), Is.False);
        }

        [Test]
        public void BanishingTheLastItem_PaysOutAndClosesLevelUp()
        {
            UsePool(UpgradeData.UpgradeType.MagicWand);
            OpenLevelUp();

            Assert.That(_manager.Banish(_lastRound[0]), Is.True);

            Assert.That(_manager.IsLevelUpActive, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }
    }
}
