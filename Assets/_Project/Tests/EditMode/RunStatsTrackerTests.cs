using System.Collections.Generic;
using NUnit.Framework;
using SoulHunter.Gameplay.Core;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    public class RunStatsTrackerTests
    {
        private GameObject _gameObject;
        private RunStatsTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject(nameof(RunStatsTrackerTests));
            // Test the counters without invoking singleton ownership in Awake.
            _gameObject.SetActive(false);
            _tracker = _gameObject.AddComponent<RunStatsTracker>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void AddKill_IncrementsTotalKillsAndNotifiesWithEachUpdatedTotal()
        {
            var receivedTotals = new List<int>();
            _tracker.OnKillsChanged += receivedTotals.Add;

            Assert.That(_tracker.TotalKills, Is.Zero);

            _tracker.AddKill();

            Assert.That(_tracker.TotalKills, Is.EqualTo(1));
            Assert.That(receivedTotals, Is.EqualTo(new[] { 1 }));

            _tracker.AddKill();

            Assert.That(_tracker.TotalKills, Is.EqualTo(2));
            Assert.That(receivedTotals, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void AddGold_AccumulatesTotalGoldCollected()
        {
            Assert.That(_tracker.TotalGoldCollected, Is.Zero);

            _tracker.AddGold(3);
            Assert.That(_tracker.TotalGoldCollected, Is.EqualTo(3));

            _tracker.AddGold(7);
            Assert.That(_tracker.TotalGoldCollected, Is.EqualTo(10));

            _tracker.AddGold(0);
            Assert.That(_tracker.TotalGoldCollected, Is.EqualTo(10));
        }

        [Test]
        public void RecordDamage_StoresAndAccumulatesDamageSeparatelyByWeaponName()
        {
            Assert.That(_tracker.GetWeaponStats(), Is.Empty);

            _tracker.RecordDamage("TestWeaponA", 3);
            Assert.That(_tracker.GetWeaponStats()["TestWeaponA"], Is.EqualTo(3));

            _tracker.RecordDamage("TestWeaponB", 7);
            _tracker.RecordDamage("TestWeaponA", 5);

            var stats = _tracker.GetWeaponStats();
            Assert.That(stats.Count, Is.EqualTo(2));
            Assert.That(stats["TestWeaponA"], Is.EqualTo(8));
            Assert.That(stats["TestWeaponB"], Is.EqualTo(7));
        }
    }
}
