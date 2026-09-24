using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SoulHunter.Gameplay.Combat;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: PierceTests verifies VS-style pierce: a projectile hits as many different enemies as its
    /// pierce allows and then disappears, and Knife gains pierce at levels 5 and 8.
    /// </summary>
    public class PierceTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _objects) if (obj != null) Object.DestroyImmediate(obj);
            _objects.Clear();
        }

        private GameObject Track(GameObject obj) { _objects.Add(obj); return obj; }

        private Collider CreateEnemy(int index)
        {
            var enemy = Track(new GameObject($"Enemy{index}"));
            enemy.AddComponent<HealthController>().Initialize(100);
            return enemy.AddComponent<BoxCollider>();
        }

        private int HitsBeforeProjectileDisappears(int pierce, int enemies)
        {
            var projectileObject = Track(new GameObject("Projectile"));
            projectileObject.AddComponent<Rigidbody>();
            projectileObject.AddComponent<SphereCollider>();
            var projectile = projectileObject.AddComponent<Projectile>();
            projectile.Initialize(Vector3.forward, 10f, 5f, 3f, DamageType.Normal, "Test", pierce);

            var onTrigger = typeof(Projectile).GetMethod("OnTriggerEnter", BindingFlags.Instance | BindingFlags.NonPublic);
            int hits = 0;
            for (int i = 0; i < enemies && projectileObject.activeSelf; i++)
            {
                onTrigger.Invoke(projectile, new object[] { CreateEnemy(i) });
                hits++;
            }
            Assert.That(projectileObject.activeSelf, Is.False, "Pierce khatam hone par projectile gayab ho.");
            return hits;
        }

        [Test]
        public void DefaultProjectile_HitsOneEnemy()
        {
            Assert.That(HitsBeforeProjectileDisappears(1, 5), Is.EqualTo(1));
        }

        [Test]
        public void PierceThree_HitsThreeDifferentEnemies()
        {
            // Learning Comment: Pierce 3 wala projectile 3 alag dushmano ko maar kar gayab ho.
            Assert.That(HitsBeforeProjectileDisappears(3, 5), Is.EqualTo(3));
        }

        [Test]
        public void Knife_GainsPierceAtLevelsFiveAndEight()
        {
            var knife = Track(new GameObject("Knife")).AddComponent<KnifeWeapon>();
            Assert.That(knife.Pierce, Is.EqualTo(1));

            while (knife.CurrentLevel < 5) knife.LevelUp();
            Assert.That(knife.Pierce, Is.EqualTo(2));

            while (knife.CurrentLevel < 8) knife.LevelUp();
            Assert.That(knife.Pierce, Is.EqualTo(3));
        }
    }
}
