using NUnit.Framework;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: GoldRewardTests verifies VS-style gold: every gold reward is multiplied by Greed,
    /// and coin values never drop below 1. (Enemies no longer drop coins: VS rule, 2026-09-29; see EnemyDropPlayModeTests.)
    /// </summary>
    public class GoldRewardTests
    {
        private GameObject _object;

        [TearDown]
        public void TearDown()
        {
            if (_object != null) Object.DestroyImmediate(_object);
        }

        [Test]
        public void ApplyGreed_MultipliesGold_ByPlayerGreed()
        {
            // Learning Comment: Greed 1.5 par 10 gold ka reward 15 ho jana chahiye.
            _object = new GameObject("Player");
            var stats = _object.AddComponent<PlayerStats>();

            Assert.That(GoldRewards.ApplyGreed(10, stats), Is.EqualTo(10));
            stats.AddGreed(0.5f);
            Assert.That(GoldRewards.ApplyGreed(10, stats), Is.EqualTo(15));
            Assert.That(GoldRewards.ApplyGreed(1, stats), Is.EqualTo(2), "1.5 round hokar 2 ho.");
        }

        [Test]
        public void ApplyGreed_WithoutStats_IsUnscaled_AndZeroStaysZero()
        {
            Assert.That(GoldRewards.ApplyGreed(25, null), Is.EqualTo(25));
            Assert.That(GoldRewards.ApplyGreed(0, null), Is.EqualTo(0));
        }

        [Test]
        public void Grant_ReturnsGreedScaledAmount()
        {
            _object = new GameObject("Player");
            var stats = _object.AddComponent<PlayerStats>();
            stats.AddGreed(1f);

            Assert.That(GoldRewards.Grant(5, stats), Is.EqualTo(10));
        }

        [Test]
        public void GoldPickupValue_NeverDropsBelowOne()
        {
            _object = new GameObject("Coin");
            _object.AddComponent<SphereCollider>();
            var coin = _object.AddComponent<GoldPickup>();

            coin.Value = 0;
            Assert.That(coin.Value, Is.EqualTo(1));
            coin.Value = 10;
            Assert.That(coin.Value, Is.EqualTo(10));
        }
    }
}
