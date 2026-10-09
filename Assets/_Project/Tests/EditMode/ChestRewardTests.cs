using System.Linq;
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

        /// <summary>
        /// VS (Treasure_Chest): chests only level up items the player has (Bug MIS B04), so the player owns the item first.
        /// </summary>
        private void Own(UpgradeData.UpgradeType type, int level)
        {
            var levels = (Dictionary<UpgradeData.UpgradeType, int>)typeof(WeaponManager).GetField("_weaponLevels", Private).GetValue(_weapons);
            levels[type] = level;
        }

        [Test]
        public void FirstSixChests_FollowTheVsSequence_ThenLuckRolls()
        {
            // VS wiki (Treasure_Chest): the first six chests on a save give 1-1-3-1-1-5 items, whatever the roll or Luck.
            // P1-14: it also overrides every boss's own tiers (the 1:00 chest would always give 1 item, the 5:00 one rarely 5).
            int[] expected = { 1, 1, 3, 1, 1, 5 };
            var bosses = new[] { ChestTiers.Minute1, ChestTiers.Minute3, ChestTiers.Minute5, ChestTiers.Minute9, ChestTiers.StageBoss };
            for (int opened = 0; opened < expected.Length; opened++)
                foreach (var tiers in bosses)
                {
                    Assert.That(ChestLogicController.ItemCountFor(opened, tiers, 1f, Rolls(0.99f, 0.99f, 0.99f)), Is.EqualTo(expected[opened]),
                        $"chest {opened + 1}, {tiers}, high rolls");
                    Assert.That(ChestLogicController.ItemCountFor(opened, tiers, 10f, Rolls(0f, 0f, 0f)), Is.EqualTo(expected[opened]),
                        $"chest {opened + 1}, {tiers}, lucky rolls");
                }
            Assert.That(ChestLogicController.FirstChestItems(6), Is.EqualTo(0), "The sequence ends after six chests.");
            Assert.That(ChestLogicController.ItemCountFor(6, ChestTiers.StageBoss, 1f, Rolls(0.02f)), Is.EqualTo(5), "The seventh chest rolls again.");
            Assert.That(ChestLogicController.ItemCountFor(6, ChestTiers.StageBoss, 1f, Rolls(0.99f, 0.99f, 0.99f)), Is.EqualTo(1));
        }

        /// <summary>A scripted random source: each check takes the next value (the last one repeats).</summary>
        private static System.Func<float> Rolls(params float[] values)
        {
            int next = 0;
            return () => values[Mathf.Min(next++, values.Length - 1)];
        }

        [Test]
        public void ChestTiers_MatchTheMadForestTable()
        {
            // vampire.survivors.wiki/w/Mad_Forest (raw, fetched 2026-10-09), each chest boss's {{treasure}} config:
            // (minute, level, evo, tier3, tier2, tier1, weapon slots, Arcana).
            var rows = new (float minute, int level, bool evo, float five, float three, float one, int weapons, bool arcana)[]
            {
                (1, 1, true, 0, 0, 30, 0, false),
                (3, 1, false, 0, 5, 40, ChestTiers.Slot(1), false),
                (5, 1, false, 1, 5, 100, ChestTiers.Slot(1), false),
                (7, 2, false, 3, 10, 50, ChestTiers.Slot(1), false),
                (9, 1, false, 3, 10, 50, ChestTiers.Slot(1) | ChestTiers.Slot(4) | ChestTiers.Slot(5), false),
                (10, 1, true, 3, 10, 100, ChestTiers.Slot(4) | ChestTiers.Slot(5), false),
                (11, 1, false, 0, 0, 100, 0, true),
                (12, 1, true, 1, 5, 50, 0, false),
                (14, 1, true, 3, 10, 100, 0, false),
                (15, 1, true, 3, 10, 100, 0, false),
                (16, 1, true, 1, 5, 50, 0, false),
                (18, 1, true, 3, 10, 100, 0, false),
                (20, 1, true, 3, 10, 100, 0, false),
                (21, 1, false, 0, 0, 100, 0, true),
                (22, 1, true, 3, 10, 100, 0, false),
                (23, 1, true, 3, 10, 100, 0, false),
                (25, 1, true, 3, 10, 100, 0, false), // Giant Blue Venus, the stage boss
                (27, 1, true, 3, 10, 100, 0, false),
                (29, 1, true, 3, 10, 100, 0, false),
            };
            foreach (var row in rows)
            {
                Assert.That(ChestTiers.TryMadForest(row.minute, out var tiers), Is.True, $"{row.minute}:00 has a config.");
                var expected = new ChestTiers(row.level, row.evo, row.five, row.three, row.one, row.weapons, row.arcana);
                Assert.That(tiers, Is.EqualTo(expected), $"{row.minute}:00 is {tiers}, the wiki row is {expected}.");
            }
            Assert.That(ChestTiers.StageBoss, Is.EqualTo(ChestTiers.ForMinute(25f)), "The 25:00 stage boss uses the Giant Blue Venus config.");

            // Every minute Soul Hunter schedules a chest carrier at has its own row.
            foreach (float minute in SoulHunter.Gameplay.AI.EnemySpawner.DefaultChestEliteMinutes.Concat(SoulHunter.Gameplay.AI.EnemySpawner.ArcanaChestMinutes))
                Assert.That(ChestTiers.TryMadForest(minute, out _), Is.True, $"The {minute}:00 chest elite has a Mad Forest config.");
            Assert.That(ChestTiers.TryMadForest(2f, out _), Is.False, "No boss at 2:00.");
            Assert.That(ChestTiers.ForMinute(2f).CanEvolve, Is.False, "Off-table chests fall back to the clock rule.");
            Assert.That(ChestTiers.ForMinute(10.5f).CanEvolve, Is.True);
        }

        [Test]
        public void RollItemCount_UsesSeparateRolls_FiveThenThreeThenOne_ThenFallback()
        {
            // vampire.survivors.wiki/w/Treasure_Chest: 5-item check, then 3, then 1, each with its own random number and
            // chanceWithLuck = levelChance x totalLuck; all three failing gives the boss's fallback level.
            var silverBat = ChestTiers.Minute9; // 3 / 10 / 50, level 1
            Assert.That(ChestLogicController.RollItemCount(silverBat, 1f, Rolls(0.02f)), Is.EqualTo(5));
            Assert.That(ChestLogicController.RollItemCount(silverBat, 1f, Rolls(0.5f, 0.05f)), Is.EqualTo(3),
                "The 3-item check uses a new number (0.05 < 10%), not the one that failed the 5-item check.");
            Assert.That(ChestLogicController.RollItemCount(silverBat, 1f, Rolls(0.5f, 0.5f, 0.2f)), Is.EqualTo(1));
            Assert.That(ChestLogicController.RollItemCount(silverBat, 1f, Rolls(0.5f, 0.5f, 0.9f)), Is.EqualTo(silverBat.FallbackItems),
                "All three checks failed: the level 1 fallback (1 item).");
            Assert.That(ChestLogicController.RollItemCount(silverBat, 1f, Rolls(0.05f, 0.5f)), Is.Not.EqualTo(5), "0.05 fails the 3% check at Luck 1...");
            Assert.That(ChestLogicController.RollItemCount(silverBat, 2f, Rolls(0.05f)), Is.EqualTo(5), "...and passes it at Luck 2 (6%).");

            // The 1:00 Glowing Bat (0 / 0 / 30, level 1) always gives 1 item, at any Luck.
            foreach (float roll in new[] { 0f, 0.29f, 0.5f, 0.99f })
                Assert.That(ChestLogicController.RollItemCount(ChestTiers.Minute1, 5f, Rolls(roll)), Is.EqualTo(1), $"1:00 chest, roll {roll}");
            // The 3:00 Glowing Bat (0 / 5 / 40) never gives 5.
            Assert.That(ChestLogicController.RollItemCount(ChestTiers.Minute3, 10f, Rolls(0f, 0.99f, 0.99f)), Is.EqualTo(1));
            Assert.That(ChestLogicController.RollItemCount(ChestTiers.Minute3, 10f, Rolls(0f, 0f)), Is.EqualTo(3));
            // The 7:00 Glowing Bat's level=2: Treasure_Chest's v1.6.0 notes call the 3-item screen "lvl 2", so all rolls
            // failing gives 3 items.
            Assert.That(ChestLogicController.RollItemCount(ChestTiers.Minute7, 1f, Rolls(0.99f, 0.99f, 0.99f)), Is.EqualTo(3));
            Assert.That(ChestTiers.Minute7.FallbackItems, Is.EqualTo(3));
            Assert.That(ChestTiers.Minute9.FallbackItems, Is.EqualTo(1));
        }

        [Test]
        public void SeparateRolls_GiveTheVsOdds()
        {
            // Separate checks: P(5) = 3%, P(3) = (1 - 0.03) x 10% = 9.7% (one shared number would give 10%).
            var random = new System.Random(1234);
            System.Func<float> next = () => (float)random.NextDouble();
            const int chests = 100000;
            int five = 0, three = 0;
            for (int i = 0; i < chests; i++)
            {
                int items = ChestLogicController.RollItemCount(ChestTiers.Minute9, 1f, next);
                if (items == 5) five++;
                else if (items == 3) three++;
            }
            Assert.That(five / (float)chests, Is.EqualTo(0.03f).Within(0.003f));
            Assert.That(three / (float)chests, Is.EqualTo(0.097f).Within(0.004f));
        }

        [Test]
        public void WeaponSlots_LevelAnOwnedWeapon_AndPayCoinsWithNoWeaponLeft()
        {
            // vampire.survivors.wiki/w/Template:Treasure: a "rewardN=weapon" slot is "Upgrade (weapon)". The 9:00 Silver Bat
            // has weapon slots 1, 4 and 5, so a 5-item chest levels the Whip at least three times.
            UsePool(UpgradeData.UpgradeType.Whip, 8);
            UsePool(UpgradeData.UpgradeType.Spinach, 5);
            Own(UpgradeData.UpgradeType.Whip, 1);
            Own(UpgradeData.UpgradeType.Spinach, 1);

            _chest.ProcessChest(_weapons, _levelUps, 5, ChestTiers.Minute9, 0f);

            int whip = _weapons.GetWeaponLevel(UpgradeData.UpgradeType.Whip);
            int spinach = _weapons.GetWeaponLevel(UpgradeData.UpgradeType.Spinach);
            Assert.That(whip, Is.GreaterThanOrEqualTo(4), "Slots 1, 4 and 5 are weapon upgrades.");
            Assert.That(whip - 1 + spinach - 1, Is.EqualTo(5), "Five items, five levels.");
            Assert.That(_chest.LastResult.Kind, Is.EqualTo(SoulHunter.Gameplay.Pickups.ChestKind.Bronze), "9:00 has evo= blank: bronze.");

            // No weapon to level: Patch 0.2.7, "or give extra coins if no weapons can be leveled up". The slot pays a Big
            // Coin Bag and the passive is not levelled.
            Own(UpgradeData.UpgradeType.Whip, 0);
            _chest.ProcessChest(_weapons, _levelUps, 1, ChestTiers.Minute9, 0f);
            Assert.That(_weapons.GetWeaponLevel(UpgradeData.UpgradeType.Spinach), Is.EqualTo(spinach));
            Assert.That(_chest.LastResult.Rewards.Exists(r => r.IsCoinBag), Is.True, "The weapon slot paid a coin bag.");
        }

        [Test]
        public void ThreeItemChest_AppliesThreeUpgrades()
        {
            UsePool(UpgradeData.UpgradeType.Spinach, 8);
            Own(UpgradeData.UpgradeType.Spinach, 1);

            string message = _chest.ProcessChest(_weapons, _levelUps, 3);

            Assert.That(_weapons.GetWeaponLevel(UpgradeData.UpgradeType.Spinach), Is.EqualTo(4), "Three levels on top of the owned level 1.");
            Assert.That(message, Does.Contain("x3"));
        }

        [Test]
        public void FiveItemChest_StopsWhenNothingIsLeft()
        {
            // Learning Comment: Sirf 2 upgrades bache hon toh 5-item chest 2 hi de, crash ya loop na kare.
            UsePool(UpgradeData.UpgradeType.Spinach, 2);
            Own(UpgradeData.UpgradeType.Spinach, 1);

            _chest.ProcessChest(_weapons, _levelUps, 5);

            Assert.That(_weapons.GetWeaponLevel(UpgradeData.UpgradeType.Spinach), Is.EqualTo(2));
        }

        [Test]
        public void EveryChest_HoldsVsGold_ByItsSize()
        {
            // VS wiki (Treasure_Chest): 1 item 100-200, 3 items 300-600, 5 items 500-1000 (then Greed).
            Assert.That(ChestLogicController.ChestGold(1, 0f), Is.EqualTo(100));
            Assert.That(ChestLogicController.ChestGold(1, 1f), Is.EqualTo(200));
            Assert.That(ChestLogicController.ChestGold(3, 0.5f), Is.EqualTo(450));
            Assert.That(ChestLogicController.ChestGold(5, 1f), Is.EqualTo(1000));

            UsePool(UpgradeData.UpgradeType.Spinach, 8);
            Own(UpgradeData.UpgradeType.Spinach, 1);
            string message = _chest.ProcessChest(_weapons, _levelUps, 1, true, 0f);
            Assert.That(message, Does.Contain("Gold: +100"), "A chest with an upgrade still pays its gold.");
        }

        [Test]
        public void MaxedItems_PayABigCoinBagEach()
        {
            // VS Big Coin Bag: 25 gold for each item with nothing left to upgrade, on top of the chest gold.
            UsePool(UpgradeData.UpgradeType.Spinach, 2);
            Own(UpgradeData.UpgradeType.Spinach, 1);
            string message = _chest.ProcessChest(_weapons, _levelUps, 5, true, 0f);
            Assert.That(_weapons.GetWeaponLevel(UpgradeData.UpgradeType.Spinach), Is.EqualTo(2));
            Assert.That(message, Does.Contain("Gold: +600"), "500 chest gold + 4 maxed items x 25 (one level was left).");
        }

        [Test]
        public void Chest_RecordsWhatItGave_ForTheOpeningScreen()
        {
            UsePool(UpgradeData.UpgradeType.Spinach, 3);
            Own(UpgradeData.UpgradeType.Spinach, 1);
            _chest.ProcessChest(_weapons, _levelUps, 5, true, 0f);
            var result = _chest.LastResult;
            Assert.That(result.ItemCount, Is.EqualTo(5));
            Assert.That(result.Rewards.Count, Is.EqualTo(5), "One entry per item.");
            Assert.That(result.Rewards.Take(2).All(r => !r.IsCoinBag && r.Level > 0), Is.True, "Two upgrades.");
            Assert.That(result.Rewards.Skip(2).All(r => r.IsCoinBag), Is.True, "Three coin bags.");
            Assert.That(result.OnlyCoins, Is.False);
            Assert.That(result.Gold, Is.EqualTo(575));
        }

        [TestCase(0, 0, 1, false)]   // the first six chests cannot be skipped
        [TestCase(5, 0, 3, false)]
        [TestCase(6, 0, 1, true)]    // from the seventh, 1- and 3-item chests can
        [TestCase(6, 0, 3, true)]
        [TestCase(6, 0, 5, false)]   // 5-item chests need 50 of them ...
        [TestCase(40, 50, 5, true)]
        [TestCase(500, 0, 5, true)]  // ... or 500 chests
        public void VsSkipRules(int opened, int fiveOpened, int items, bool canSkip)
        {
            Assert.That(SoulHunter.Gameplay.UI.ChestSkipRules.CanSkip(opened, fiveOpened, items), Is.EqualTo(canSkip));
        }

        [Test]
        public void CoinOnlyChests_SkipThemselves_OnceFiveItemChestsCanBeSkipped()
        {
            Assert.That(SoulHunter.Gameplay.UI.ChestSkipRules.SkipsItself(10, 0, onlyCoins: true), Is.False);
            Assert.That(SoulHunter.Gameplay.UI.ChestSkipRules.SkipsItself(10, 50, onlyCoins: true), Is.True);
            Assert.That(SoulHunter.Gameplay.UI.ChestSkipRules.SkipsItself(10, 50, onlyCoins: false), Is.False);
        }

        [Test]
        public void ChestAndLevelUpEffects_RunWhileThePauseStarts()
        {
            Assert.That(SoulHunter.Gameplay.VFX.VFXPoolManager.PlaysWhilePaused(SoulHunter.Gameplay.VFX.VfxKind.ChestOpen), Is.True);
            Assert.That(SoulHunter.Gameplay.VFX.VFXPoolManager.PlaysWhilePaused(SoulHunter.Gameplay.VFX.VfxKind.LevelUp), Is.True);
            Assert.That(SoulHunter.Gameplay.VFX.VFXPoolManager.PlaysWhilePaused(SoulHunter.Gameplay.VFX.VfxKind.HitSpark), Is.False,
                "Combat effects still freeze with the game.");
        }

        [TestCase(30f, false)]
        [TestCase(60f, true)]
        [TestCase(119f, true)]
        [TestCase(180f, false)]
        [TestCase(540f, false)]
        [TestCase(600f, true)]
        [TestCase(1740f, true)]
        public void OnlyChestsFromTenMinutes_AndTheFirstChest_CanEvolve(float stageSeconds, bool canEvolve)
        {
            // VS Mad Forest wave table: the 1:00 chest and every chest from 10:00 on can evolve; 3, 5, 7 and 9 cannot.
            Assert.That(ChestLogicController.CanEvolveAt(stageSeconds), Is.EqualTo(canEvolve));
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
