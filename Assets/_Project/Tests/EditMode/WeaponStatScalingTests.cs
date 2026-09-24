using NUnit.Framework;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: WeaponStatScalingTests verifies the shared AutoAttackWeapon stat helpers:
    /// a weapon held by the player scales with its PlayerStats, and a weapon without an owner stays neutral.
    /// </summary>
    public class WeaponStatScalingTests
    {
        private class ProbeWeapon : AutoAttackWeapon
        {
            public float Damage(float baseDamage) => ScaledDamage(baseDamage);
            public float Area => AreaMultiplier;
            public float Duration => DurationMultiplier;
            public float Speed => SpeedMultiplier;
            public float Cooldown => CooldownMultiplier;
            public int Extra => ExtraAmount;

            public void ScaleByArea(GameObject instance, GameObject prefab) => ApplyArea(instance, prefab);
        }

        private GameObject _player;
        private GameObject _prefab;
        private GameObject _instance;

        [TearDown]
        public void TearDown()
        {
            if (_player != null) Object.DestroyImmediate(_player);
            if (_prefab != null) Object.DestroyImmediate(_prefab);
            if (_instance != null) Object.DestroyImmediate(_instance);
        }

        private ProbeWeapon CreateWeaponUnder(GameObject owner)
        {
            var weaponObject = new GameObject("ProbeWeapon");
            weaponObject.transform.SetParent(owner.transform);
            return weaponObject.AddComponent<ProbeWeapon>();
        }

        [Test]
        public void WeaponHeldByPlayer_ScalesWithEveryPlayerStat()
        {
            // Learning Comment: Passive items (Spinach, Candelabrador, Spellbinder, Bracer, Empty Tome, Duplicator)
            // ka asar player ke har weapon par hona chahiye.
            _player = new GameObject("Player");
            var stats = _player.AddComponent<PlayerStats>();
            stats.AddMight(0.5f);
            stats.AddArea(0.25f);
            stats.AddDuration(0.5f);
            stats.AddProjectileSpeed(0.2f);
            stats.ReduceCooldown(0.2f);
            stats.AddAmount(2);

            var weapon = CreateWeaponUnder(_player);

            Assert.That(weapon.Damage(10f), Is.EqualTo(15f).Within(0.001f));
            Assert.That(weapon.Area, Is.EqualTo(1.25f).Within(0.001f));
            Assert.That(weapon.Duration, Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(weapon.Speed, Is.EqualTo(1.2f).Within(0.001f));
            Assert.That(weapon.Cooldown, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(weapon.Extra, Is.EqualTo(2));
        }

        [Test]
        public void WeaponWithoutPlayerStats_IsNeutral()
        {
            // Learning Comment: Shadow Kael ke mirrored weapons ke paas PlayerStats nahi hota; unhe default values milni chahiye.
            _player = new GameObject("Boss");
            var weapon = CreateWeaponUnder(_player);

            Assert.That(weapon.Damage(10f), Is.EqualTo(10f));
            Assert.That(weapon.Area, Is.EqualTo(1f));
            Assert.That(weapon.Duration, Is.EqualTo(1f));
            Assert.That(weapon.Speed, Is.EqualTo(1f));
            Assert.That(weapon.Cooldown, Is.EqualTo(1f));
            Assert.That(weapon.Extra, Is.EqualTo(0));
        }

        [Test]
        public void ApplyArea_ScalesFromPrefabScale_SoPooledObjectsDoNotGrowEachUse()
        {
            // Learning Comment: Pool se wapas aane wale object ka size har baar prefab se calculate ho, warna woh baar-baar bada hota jayega.
            _player = new GameObject("Player");
            _player.AddComponent<PlayerStats>().AddArea(1f);
            var weapon = CreateWeaponUnder(_player);

            _prefab = new GameObject("Prefab");
            _prefab.transform.localScale = new Vector3(2f, 2f, 2f);
            _instance = new GameObject("Instance");

            weapon.ScaleByArea(_instance, _prefab);
            weapon.ScaleByArea(_instance, _prefab);

            Assert.That(_instance.transform.localScale, Is.EqualTo(new Vector3(4f, 4f, 4f)));
        }
    }
}
