using NUnit.Framework;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Data;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: BossScalingAndChestEliteTests verifies two VS rules: stage boss health is
    /// a per-level value x the player's level, and chest elites are due every 5 minutes before the boss.
    /// </summary>
    public class BossScalingAndChestEliteTests
    {
        private const string BossPrefabPath = "Assets/Prefabs/Bosses/Gameplay/SH10_L{0:00}_BossGameplay.prefab";

        [Test]
        public void LevelScaledHealth_IsPerLevelTimesPlayerLevel()
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            data.MaxHealth = 1000;
            data.HealthPerPlayerLevel = 50;
            Assert.That(data.HealthForPlayerLevel(20), Is.EqualTo(1000));
            Assert.That(data.HealthForPlayerLevel(80), Is.EqualTo(4000));
            Assert.That(data.HealthForPlayerLevel(0), Is.EqualTo(50), "Level is at least 1.");

            data.HealthPerPlayerLevel = 0;
            Assert.That(data.HealthForPlayerLevel(80), Is.EqualTo(1000), "Unscaled enemies keep MaxHealth.");
            Object.DestroyImmediate(data);
        }

        [Test]
        public void EveryStageBoss_ScalesWithPlayerLevel()
        {
            for (int stage = 1; stage <= 10; stage++)
            {
                string path = string.Format(BossPrefabPath, stage);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(prefab, Is.Not.Null, $"{path} should load.");
                var data = prefab.GetComponent<EnemyController>().Data;
                Assert.That(data.HealthPerPlayerLevel, Is.GreaterThan(0), $"Stage {stage} boss health does not scale with level.");
            }
        }

        [TestCase(0f, 0)]
        [TestCase(299f, 0)]
        [TestCase(300f, 1)]
        [TestCase(1200f, 4)]
        [TestCase(1499f, 4)]
        [TestCase(1800f, 4)]
        public void ChestElites_AreDueEveryFiveMinutesBeforeTheBoss(float stageTime, int expected)
        {
            Assert.That(EnemySpawner.ChestElitesDue(stageTime, 300f, 1500f), Is.EqualTo(expected));
        }

        [Test]
        public void ChestElites_AreOffWithoutAnInterval()
        {
            Assert.That(EnemySpawner.ChestElitesDue(1000f, 0f, 1500f), Is.EqualTo(0));
        }
    }
}
