using System.Reflection;
using NUnit.Framework;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Weapons;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// VS wiki (Soul_Eater, fetched 2026-10-09): "Soul Eater gains +1 damage every 60 health healed, capping at +60 damage or
    /// 3,600 health healed. ... only healing that actually takes place is counted, overhealing does not." Revival: "The
    /// health gained upon reviving counts as healing".
    /// </summary>
    public class SoulEaterHealingTests
    {
        private GameObject _owner;

        [TearDown]
        public void Destroy()
        {
            if (_owner != null) Object.DestroyImmediate(_owner);
        }

        private GarlicWeapon Create(float lifeStealPerHit, out HealthController health)
        {
            _owner = new GameObject("Owner");
            health = _owner.AddComponent<HealthController>();
            health.Initialize(100);
            var weaponObject = new GameObject("SoulEater");
            weaponObject.transform.SetParent(_owner.transform);
            var garlic = weaponObject.AddComponent<GarlicWeapon>();
            typeof(GarlicWeapon).GetField("_lifeStealPerHit", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(garlic, lifeStealPerHit);
            // Edit Mode does not run OnEnable on its own: the subscription to the holder's healing happens there.
            typeof(GarlicWeapon).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(garlic, null);
            return garlic;
        }

        [Test]
        public void TheBonus_IsOnePer60Healed_CappedAt60()
        {
            Assert.That(GarlicWeapon.HealDamageBonusFor(0), Is.EqualTo(0));
            Assert.That(GarlicWeapon.HealDamageBonusFor(59), Is.EqualTo(0));
            Assert.That(GarlicWeapon.HealDamageBonusFor(60), Is.EqualTo(1));
            Assert.That(GarlicWeapon.HealDamageBonusFor(179), Is.EqualTo(2));
            Assert.That(GarlicWeapon.HealDamageBonusFor(3600), Is.EqualTo(60));
            Assert.That(GarlicWeapon.HealDamageBonusFor(100000), Is.EqualTo(60), "+60 at most.");
        }

        [Test]
        public void OnlyRealHealing_Counts_OverhealDoesNot()
        {
            var soulEater = Create(0.2f, out var health);
            health.TakeDamage(new DamagePacket(90, Vector3.zero, Vector3.zero) { IgnoresHitInvulnerability = true });
            Assert.That(health.CurrentHealth, Is.EqualTo(10));

            health.Heal(500); // 90 really healed, 410 overheal
            Assert.That(soulEater.HealthHealed, Is.EqualTo(90));
            Assert.That(soulEater.HealDamageBonus, Is.EqualTo(1));
            health.Heal(500); // full: nothing counts
            Assert.That(soulEater.HealthHealed, Is.EqualTo(90));
        }

        [Test]
        public void TheCount_StopsAt3600()
        {
            var soulEater = Create(0.2f, out _);
            for (int i = 0; i < 100; i++) soulEater.CountHealing(60);
            Assert.That(soulEater.HealthHealed, Is.EqualTo(3600));
            Assert.That(soulEater.HealDamageBonus, Is.EqualTo(60));
        }

        [Test]
        public void ARevivalsHeal_RaisesTheBonus()
        {
            var soulEater = Create(0.2f, out var health);
            health.Initialize(240);
            health.TryPreventDeath = () => { health.HoldForRevive(); return true; };
            health.TakeDamage(new DamagePacket(1000, Vector3.zero, Vector3.zero) { IgnoresHitInvulnerability = true });
            health.ReviveFromDeath(); // 120 health back
            Assert.That(soulEater.HealthHealed, Is.EqualTo(120));
            Assert.That(soulEater.HealDamageBonus, Is.EqualTo(2), "floor(120 / 60)");
        }

        [Test]
        public void PlainGarlic_DoesNotGrowWithHealing()
        {
            var garlic = Create(0f, out var health);
            health.TakeDamage(new DamagePacket(50, Vector3.zero, Vector3.zero) { IgnoresHitInvulnerability = true });
            health.Heal(50);
            garlic.CountHealing(600);
            Assert.That(garlic.HealthHealed, Is.EqualTo(0));
            Assert.That(garlic.HealDamageBonus, Is.EqualTo(0));
        }
    }
}
