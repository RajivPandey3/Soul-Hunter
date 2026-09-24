using System.Collections.Generic;
using NUnit.Framework;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Data;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: EnemyDataWiringTests verifies that every stage enemy, elite and boss gameplay prefab
    /// has its own EnemyData asset instead of sharing one, and that bosses drop chests while enemies and elites do not.
    /// </summary>
    public class EnemyDataWiringTests
    {
        private const string EnemyPrefabPath = "Assets/Prefabs/Enemies/Gameplay/SH10_L{0:00}_EnemyGameplay.prefab";
        private const string ElitePrefabPath = "Assets/Prefabs/Enemies/Gameplay/SH10_L{0:00}_EliteGameplay.prefab";
        private const string BossPrefabPath = "Assets/Prefabs/Bosses/Gameplay/SH10_L{0:00}_BossGameplay.prefab";

        private static EnemyData LoadData(string pathFormat, int stage)
        {
            string path = string.Format(pathFormat, stage);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, $"{path} load hona chahiye.");
            var controller = prefab.GetComponent<EnemyController>();
            Assert.That(controller, Is.Not.Null, $"{path} par EnemyController hona chahiye.");
            Assert.That(controller.Data, Is.Not.Null, $"{path} ka EnemyData missing nahi hona chahiye.");
            return controller.Data;
        }

        [Test]
        public void AllStageEnemiesElitesAndBosses_UseDistinctEnemyData()
        {
            // Learning Comment: Har stage ke enemy, elite aur boss ka alag data asset hona chahiye, warna sab ek jaise stats share karenge.
            var seen = new HashSet<EnemyData>();
            for (int stage = 1; stage <= CampaignLevelCatalog.Count; stage++)
            {
                Assert.That(seen.Add(LoadData(EnemyPrefabPath, stage)), Is.True, $"Stage {stage} enemy data shared hai.");
                Assert.That(seen.Add(LoadData(ElitePrefabPath, stage)), Is.True, $"Stage {stage} elite data shared hai.");
                Assert.That(seen.Add(LoadData(BossPrefabPath, stage)), Is.True, $"Stage {stage} boss data shared hai.");
            }

            Assert.That(seen.Count, Is.EqualTo(30));
        }

        [Test]
        public void Bosses_DropChests_AndEnemies_DoNot()
        {
            // Learning Comment: Vampire Survivors ki tarah boss marne par chest girna chahiye; normal dushman chest nahi girate.
            for (int stage = 1; stage <= CampaignLevelCatalog.Count; stage++)
            {
                Assert.That(LoadData(BossPrefabPath, stage).DropsChest, Is.True, $"Stage {stage} boss ko chest girana chahiye.");
                Assert.That(LoadData(EnemyPrefabPath, stage).DropsChest, Is.False, $"Stage {stage} enemy ko chest nahi girana chahiye.");
                Assert.That(LoadData(ElitePrefabPath, stage).DropsChest, Is.False, $"Stage {stage} elite ko chest nahi girana chahiye.");
            }
        }

        [Test]
        public void EnemyAndBossStats_GrowWithStage()
        {
            // Learning Comment: Aage ke stages ke dushman pichhle stage se kamzor nahi hone chahiye.
            for (int stage = 2; stage <= CampaignLevelCatalog.Count; stage++)
            {
                var enemy = LoadData(EnemyPrefabPath, stage);
                var previousEnemy = LoadData(EnemyPrefabPath, stage - 1);
                Assert.That(enemy.MaxHealth, Is.GreaterThan(previousEnemy.MaxHealth), $"Stage {stage} enemy health.");
                Assert.That(enemy.DamageToPlayer, Is.GreaterThanOrEqualTo(previousEnemy.DamageToPlayer), $"Stage {stage} enemy damage.");

                var elite = LoadData(ElitePrefabPath, stage);
                var previousElite = LoadData(ElitePrefabPath, stage - 1);
                Assert.That(elite.MaxHealth, Is.GreaterThan(previousElite.MaxHealth), $"Stage {stage} elite health.");

                var boss = LoadData(BossPrefabPath, stage);
                var previousBoss = LoadData(BossPrefabPath, stage - 1);
                Assert.That(boss.MaxHealth, Is.GreaterThan(previousBoss.MaxHealth), $"Stage {stage} boss health.");
            }
        }

        [Test]
        public void Elites_AreTougherThanStageEnemies_AndHaveGameplayComponents()
        {
            // Learning Comment: Elite sirf model nahi hona chahiye; usme chase, damage, health aur drop components hone chahiye.
            for (int stage = 1; stage <= CampaignLevelCatalog.Count; stage++)
            {
                var enemy = LoadData(EnemyPrefabPath, stage);
                var elite = LoadData(ElitePrefabPath, stage);
                Assert.That(elite.MaxHealth, Is.GreaterThan(enemy.MaxHealth), $"Stage {stage} elite health.");
                Assert.That(elite.DamageToPlayer, Is.GreaterThan(enemy.DamageToPlayer), $"Stage {stage} elite damage.");

                string path = string.Format(ElitePrefabPath, stage);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(prefab.GetComponent<Rigidbody>(), Is.Not.Null, $"{path} Rigidbody.");
                Assert.That(prefab.GetComponent<Collider>(), Is.Not.Null, $"{path} Collider.");
                Assert.That(prefab.GetComponent<SoulHunter.Gameplay.Combat.HealthController>(), Is.Not.Null, $"{path} HealthController.");
                Assert.That(prefab.GetComponent<SoulHunter.Gameplay.Combat.TouchDamage>(), Is.Not.Null, $"{path} TouchDamage.");
                Assert.That(prefab.GetComponent<EnemyDrop>(), Is.Not.Null, $"{path} EnemyDrop.");
                Assert.That(prefab.transform.Find("SH10_Visual"), Is.Not.Null, $"{path} SH10_Visual child.");
            }
        }
    }
}
