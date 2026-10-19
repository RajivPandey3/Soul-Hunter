using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.AI;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Requiem of Souls: Soul Hunter's evolution of Song of Mana (owner's name, 2026-10-01), built on VS Mannajja
    /// (vampire.survivors.wiki/w/Mannajja, fetched 2026-10-01): Song of Mana at max level + Skull O'Maniac. 40 damage,
    /// Area 6, 2.0 s Duration, 4.5 s cooldown, "Ignores Amount and Speed"; "a pulsing wave ... outwards from the player"
    /// that follows them; "a 1 second Hitbox Delay"; each hit slows the enemy by 5% more, down to 20% of its speed (not
    /// debuff-resistant enemies). The wave reaches 6 units x Area from the player (Soul Hunter's reading of Area 6) and
    /// pulses every 0.1 s (the infobox interval). Its look is the approved VFX v2 magic circle (the level-up effect),
    /// shown every 0.5 s while the wave is out.
    /// </summary>
    public class RequiemOfSoulsWeapon : AutoAttackWeapon
    {
        public override float LimitBreakBaseDuration => VsDuration;
        protected override bool ShowsAttackPose => false; // an aura around the player
        protected override SoulHunter.Gameplay.Audio.WeaponSound FireSound => SoulHunter.Gameplay.Audio.WeaponSound.Magic;

        public const float VsDamage = 40f;
        public const float VsDuration = 2f;
        public const float VsCooldown = 4.5f;
        public const float VsInterval = 0.1f;
        public const float VsHitboxDelay = 1f;
        public const float WaveRadius = 6f;
        public const float SlowStep = 0.05f;
        public const float SlowFloor = 0.2f;

        public override int MaxLevel => 1;
        public override void LevelUp() { }

        private float _activeUntil = -1f, _nextPulse, _nextRing;
        private readonly Dictionary<HealthController, float> _nextHit = new Dictionary<HealthController, float>();
        private Collider[] _hits = new Collider[128];
        private static int _enemyMask, _playerMask;

        public bool WaveActive => Time.time < _activeUntil;
        public float Radius => WaveRadius * AreaMultiplier;
        public override float ArcanaZoneBaseDamage => VsDamage;

        protected override void Awake()
        {
            base.Awake();
            DamageAmount = VsDamage;
            AttackCooldown = VsCooldown;
        }

        protected override void Attack()
        {
            _activeUntil = Time.time + VsDuration * DurationMultiplier;
            SetNextAttackIn((VsDuration * DurationMultiplier) + VsCooldown * CooldownMultiplier);
        }

        protected override void Update()
        {
            base.Update();
            if (!WaveActive) return;
            if (Time.time >= _nextRing)
            {
                _nextRing = Time.time + 0.5f;
                SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.LevelUp, transform.position, Quaternion.identity, Radius * 0.4f);
            }
            if (Time.time < _nextPulse) return;
            _nextPulse = Time.time + VsInterval;
            if (_nextHit.Count > 1024) _nextHit.Clear(); // pooled enemies come back; old entries are not needed
            Pulse();
        }

        private void Pulse()
        {
            string hostileTag = HostileTag;
            // Only the hostile's layer (CMB-07): with every layer (~0) the 128 slots filled with gems, shots and props, and a
            // late crowd of 300 left part of it untouched. Enemies, Soul Lanterns and rail carts are on the Enemy layer; a
            // boss-held copy (Shadow Kael) looks for the player. A full buffer grows (rarely, never in a steady state) and asks
            // again, as the magnet sweep does.
            if (_enemyMask == 0) { _enemyMask = LayerMask.GetMask("Enemy"); _playerMask = LayerMask.GetMask("Player"); }
            int mask = HeldByEnemy ? _playerMask : _enemyMask;
            float radius = Radius;
            int count = UnityEngine.Physics.OverlapSphereNonAlloc(transform.position, radius, _hits, mask, QueryTriggerInteraction.Collide);
            while (count == _hits.Length)
            {
                _hits = new Collider[_hits.Length * 2];
                count = UnityEngine.Physics.OverlapSphereNonAlloc(transform.position, radius, _hits, mask, QueryTriggerInteraction.Collide);
            }
            int amount = Mathf.RoundToInt(ScaledDamage(VsDamage));
            for (int i = 0; i < count; i++)
            {
                var col = _hits[i];
                if (col == null || !col.CompareTag(hostileTag)) continue;
                var health = col.GetComponentInParent<HealthController>();
                if (health == null || (_nextHit.TryGetValue(health, out float next) && Time.time < next)) continue;
                _nextHit[health] = Time.time + VsHitboxDelay;
                Hit(health, new DamagePacket(amount, health.transform.position, (health.transform.position - transform.position).normalized, DamageType.Magic).WithKnockback(Knockback));
                var enemy = health.GetComponent<EnemyController>();
                if (enemy != null) enemy.ApplyLingeringSlow(SlowStep, SlowFloor);
            }
        }
    }
}
