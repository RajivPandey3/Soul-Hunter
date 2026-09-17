using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
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

        public DamagePacket(int amount, Vector3 hitPoint, Vector3 knockbackDirection)
        {
            Amount = amount;
            HitPoint = hitPoint;
            KnockbackDirection = knockbackDirection;
        }
    }
}
