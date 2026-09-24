using System.Reflection;
using NUnit.Framework;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Weapons;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: EvolvedWeaponTests verifies the Heaven Sword, Soul Eater and Unholy Vespers evolutions:
    /// each recipe spawns a prefab built on its base weapon, and Soul Eater heals its owner per enemy hit.
    /// </summary>
    public class EvolvedWeaponTests
    {
        private GameObject _owner;

        [TearDown]
        public void TearDown()
        {
            if (_owner != null) Object.DestroyImmediate(_owner);
        }

        [TestCase("Evo_HeavenSword", UpgradeData.UpgradeType.Cross, UpgradeData.UpgradeType.Clover, typeof(CrossWeapon))]
        [TestCase("Evo_SoulEater", UpgradeData.UpgradeType.Garlic, UpgradeData.UpgradeType.Pummarola, typeof(GarlicWeapon))]
        [TestCase("Evo_UnholyVespers", UpgradeData.UpgradeType.Bible, UpgradeData.UpgradeType.Spellbinder, typeof(BibleWeapon))]
        public void Evolution_SpawnsPrefabBuiltOnItsBaseWeapon(string asset, UpgradeData.UpgradeType baseWeapon,
            UpgradeData.UpgradeType passive, System.Type weaponType)
        {
            // Learning Comment: VS recipe (base weapon Lv8 + passive) ka evolved prefab usi weapon script par bana ho.
            var evo = AssetDatabase.LoadAssetAtPath<WeaponEvolutionData>($"Assets/_Project/Data/Weapons/{asset}.asset");
            Assert.That(evo, Is.Not.Null);
            Assert.That(evo.BaseWeapon, Is.EqualTo(baseWeapon));
            Assert.That(evo.RequiredPassive, Is.EqualTo(passive));
            Assert.That(evo.EvolvedWeaponPrefab, Is.Not.Null, $"{asset} ab enabled hona chahiye.");
            Assert.That(evo.EvolvedWeaponPrefab.GetComponent(weaponType), Is.Not.Null);
        }

        private GarlicWeapon CreateSoulEater(float lifeStealPerHit, out HealthController health)
        {
            _owner = new GameObject("Owner");
            health = _owner.AddComponent<HealthController>();
            health.Initialize(100);
            health.TakeDamage(new DamagePacket(50, Vector3.zero, Vector3.zero));

            var weaponObject = new GameObject("SoulEater");
            weaponObject.transform.SetParent(_owner.transform);
            var garlic = weaponObject.AddComponent<GarlicWeapon>();
            typeof(GarlicWeapon).GetField("_lifeStealPerHit", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(garlic, lifeStealPerHit);
            return garlic;
        }

        [Test]
        public void SoulEater_HealsPerEnemyHit_CarryingFractions()
        {
            // Learning Comment: 0.2 HP per hit: 10 hits = 2 HP; 3 hits (0.6) bache rehte hain, agle 2 hits ke saath 1 HP.
            var soulEater = CreateSoulEater(0.2f, out var health);

            Assert.That(soulEater.StealLife(10), Is.EqualTo(2));
            Assert.That(health.CurrentHealth, Is.EqualTo(52));
            Assert.That(soulEater.StealLife(3), Is.EqualTo(0));
            Assert.That(soulEater.StealLife(2), Is.EqualTo(1));
            Assert.That(health.CurrentHealth, Is.EqualTo(53));
        }

        [Test]
        public void SoulEater_HealIsCappedPerPulse()
        {
            var soulEater = CreateSoulEater(0.2f, out var health);

            Assert.That(soulEater.StealLife(100), Is.EqualTo(5));
            Assert.That(health.CurrentHealth, Is.EqualTo(55));
        }

        [Test]
        public void PlainGarlic_DoesNotHeal()
        {
            var garlic = CreateSoulEater(0f, out var health);

            Assert.That(garlic.StealLife(50), Is.EqualTo(0));
            Assert.That(health.CurrentHealth, Is.EqualTo(50));
        }
    }
}
