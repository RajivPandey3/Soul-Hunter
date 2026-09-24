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
    /// coin values never drop below 1, and elites drop more and bigger coins than ordinary enemies.
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

        [Test]
        public void Elites_DropMoreAndBiggerCoins_ThanStageEnemies()
        {
            // Learning Comment: Elite ka gold chance aur coin value normal dushman se zyada ho.
            for (int stage = 1; stage <= CampaignLevelCatalog.Count; stage++)
            {
                var enemy = LoadData($"Assets/Prefabs/Enemies/Gameplay/SH10_L{stage:00}_EnemyGameplay.prefab");
                var elite = LoadData($"Assets/Prefabs/Enemies/Gameplay/SH10_L{stage:00}_EliteGameplay.prefab");
                Assert.That(enemy.DropChanceGold, Is.GreaterThan(0f), $"Stage {stage} enemy gold chance.");
                Assert.That(elite.DropChanceGold, Is.GreaterThan(enemy.DropChanceGold), $"Stage {stage} elite gold chance.");
                Assert.That(elite.GoldValue, Is.GreaterThan(enemy.GoldValue), $"Stage {stage} elite coin value.");
            }
        }

        private static EnemyData LoadData(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            var data = prefab.GetComponent<EnemyController>().Data;
            Assert.That(data, Is.Not.Null, path);
            return data;
        }
    }
}
