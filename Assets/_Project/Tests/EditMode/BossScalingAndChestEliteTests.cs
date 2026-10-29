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
                if (stage == 7) continue; // VS Avatar of Gaea: 8, then 8 + the run's kills (AvatarOfGaea), not HP x Level
                Assert.That(data.HealthPerPlayerLevel, Is.GreaterThan(0), $"Stage {stage} boss health does not scale with level.");
            }
        }

        [Test]
        public void StageBosses_FollowTheVs2500Bosses_AlongTheStages()
        {
            // Each source stage's 25:00 boss statbox (HP x Level, raw pages 2026-10-27): Giant Blue Venus 150, Hag 250, Sword
            // Guardian 250, Giant Enemy Crab 500, Ethlenn 150, Orochimario 500, Trinacria (boss) 250 on Levels 8-9. Owner
            // 2026-10-27: VS first. Level 7 is Avatar of Gaea ("health = 8", then the run's kills: AvatarOfGaea, 2026-10-28);
            // Level 10 (Shadow Kael, the owner's story) keeps 500.
            int[] expected = { 150, 250, 250, 500, 150, 500, 0, 250, 250, 500 };
            for (int stage = 1; stage <= 10; stage++)
            {
                var data = AssetDatabase.LoadAssetAtPath<GameObject>(string.Format(BossPrefabPath, stage)).GetComponent<EnemyController>().Data;
                Assert.That(data.HealthPerPlayerLevel, Is.EqualTo(expected[stage - 1]), $"Stage {stage}");
            }
        }

        [TestCase(1f, 10, 500)]    // Glowing Bat: 50 x level
        [TestCase(5f, 10, 500)]    // the 5:00 Mantichana: a flat 500
        [TestCase(5f, 80, 500)]
        [TestCase(10f, 10, 1500)]  // Giant Mantichana: 150 x level
        [TestCase(15f, 10, 2000)]  // Giant Werewolf: 200 x level
        [TestCase(20f, 10, 2500)]  // Giant Mummy: 250 x level
        [TestCase(29f, 10, 500)]
        public void ChestElites_UseTheVsMadForestChestBosses(float minute, int level, int expected)
        {
            Assert.That(EnemySpawner.ChestEliteHealth(minute, level), Is.EqualTo(expected));
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
    
        [Test]
        public void LevelSevensGuardian_IsTheAvatarOfGaea()
        {
            // Avatar_of_Gaea (raw 2026-10-28): "health = 8 | movespeed = 140 | damage = 5 | knockback = 0 | knockback-max = 1 |
            // xp = 30 | res-freeze = 2 | res-debuff = 1"; "[(enemies killed / 1,000 x 0.2 seconds), at most 10 seconds + 2 more]".
            var data = AssetDatabase.LoadAssetAtPath<GameObject>(string.Format(BossPrefabPath, 7)).GetComponent<EnemyController>().Data;
            Assert.That((data.MaxHealth, data.DamageToPlayer, data.Knockback, data.MaxKnockback, data.FreezeResistance, data.DebuffResistance),
                Is.EqualTo((8, 5, 0f, 1f, 2f, true)));
            Assert.That(data.MoveSpeed, Is.EqualTo(140f * SoulHunter.Gameplay.Data.EnemyAbilityDefaults.VsSpeedToUnits).Within(1e-4f));
            Assert.That(SoulHunter.Gameplay.Hazards.AvatarOfGaea.CountdownFor(0), Is.EqualTo(2f));
            Assert.That(SoulHunter.Gameplay.Hazards.AvatarOfGaea.CountdownFor(25000), Is.EqualTo(7f).Within(1e-4f));
            Assert.That(SoulHunter.Gameplay.Hazards.AvatarOfGaea.CountdownFor(90000), Is.EqualTo(12f), "at most 10 seconds + 2 more");
            Assert.That(SoulHunter.Gameplay.Hazards.AvatarOfGaea.HealedHealthFor(40000), Is.EqualTo(40008));
            // "it gains 1 max health per Max Health bonus and 0.01 power per 1% Might bonus" (raw 2026-10-29).
            Assert.That(SoulHunter.Gameplay.Hazards.AvatarOfGaea.HealedHealthFor(40000, 3), Is.EqualTo(40011));
            Assert.That(SoulHunter.Gameplay.Hazards.AvatarOfGaea.DamageFor(5f, 0.10f), Is.EqualTo(5.5f).Within(1e-4f), "10 eggs of +1% Might: power 1.1");
            Assert.That(SoulHunter.Gameplay.Hazards.AvatarOfGaea.DamageFor(5f, 0f), Is.EqualTo(5f));
        }
    }
}
