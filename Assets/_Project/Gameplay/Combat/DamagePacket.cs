using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    public enum DamageType { Normal, Holy, Fire, Poison, Shadow, Magic, Spectral }

    /// <summary>
    /// Learning Comment:
    /// Blueprint (combat-system.md) ke anusar ye zero-allocation struct hai.
    /// Ye attack ka sara data (kitna damage, kis direction mein) hold karta hai, bina Garbage Collection ke.
    /// </summary>
    public struct DamagePacket
    {
        public int Amount;
        public Vector3 HitPoint;
        public Vector3 KnockbackDirection;
        public DamageType Type;
        /// <summary>
        /// Continuous hazards (e.g. the Level 9 arena boundary, applied every frame) set this so the
        /// post-hit invulnerability window neither blocks them nor is started by them.
        /// </summary>
        public bool IgnoresHitInvulnerability;
        /// <summary>
        /// VS weapon knockback: how hard this hit reverses an enemy's movement (1 = a normal weapon). 0 or less means
        /// the default 1, so packets built without it still knock back normally.
        /// </summary>
        public float WeaponKnockback;
        public float KnockbackOrDefault => WeaponKnockback > 0f ? WeaponKnockback : 1f;
        public DamagePacket(int amount, Vector3 hitPoint, Vector3 knockbackDirection)
            : this(amount, hitPoint, knockbackDirection, DamageType.Normal) { }

        public DamagePacket(int amount, Vector3 hitPoint, Vector3 knockbackDirection, DamageType type)
        {
            Amount = amount;
            HitPoint = hitPoint;
            KnockbackDirection = knockbackDirection;
            Type = type;
            IgnoresHitInvulnerability = false;
            WeaponKnockback = 0f; // the default weapon knockback (1)
        }
    }
}
