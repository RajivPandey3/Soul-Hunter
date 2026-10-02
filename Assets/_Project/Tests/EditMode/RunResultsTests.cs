using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Pickups;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>VS Results rules (vampire.survivors.wiki/w/Results, fetched 2026-10-02).</summary>
    public class RunResultsTests
    {
        [TestCase(0d, "0")]
        [TestCase(999d, "999")]
        [TestCase(1234d, "1.23k")]
        [TestCase(45678d, "45.6k")]
        [TestCase(999999d, "999k")]
        [TestCase(2500000d, "2.50M")]
        [TestCase(3.2e9, "3.20G")]
        [TestCase(1e12, "1.00T")]
        [TestCase(7e15, "7.00P")]
        [TestCase(1e18, "1.00E")]
        [TestCase(5e21, "5000E")]
        public void Damage_UsesSiPrefixes_UpToExa(double amount, string expected)
        {
            Assert.AreEqual(expected, RunResultsFormat.Damage(amount));
        }

        [Test]
        public void Dps_IsDamagePerSecondOfTimeUsed()
        {
            Assert.AreEqual(50d, RunResultsFormat.Dps(500d, 10f), 1e-9);
            Assert.AreEqual(0d, RunResultsFormat.Dps(500d, 0f), "No time held: no DPS.");
        }

        [Test]
        public void Time_IsMinutesSeconds_AndHoursForLongRuns()
        {
            Assert.AreEqual("00:00", RunResultsFormat.Time(0f));
            Assert.AreEqual("30:00", RunResultsFormat.Time(1800f));
            Assert.AreEqual("4:59:07", RunResultsFormat.Time(4 * 3600 + 59 * 60 + 7.9f));
        }

        [Test]
        public void TheYellowRows_AreTheMostDamage_AndTheMostDps()
        {
            var results = new RunResults();
            results.AddWeapons(new List<RunStatsTracker.WeaponRecord>
            {
                new RunStatsTracker.WeaponRecord { Name = "A", Damage = 5000, SecondsUsed = 100f, Level = 8, MaxLevel = 8 },
                new RunStatsTracker.WeaponRecord { Name = "B", Damage = 300, SecondsUsed = 1f, Level = 2, MaxLevel = 8 },
                new RunStatsTracker.WeaponRecord { Name = "C", Damage = 0, SecondsUsed = 5f, Level = 1, MaxLevel = 0, Evolved = true }
            });
            Assert.AreEqual(0, results.TopDamageRow);
            Assert.AreEqual(1, results.TopDpsRow);
            Assert.IsTrue(results.Weapons[0].Maxed, "Level 8 of 8 is max.");
            Assert.IsFalse(results.Weapons[1].Maxed);
            Assert.IsTrue(results.Weapons[2].Maxed, "An evolution cannot level further.");

            var empty = new RunResults();
            empty.AddWeapons(new List<RunStatsTracker.WeaponRecord> { new RunStatsTracker.WeaponRecord { Name = "Laurel" } });
            Assert.AreEqual(-1, empty.TopDamageRow, "No damage at all: nothing yellow.");
        }

        [Test]
        public void Tracker_CountsPickups_NamedSources_AndUnlocksOnce()
        {
            var go = new GameObject(nameof(RunResultsTests));
            go.SetActive(false); // no singleton Awake
            var tracker = go.AddComponent<RunStatsTracker>();
            try
            {
                tracker.RecordPickup(PickupNames.Coin);
                tracker.RecordPickup(PickupNames.Coin);
                tracker.RecordPickup(PickupNames.Chicken);
                Assert.AreEqual(2, tracker.PickupCount(PickupNames.Coin));
                Assert.AreEqual(1, tracker.PickupCount(PickupNames.Chicken));
                Assert.AreEqual(PickupNames.Coin, tracker.Pickups[0].Key, "Pickups keep the order first collected.");

                tracker.RecordHit(null, "Soul Burst", 40);
                tracker.RecordHit(null, "Soul Burst", 2);
                tracker.RecordHit(null, null, 99); // no source: not credited
                Assert.AreEqual(1, tracker.Weapons.Count);
                Assert.AreEqual(42d, tracker.Weapons[0].Damage, 1e-9);
                Assert.AreEqual(42, tracker.GetWeaponStats()["Soul Burst"]);

                tracker.RecordUnlock("Character: Corvan");
                tracker.RecordUnlock("Character: Corvan");
                Assert.AreEqual(1, tracker.Unlocks.Count);

                tracker.ResetStats();
                Assert.AreEqual(0, tracker.Weapons.Count + tracker.Pickups.Count + tracker.Unlocks.Count, "A new run starts empty.");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void PickupNames_FollowTheCoinValues_AndChestSizes()
        {
            Assert.AreEqual(PickupNames.Coin, PickupNames.ForGold(1));
            Assert.AreEqual(PickupNames.CoinBag, PickupNames.ForGold(10));
            Assert.AreEqual(PickupNames.RichCoinBag, PickupNames.ForGold(100));
            Assert.AreEqual("Soul Reliquary x5", PickupNames.ForChest(5, false));
            Assert.AreEqual("Soul Reliquary x3", PickupNames.ForChest(3, false));
            Assert.AreEqual("Soul Reliquary", PickupNames.ForChest(1, false));
            Assert.AreEqual("Arcana Reliquary", PickupNames.ForChest(1, true));
        }
    }
}
