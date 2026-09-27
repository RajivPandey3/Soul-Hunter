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

        [Test]
        public void StageBosses_GrowWithTheStory_AndOutlastTheirChestElites()
        {
            // Owner's story (2026-09-27): later guardians are stronger; per-level health is 50 x stage.
            for (int stage = 1; stage <= 10; stage++)
            {
                var data = AssetDatabase.LoadAssetAtPath<GameObject>(string.Format(BossPrefabPath, stage)).GetComponent<EnemyController>().Data;
                Assert.That(data.HealthPerPlayerLevel, Is.EqualTo(50 * stage), $"Stage {stage}");
            }
            Assert.That(EnemySpawner.StoryBossHealth(200, 625), Is.EqualTo(1250), "A low-level player still meets a boss stronger than two elites.");
            Assert.That(EnemySpawner.StoryBossHealth(2400, 625), Is.EqualTo(2400), "Otherwise per-level health decides.");
        }

        // Owner (2026-09-27): the VS Mad Forest chest minutes 1, 3, 5, 7, 9, 10, 12, 14, 15, 16, 18, 20, 22, 23, 27, 29
        // (25:00 is the boss; the Arcana-only 11:00 and 21:00 chests are left out).
        [TestCase(0f, 0)]
        [TestCase(59f, 0)]
        [TestCase(60f, 1)]
        [TestCase(300f, 3)]
        [TestCase(660f, 6)]
        [TestCase(1260f, 12)]
        [TestCase(1500f, 14)]
        [TestCase(1740f, 16)]
        [TestCase(1800f, 16)]
        public void ChestElites_ComeAtTheVsMinutes(float stageTime, int expected)
        {
            Assert.That(EnemySpawner.ChestElitesDue(stageTime, EnemySpawner.DefaultChestEliteMinutes), Is.EqualTo(expected));
        }

        [Test]
        public void ChestElites_AreOffWithoutMinutes()
        {
            Assert.That(EnemySpawner.ChestElitesDue(1000f, null), Is.EqualTo(0));
            Assert.That(EnemySpawner.ChestElitesDue(1000f, new float[0]), Is.EqualTo(0));
        }
    }
}
