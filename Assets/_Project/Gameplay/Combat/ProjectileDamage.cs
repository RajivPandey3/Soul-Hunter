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

        private void OnTriggerEnter(Collider other) => TryHit(other);

        /// <summary>Hits <paramref name="other"/> if it is a target (and the hit gate allows it). True when damage was dealt.</summary>
        public bool TryHit(Collider other)
        {
            var projectile = GetComponent<Projectile>();
            if (other == null || !isActiveAndEnabled || (projectile != null && projectile.enabled)) return false;
            if (!other.CompareTag(TargetTag)) return false;
            var damageable = DamageableLookup.Of(other);
            if (damageable == null) return false;
            if (HitGate != null && !HitGate(damageable as Component)) return false;
            Vector3 knockback = (other.transform.position - transform.position).normalized;
            int dmg = Mathf.RoundToInt(DamageAmount);
            using (DamageSource.From(SourceWeapon, SourceWeaponName != "Unknown" ? SourceWeaponName : null))
                damageable.TakeDamage(new DamagePacket(dmg, transform.position, knockback, DamageType).WithKnockback(Knockback));
            return true;
        }
    }
}
