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
            // VS: a run starts with no Reroll/Skip/Banish; the PowerUps give 2 each per rank (rank 1 here).
            _manager.AddCharges(2, 2, 2);
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

        /// <summary>
        /// Bug MIS B04 (2026-09-30): chests handed out new items ("Knife NEW!"). vampire.survivors.wiki/w/Treasure_Chest:
        /// chests contain "a random weapon or passive item the player currently has, leveling up the item by one".
        /// </summary>
        [Test]
        public void Chests_OnlyLevelUpOwnedItems()
        {
            UsePool(UpgradeData.UpgradeType.MagicWand, UpgradeData.UpgradeType.Whip);
            Assert.That(_manager.GetRandomValidUpgradesForChest(), Is.Empty, "Nothing owned: the chest gives no item (it pays gold).");

            var whip2 = ScriptableObject.CreateInstance<UpgradeData>();
            whip2.Type = UpgradeData.UpgradeType.Whip;
            whip2.Level = 2;
            _pool.Add(whip2);
            typeof(LevelUpManager).GetField("_allUpgrades", Private).SetValue(_manager, new List<UpgradeData>(_pool));
            var levels = (Dictionary<UpgradeData.UpgradeType, int>)typeof(WeaponManager).GetField("_weaponLevels", Private).GetValue(_weapons);
            levels[UpgradeData.UpgradeType.Whip] = 1;

            for (int i = 0; i < 30; i++)
            {
                var drawn = _manager.GetRandomValidUpgradesForChest();
                Assert.That(drawn.Count, Is.EqualTo(1));
                Assert.That(drawn[0].Type, Is.EqualTo(UpgradeData.UpgradeType.Whip), "Only the owned Whip, never a new Magic Wand.");
                Assert.That(drawn[0].Level, Is.EqualTo(2), "Levelled by one.");
            }
        }

        [Test]
        public void BanishingTheLastItem_OffersGoldOrChicken_ThenCloses()
        {
            // VS wiki (Level_up): with nothing left to upgrade, the level-up offers gold or a Floor Chicken to pick.
            UsePool(UpgradeData.UpgradeType.MagicWand);
            OpenLevelUp();

            Assert.That(_manager.Banish(_lastRound[0]), Is.True);

            Assert.That(_manager.IsLevelUpActive, Is.True, "The screen stays open with the two reward cards.");
            Assert.That(_lastRound.Count, Is.EqualTo(2));
            Assert.That(_lastRound.TrueForAll(MaxedRewards.IsCard), Is.True);

            _manager.SelectUpgrade(_lastRound[0]);
            Assert.That(_manager.IsLevelUpActive, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void Banish_IsUnavailable_OnTheGoldPouchAndRoastChickenCards()
        {
            // Bug MIS CORE-09 (2026-10-19): Banish on the maxed-out cards spent a charge and did nothing. VS Banish removes
            // "an item choice"; gold and a chicken are not items (wiki silent: balance method, like Skip and Reroll).
            UsePool(UpgradeData.UpgradeType.MagicWand);
            OpenLevelUp();
            Assert.That(_manager.Banish(_lastRound[0]), Is.True, "Banishing the last item deals the reward cards.");
            Assert.That(_lastRound.TrueForAll(MaxedRewards.IsCard), Is.True);
            int banishes = _manager.BanishesLeft;
            int rounds = _rounds;

            Assert.That(_manager.CanBanish, Is.False, "No Banish on the maxed-out cards.");
            Assert.That(_manager.Banish(_lastRound[0]), Is.False);
            Assert.That(_manager.BanishesLeft, Is.EqualTo(banishes), "No charge is spent.");
            Assert.That(_rounds, Is.EqualTo(rounds), "The same cards stay.");
            Assert.That(_manager.IsLevelUpActive, Is.True);
        }
    }
}
