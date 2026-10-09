using NUnit.Framework;
using SoulHunter.Gameplay.Combat;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// VS Revival (vampire.survivors.wiki/w/Revival, fetched 2026-10-09): "Each point of Revival a player has allows them to
    /// revive once upon death with 50% of their Max Health"; "The health gained upon reviving counts as healing"; "The
    /// player is additionally given a large amount of invulnerability frames" (no length on the wiki: Soul Hunter's 1.5 s,
    /// plus the 240 ms of Invulnerability_Time, which "adds a flat amount of time ... to another source of immunity").
    /// Soul_Eater: "only healing that actually takes place is counted, overhealing does not."
    /// </summary>
    public class RevivalTests
    {
        private GameObject _owner;
        private HealthController _health;

        [SetUp]
        public void Create()
        {
            _owner = new GameObject("RevivalOwner");
            _health = _owner.AddComponent<HealthController>();
        }

        [TearDown]
        public void Destroy()
        {
            if (_owner != null) Object.DestroyImmediate(_owner);
        }

        private static DamagePacket Lethal(int amount) => new DamagePacket(amount, Vector3.zero, Vector3.zero) { IgnoresHitInvulnerability = true };

        [Test]
        public void AHeldDeath_FiresNoOnDied_AndTheReviveRestoresHalfMaxHealth_AsHealing([Values(100, 101, 133)] int max)
        {
            _health.Initialize(max);
            int deaths = 0, holds = 0;
            var healed = new System.Collections.Generic.List<int>();
            _health.OnDied += () => deaths++;
            _health.OnHealed += healed.Add;
            _health.TryPreventDeath = () => { holds++; _health.HoldForRevive(); return true; };

            _health.TakeDamage(Lethal(max * 10));
            Assert.That(deaths, Is.EqualTo(0), "A held death is not a death: no OnDied (no Game Over).");
            Assert.That(_health.IsAwaitingRevive, Is.True);
            Assert.That(_health.CurrentHealth, Is.EqualTo(0));
            Assert.That(_owner.activeSelf, Is.True, "The player stays where it fell.");

            // A second lethal hit in the same frame (VS v1.6.0: "Fix for revives triggering twice"): nothing happens.
            _health.TakeDamage(Lethal(max * 10));
            _health.KillSilently();
            Assert.That(holds, Is.EqualTo(1), "One death asks for one revive.");
            _health.Heal(50);
            Assert.That(healed, Is.Empty, "Nothing heals a held death.");

            _health.ReviveFromDeath();
            int expected = Mathf.CeilToInt(max * 0.5f);
            Assert.That(_health.CurrentHealth, Is.EqualTo(expected), "50% of Max Health, rounded up.");
            Assert.That(HealthController.RevivalHealth(max), Is.EqualTo(expected));
            Assert.That(healed, Is.EqualTo(new[] { expected }), "The revive's health counts as healing, exactly once.");
            Assert.That(_health.IsAwaitingRevive, Is.False);
            Assert.That(deaths, Is.EqualTo(0));
        }

        [Test]
        public void Heal_ReportsOnlyTheHealthReallyGained()
        {
            _health.Initialize(100);
            var healed = new System.Collections.Generic.List<int>();
            _health.OnHealed += healed.Add;
            _health.TakeDamage(new DamagePacket(10, Vector3.zero, Vector3.zero));
            Assert.That(_health.CurrentHealth, Is.EqualTo(90));

            _health.Heal(30);
            _health.Heal(30); // already full: overheal is not healing
            _health.Heal(0);
            Assert.That(_health.CurrentHealth, Is.EqualTo(100));
            Assert.That(healed, Is.EqualTo(new[] { 10 }));
        }

        [Test]
        public void TheRevivalImmunity_BlocksHits_IsNotTheDashFlag_AndEndsOnlyItself()
        {
            Assert.That(HealthController.RevivalInvulnerabilitySeconds, Is.EqualTo(1.5f), "Soul Hunter's number for the wiki's 'large amount' (owner question).");
            _health.Initialize(100);
            _health.TryPreventDeath = () => { _health.HoldForRevive(); return true; };
            _health.TakeDamage(Lethal(1000));
            _health.ReviveFromDeath();

            Assert.That(_health.IsRevivalInvulnerable, Is.True);
            Assert.That(_health.IsInvincible, Is.False, "The revival does not use the dash's flag.");
            // A dash that starts and ends inside the window (PlayerController sets and clears IsInvincible) does not end it.
            _health.IsInvincible = true;
            _health.IsInvincible = false;
            _health.TakeDamage(Lethal(20));
            Assert.That(_health.CurrentHealth, Is.EqualTo(50), "Every hit is blocked during the revival's immunity.");

            _health.EndRevivalInvulnerability();
            _health.TakeDamage(Lethal(20));
            Assert.That(_health.CurrentHealth, Is.EqualTo(30), "Hits land again once it ends.");
        }

        [Test]
        public void AnImmediateRevive_ThroughTryPreventDeath_StillFiresNoOnDied()
        {
            // Shadow Kael's mirrored revivals (owner rule): revived inside TryPreventDeath, no Revive screen.
            _health.Initialize(100);
            int deaths = 0, healed = 0;
            _health.OnDied += () => deaths++;
            _health.OnHealed += amount => healed += amount;
            _health.TryPreventDeath = () => { _health.ReviveFromDeath(); return true; };
            _health.TakeDamage(Lethal(1000));
            Assert.That(deaths, Is.EqualTo(0));
            Assert.That(_health.CurrentHealth, Is.EqualTo(50));
            Assert.That(healed, Is.EqualTo(50));
            Assert.That(_owner.activeSelf, Is.True);
        }

        [Test]
        public void FinishDeath_RunsTheNormalDeathOnce()
        {
            _health.Initialize(100);
            int deaths = 0;
            _health.OnDied += () => deaths++;
            _health.TryPreventDeath = () => { _health.HoldForRevive(); return true; };
            _health.TakeDamage(Lethal(1000));
            _health.FinishDeath();
            _health.FinishDeath();
            Assert.That(deaths, Is.EqualTo(1));
            Assert.That(_owner.activeSelf, Is.False);
        }

        [Test]
        public void WithoutAHold_ALethalHitDies_AsBefore()
        {
            _health.Initialize(100);
            int deaths = 0;
            _health.OnDied += () => deaths++;
            _health.TryPreventDeath = () => false; // no Revival left
            _health.TakeDamage(Lethal(1000));
            Assert.That(deaths, Is.EqualTo(1));
            Assert.That(_health.IsAwaitingRevive, Is.False);
        }
    }
}
