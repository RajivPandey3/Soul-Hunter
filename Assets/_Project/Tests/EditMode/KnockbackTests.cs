using System.Collections.Generic;
using NUnit.Framework;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Weapons;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Part 5, VS knockback (vampire.survivors.wiki, Knockback): a hit reverses an enemy's movement for 120 ms at
    /// move speed x weapon knockback x the enemy's knockback taken; Garlic raises knockback taken up to the maximum;
    /// ghosts take none; the player is never knocked back.
    /// </summary>
    public class KnockbackTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects) if (go != null) Object.DestroyImmediate(go);
            _objects.Clear();
        }

        [Test]
        public void KnockbackSpeed_IsMoveSpeedTimesWeaponTimesTaken()
        {
            Assert.That(EnemyController.KnockbackSpeed(3f, 1f, 1f), Is.EqualTo(3f));
            Assert.That(EnemyController.KnockbackSpeed(3f, 0.3f, 2f), Is.EqualTo(1.8f).Within(1e-5));
            Assert.That(EnemyController.KnockbackSpeed(3f, 1f, 0f), Is.EqualTo(0f), "Ghosts take no knockback.");
            Assert.That(EnemyController.KnockbackSeconds, Is.EqualTo(0.12f).Within(1e-6), "VS: 120 ms.");
        }

        [Test]
        public void EnemyData_GhostsTakeNone_ElitesHalf_BossesNone_OthersOne()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:EnemyData", new[] { "Assets/_Project/Data/Config/Enemies" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
                string file = System.IO.Path.GetFileName(path);
                float expected = file.Contains("_Boss_") ? 0f : file.Contains("_Elite_") ? 0.5f
                               : file.StartsWith("L07") ? 0f : 1f;
                Assert.That(data.Knockback, Is.EqualTo(expected).Within(1e-5), file);
                Assert.That(data.MaxKnockback, Is.EqualTo(3f).Within(1e-5), file);
            }
        }

        [Test]
        public void Garlic_RaisesKnockbackTaken_UpToTheMaximum()
        {
            var go = new GameObject("KnockbackEnemy");
            _objects.Add(go);
            var enemy = go.AddComponent<EnemyController>();
            Assert.That(enemy.KnockbackTaken, Is.EqualTo(1f));
            enemy.AddKnockbackTaken(EnemyController.GarlicKnockbackStep);
            Assert.That(enemy.KnockbackTaken, Is.EqualTo(1.3f).Within(1e-5));
            for (int i = 0; i < 20; i++) enemy.AddKnockbackTaken(EnemyController.GarlicKnockbackStep);
            Assert.That(enemy.KnockbackTaken, Is.EqualTo(enemy.MaxKnockback).Within(1e-5), "Capped at the enemy's maximum.");
            var garlic = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/Garlic_Weapon.prefab").GetComponent<GarlicWeapon>();
            Assert.That(garlic.Knockback, Is.EqualTo(0f), "VS wiki Garlic: knockback 0 (it raises knockback taken instead).");
        }

        /// <summary>VS DOT Phase 1 row 8 (2026-10-01): each weapon's knockback from its wiki infobox.</summary>
        [TestCase("Assets/Prefabs/Weapons/Whip_Weapon.prefab", 1f)]
        [TestCase("Assets/Prefabs/Weapons/Generated/Knife.prefab", 0.5f)]
        [TestCase("Assets/Prefabs/Weapons/Generated/ThousandEdge.prefab", 0.5f)]
        [TestCase("Assets/Prefabs/Weapons/Generated/Peachone.prefab", 2f)]
        [TestCase("Assets/Prefabs/Weapons/Generated/SantaWater.prefab", 0f)]
        [TestCase("Assets/Prefabs/Weapons/Cross_Weapon.prefab", 1f)]
        [TestCase("Assets/Prefabs/Weapons/Generated/HeavenSword.prefab", 6f)]
        [TestCase("Assets/Prefabs/Weapons/Bible_Weapon.prefab", 1f)]
        [TestCase("Assets/Prefabs/Weapons/Generated/UnholyVespers.prefab", 4f)]
        [TestCase("Assets/Prefabs/Weapons/Garlic_Weapon.prefab", 0f)]
        [TestCase("Assets/Prefabs/Weapons/Generated/SoulEater.prefab", 0f)]
        public void EachWeapon_HasItsVsKnockback(string path, float knockback)
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            Assert.That(prefab.GetComponent<SoulHunter.Gameplay.Combat.AutoAttackWeapon>().Knockback, Is.EqualTo(knockback).Within(1e-5), path);
        }

        [Test]
        public void APacketWithZeroKnockback_DoesNotPush_AndAnUnsetOneUsesTheDefault()
        {
            Assert.That(new SoulHunter.Gameplay.Combat.DamagePacket(1, Vector3.zero, Vector3.zero).KnockbackOrDefault, Is.EqualTo(1f));
            Assert.That(new SoulHunter.Gameplay.Combat.DamagePacket(1, Vector3.zero, Vector3.zero).WithKnockback(0f).KnockbackOrDefault, Is.EqualTo(0f));
            Assert.That(new SoulHunter.Gameplay.Combat.DamagePacket(1, Vector3.zero, Vector3.zero).WithKnockback(6f).KnockbackOrDefault, Is.EqualTo(6f));
            Assert.That(new SoulHunter.Gameplay.Combat.DamagePacket(1, Vector3.zero, Vector3.zero).WithKnockback(-1f).KnockbackOrDefault, Is.EqualTo(1f));
        }

        [Test]
        public void TheHealthOfSomethingThatIsNotAnEnemy_NeverSlides()
        {
            // The player (no EnemyController) used to slide 2 units when touched; VS never knocks the player back.
            var go = new GameObject("NotAnEnemy");
            _objects.Add(go);
            go.transform.position = new Vector3(1f, 0f, 2f);
            var health = go.AddComponent<HealthController>();
            health.Initialize(100);
            health.TakeDamage(new DamagePacket(5, Vector3.zero, Vector3.right * 5f));
            Assert.That(health.CurrentHealth, Is.EqualTo(95));
            Assert.That(go.transform.position, Is.EqualTo(new Vector3(1f, 0f, 2f)));
        }
    }
}
