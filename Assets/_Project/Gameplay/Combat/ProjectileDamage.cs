using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Learning Comment:
    /// Player ke projectiles (Axe, Magic Wand) ke liye damage script.
    /// TouchDamage siraf dushmano ke liye tha (jo player ko marta hai).
    /// </summary>
    public class ProjectileDamage : MonoBehaviour
    {
        /// <summary>The knockback of the weapon that spawned this (VS infobox "knockback"); negative = the default 1.</summary>
        [System.NonSerialized] public float Knockback = -1f;
        public float DamageAmount;
        public string TargetTag = "Enemy";
        public DamageType DamageType = DamageType.Normal;

        [Tooltip("Damage report ke liye hathiyar ka naam (e.g. 'Axe')")]
        public string SourceWeaponName = "Unknown";
        /// <summary>The weapon that spawned this (set by AutoAttackWeapon.AimAtHostiles), credited on the Results screen.</summary>
        [System.NonSerialized] public AutoAttackWeapon SourceWeapon;
        /// <summary>When set (HitboxDelay), a target is hit only if this allows it.</summary>
        [System.NonSerialized] public System.Func<Component, bool> HitGate;

        private void OnCollisionEnter(Collision collision)
        {
            var projectile = GetComponent<Projectile>();
            if (!isActiveAndEnabled || (projectile != null && projectile.enabled)) return;
            if (collision.gameObject.CompareTag(TargetTag))
            {
                var damageable = DamageableLookup.Of(collision.collider);
                if (damageable != null && (HitGate == null || HitGate(damageable as Component)))
                {
                    Vector3 knockback = (collision.transform.position - transform.position).normalized;
                    int dmg = Mathf.RoundToInt(DamageAmount);
                    using (DamageSource.From(SourceWeapon, SourceWeaponName != "Unknown" ? SourceWeaponName : null))
                        damageable.TakeDamage(new DamagePacket(dmg, collision.contacts[0].point, knockback, DamageType).WithKnockback(Knockback));
                }
            }
        }

        /// <summary>
        /// Measuring only (AutoplayBot -sh-autoplay-count-triggers): trigger enters counted by this object, the other collider's
        /// layer and whether it dealt damage. A Level 3 frame had about 1,000 enters and only 20 hits (2026-10-17). Null = off.
        /// </summary>
        public static System.Collections.Generic.Dictionary<string, int> EnterStats;

        private static readonly Unity.Profiling.ProfilerMarker FindMarker = new("Hit.FindTarget");
        private static readonly Unity.Profiling.ProfilerMarker TakeDamageMarker = new("Hit.TakeDamage");

        private void OnTriggerEnter(Collider other)
        {
            bool hit = TryHit(other);
            if (EnterStats == null || other == null) return;
            string key = name + " > " + LayerMask.LayerToName(other.gameObject.layer) + (hit ? " hit" : " miss");
            EnterStats.TryGetValue(key, out int count);
            EnterStats[key] = count + 1;
        }

        /// <summary>Hits <paramref name="other"/> if it is a target (and the hit gate allows it). True when damage was dealt.</summary>
        public bool TryHit(Collider other)
        {
            IDamageable damageable;
            bool timing = HitTiming.Enabled;
            long started = timing ? HitTiming.Now : 0;
            using (FindMarker.Auto())
            {
                var projectile = GetComponent<Projectile>();
                if (other == null || !isActiveAndEnabled || (projectile != null && projectile.enabled)) return false;
                if (!other.CompareTag(TargetTag)) return false;
                damageable = DamageableLookup.Of(other);
                if (damageable == null) return false;
                if (HitGate != null && !HitGate(damageable as Component)) return false;
            }
            long found = timing ? HitTiming.Now : 0;
            var health = timing ? damageable as HealthController : null;
            int healthBefore = health != null ? health.CurrentHealth : 0;
            Vector3 knockback = (other.transform.position - transform.position).normalized;
            int dmg = Mathf.RoundToInt(DamageAmount);
            using (DamageSource.From(SourceWeapon, SourceWeaponName != "Unknown" ? SourceWeaponName : null))
                using (TakeDamageMarker.Auto()) damageable.TakeDamage(new DamagePacket(dmg, transform.position, knockback, DamageType).WithKnockback(Knockback));
            if (timing)
            {
                long done = HitTiming.Now;
                HitTiming.FindTicks += found - started; HitTiming.FindCalls++;
                if (health != null && health.CurrentHealth == healthBefore) { HitTiming.EmptyTicks += done - found; HitTiming.EmptyCalls++; }
                else { HitTiming.DamagingTicks += done - found; HitTiming.DamagingCalls++; }
            }
            return true;
        }
    }
}
