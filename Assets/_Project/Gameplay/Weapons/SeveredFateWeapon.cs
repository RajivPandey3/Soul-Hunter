using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Severed Fate: Soul Hunter's evolution of the Runetracer (owner's name, 2026-10-01), built on VS NO FUTURE
    /// (vampire.survivors.wiki/w/NO_FUTURE, fetched 2026-10-01): Runetracer at max level + Armor. 30 damage, 1 laser,
    /// Speed 280%, 3.0 s Duration, "its Cooldown is extended by its Duration of 3 seconds to 4 seconds total", infinite
    /// pierce, bounces off the edge of the screen, 0.5 s hitbox delay. "When retaliating, it creates an explosion around
    /// the character and can retaliate every 0.5 seconds. Each Armor also increase the retaliatory explosion damage by 10%
    /// additively." The explosion deals the laser's damage (the wiki gives no other number) within 3 units x Area (Soul
    /// Hunter's radius). A boss-held copy does not retaliate.
    /// </summary>
    public class SeveredFateWeapon : AutoAttackWeapon
    {
        public override float LimitBreakBaseDuration => VsDuration;
        protected override SoulHunter.Gameplay.Audio.WeaponSound FireSound => SoulHunter.Gameplay.Audio.WeaponSound.Magic;

        public GameObject LaserPrefab;

        public const float VsDamage = 30f;
        public const float VsCooldown = 1f;
        public const float VsDuration = 3f;
        /// <summary>The Runetracer's base speed (8) is VS 100%; NO FUTURE flies at 280%.</summary>
        public const float LaserSpeed = 8f * 2.8f;
        public const float VsHitboxDelay = 0.5f;
        public const float RetaliationInterval = 0.5f;
        public const float RetaliationRadius = 3f;

        public override int MaxLevel => 1;
        public override void LevelUp() { }

        private HealthController _holderHealth;
        private float _nextRetaliation;
        private readonly Collider[] _hits = new Collider[64];

        /// <summary>Retaliation explosions so far (tests).</summary>
        public int Retaliations { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            DamageAmount = VsDamage;
            AttackCooldown = VsCooldown;
        }

        private void OnEnable()
        {
            _holderHealth = GetComponentInParent<HealthController>();
            if (_holderHealth != null) _holderHealth.OnDamaged += Retaliate;
        }

        private void OnDisable()
        {
            if (_holderHealth != null) _holderHealth.OnDamaged -= Retaliate;
            _holderHealth = null;
        }

        protected override void Attack()
        {
            if (LaserPrefab == null) return;
            // VS: the cooldown runs after the Duration ("extended by its Duration ... to 4 seconds total").
            SetNextAttackIn((VsCooldown + VsDuration) * CooldownMultiplier);
            int count = 1 + ExtraAmount;
            for (int i = 0; i < count; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, i * 360f / count, 0f) * AimDirection(RandomFlatDirection());
                GameObject laser = WeaponPoolManager.Instance != null
                    ? WeaponPoolManager.Instance.GetFromPool(LaserPrefab, transform.position, Quaternion.identity)
                    : Instantiate(LaserPrefab, transform.position, Quaternion.identity);
                if (laser == null) continue;
                ApplyArea(laser, LaserPrefab);
                var rb = laser.GetComponent<Rigidbody>();
                if (rb == null) rb = laser.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = false;
                rb.constraints = RigidbodyConstraints.FreezePositionY;
                rb.linearVelocity = dir * LaserSpeed * SpeedMultiplier;
                var bounce = laser.GetComponent<ScreenBounce>();
                if (bounce == null) bounce = laser.AddComponent<ScreenBounce>();
                bounce.enabled = true;

                var damage = laser.GetComponent<ProjectileDamage>();
                if (damage == null) damage = laser.AddComponent<ProjectileDamage>();
                damage.DamageAmount = ScaledDamage(VsDamage);
                damage.DamageType = DamageType.Magic;
                damage.SourceWeaponName = "Severed Fate";
                AimAtHostiles(laser);

                float seconds = VsDuration * DurationMultiplier;
                var lifetime = laser.GetComponent<PooledLifetime>();
                if (lifetime != null) lifetime.Arm(seconds);
                else Destroy(laser, seconds);
            }
        }

        /// <summary>Retaliation damage: the laser's damage, +10% per point of Armor (VS, additive).</summary>
        public float RetaliationDamage => ScaledDamage(VsDamage) * (1f + 0.1f * Mathf.Clamp(OwnerStats != null ? OwnerStats.Armor : 0, 0, 50));

        private void Retaliate()
        {
            if (!isActiveAndEnabled || HeldByEnemy || Time.time < _nextRetaliation) return;
            _nextRetaliation = Time.time + RetaliationInterval;
            Retaliations++;
            Vector3 at = transform.position;
            SoulHunter.Gameplay.VFX.VFXPoolManager.Play(SoulHunter.Gameplay.VFX.VfxKind.Explosion, at);
            int count = UnityEngine.Physics.OverlapSphereNonAlloc(at, RetaliationRadius * AreaMultiplier, _hits, LayerMask.GetMask("Enemy"), QueryTriggerInteraction.Collide);
            int amount = Mathf.RoundToInt(RetaliationDamage);
            for (int i = 0; i < count; i++)
            {
                if (_hits[i] == null || !_hits[i].CompareTag("Enemy")) continue;
                var health = _hits[i].GetComponentInParent<HealthController>();
                if (health == null) continue;
                health.TakeDamage(new DamagePacket(amount, health.transform.position, (health.transform.position - at).normalized, DamageType.Magic).WithKnockback(Knockback));
            }
        }
    }
}
