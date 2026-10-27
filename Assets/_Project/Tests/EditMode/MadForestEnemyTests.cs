using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Level 1's VS Mad Forest enemy types (vampire.survivors.wiki/w/Mad_Forest "Waves" and each enemy's statbox, fetched
    /// 2026-10-03): the table, the built assets and Level 1's wave asset.
    /// </summary>
    public class MadForestEnemyTests
    {
        [TestCase("GraveBat", 1, 5, 140f, 1f, 1f)]       // Pipeestrello 1
        [TestCase("RedEyeBat", 5, 5, 140f, 1f, 1f)]      // Pipeestrello 2
        [TestCase("RestlessCorpse", 10, 10, 100f, 0.8f, 1f)] // Zombie
        [TestCase("BoneSoldier", 15, 10, 100f, 1f, 2f)]  // Skeleton
        [TestCase("GraveWisp", 10, 5, 200f, 0f, 1.5f)]   // Ghost 1
        [TestCase("AshMireFiend", 70, 10, 100f, 0.3f, 2.5f)]  // Mudman 1
        [TestCase("MossMireFiend", 150, 10, 100f, 0.3f, 2.5f)] // Mudman 2
        [TestCase("GreatGraveBat", 270, 10, 140f, 0.1f, 2.5f)] // Giant Bat
        [TestCase("GhoulHound", 180, 14, 130f, 0.8f, 2f)] // Werewolf
        [TestCase("MantisWraith", 500, 20, 80f, 0f, 3f)]  // Mantichana
        [TestCase("ShroudGiant", 500, 20, 80f, 0f, 3f)]   // Big Mummy
        [TestCase("ThornWall", 30, 1, 20f, 1f, 2f)]       // Flower Wall
        [TestCase("GraveLily", 500, 20, 80f, 0f, 3f)]     // Venus
        [TestCase("EmberBat", 50, 10, 140f, 1f, 30f)]     // Pipeestrello 4
        public void EachType_HasItsWikiStats(string id, int health, int damage, float vsSpeed, float knockback, float vsXp)
        {
            var type = MadForestEnemies.Find(id);
            Assert.AreEqual(health, type.Health);
            Assert.AreEqual(damage, type.Damage);
            Assert.AreEqual(vsSpeed, type.VsSpeed);
            Assert.AreEqual(knockback, type.Knockback);
            Assert.AreEqual(vsXp, type.VsXp);
            Assert.AreEqual(3f, type.MaxKnockback, "Knockback cap 3 (every statbox).");
            Assert.AreEqual(vsSpeed * 0.03f, type.MoveSpeed, 1e-5, "VS 100 = 3 units/s.");
            Assert.AreEqual(Mathf.RoundToInt(vsXp * 10f), type.ExperienceValue, "XP x10 like Soul Hunter's gems.");
        }

        [Test]
        public void HpTimesLevel_IsOnlyOnTheWikiSkillHolders()
        {
            var holders = MadForestEnemies.Types.Where(t => t.HealthTimesLevel).Select(t => t.Id).OrderBy(x => x).ToArray();
            CollectionAssert.AreEqual(new[] { "AshenBat", "EmberBat", "GreatGhoulHound", "GreatMantisWraith", "GreatShroudGiant", "ThornWall" }, holders);
        }

        [Test]
        public void EveryMinute_ListsKnownTypes_AsTheWikiTableDoes()
        {
            Assert.AreEqual(30, MadForestEnemies.ByMinute.Length, "Minutes 0-29.");
            foreach (var minute in MadForestEnemies.ByMinute)
                foreach (var id in minute) Assert.DoesNotThrow(() => MadForestEnemies.Find(id), id);
            CollectionAssert.AreEqual(new[] { "BoneSoldier" }, MadForestEnemies.ByMinute[3], "3:00 Skeleton.");
            CollectionAssert.AreEqual(new[] { "GhoulHound", "GraveWisp", "BoneSoldier" }, MadForestEnemies.ByMinute[12], "12:00 Werewolf, Ghost, Skeleton.");
            CollectionAssert.AreEqual(new[] { "ShroudGiant" }, MadForestEnemies.ByMinute[17], "17:00 Big Mummy.");
            CollectionAssert.AreEqual(new[] { "GraveLily" }, MadForestEnemies.ByMinute[25], "25:00 Venus.");
            CollectionAssert.AreEqual(new[] { "EmberBat", "AshenBat" }, MadForestEnemies.ByMinute[29], "29:00 Pipeestrello 4 and 5.");
        }

        [Test]
        public void TheBuiltAssets_MatchTheTable()
        {
            foreach (var type in MadForestEnemies.Types)
            {
                var data = AssetDatabase.LoadAssetAtPath<EnemyData>(MadForestEnemies.DataPath(type.Id));
                Assert.IsNotNull(data, $"{type.Id} data (run Soul Hunter/Content/Build Level 1 Enemy Types).");
                Assert.AreEqual(type.Name, data.EnemyName);
                Assert.AreEqual(type.Health, data.MaxHealth);
                Assert.AreEqual(type.HealthTimesLevel ? type.Health : 0, data.HealthPerPlayerLevel);
                Assert.AreEqual(type.Damage, data.DamageToPlayer);
                Assert.AreEqual(type.MoveSpeed, data.MoveSpeed, 1e-5);
                Assert.AreEqual(type.Knockback, data.Knockback, 1e-5);
                Assert.AreEqual(type.ExperienceValue, data.ExperienceValue);
                Assert.AreEqual(type.Tint, data.VisualTint);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MadForestEnemies.PrefabPath(type.Id));
                Assert.IsNotNull(prefab, $"{type.Id} prefab.");
                var controller = prefab.GetComponent<EnemyController>();
                Assert.IsNotNull(controller);
                Assert.AreSame(data, controller.Data, $"{type.Id} prefab uses its own data.");
            }
        }

        [Test]
        public void LevelOnesWaveAsset_ListsEachMinutesTypes()
        {
            var wave = AssetDatabase.LoadAssetAtPath<WaveData>(WaveTables.StageWaveAssets[0]);
            Assert.IsNotNull(wave);
            Assert.AreEqual(30, wave.Minutes.Count);
            for (int m = 0; m < 30; m++)
            {
                var names = wave.Minutes[m].EnemyTypes.Select(p => p != null ? p.name : "null").ToArray();
                var expected = MadForestEnemies.ByMinute[m].Select(id => $"SH10_L01_{id}_Gameplay").ToArray();
                CollectionAssert.AreEqual(expected, names, $"Minute {m}.");
            }
        }
    }
}
