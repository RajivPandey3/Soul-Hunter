using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// The IDamageable a collider belongs to, looked up once per collider. Every weapon hit used to search the collider's
    /// parents for the interface: a bomb or pool touching a crowd of 300 enemies made a hundred searches in one physics step
    /// (a 12-18 ms ProjectileDamage.OnTriggerEnter, 2026-10-17). Pooled enemies keep their colliders, so the cache stays
    /// valid; an entry whose component was destroyed is looked up again.
    /// </summary>
    public static class DamageableLookup
    {
        private const int MaxEntries = 4096;
        private static readonly Dictionary<Collider, IDamageable> Cache = new Dictionary<Collider, IDamageable>();

        /// <summary>
        /// The collider's IDamageable, or null when there is none or its health is already 0. A body that died this frame
        /// can still be on the screen's colliders for a frame or two (EnemyDeathQueue); a shot passing it keeps its pierce.
        /// </summary>
        public static IDamageable Of(Collider collider)
        {
            if (collider == null) return null;
            if (!Cache.TryGetValue(collider, out var found) || !(found is Object alive) || alive == null)
            {
                found = collider.GetComponentInParent<IDamageable>();
                if (found == null) { Cache.Remove(collider); return null; }
                if (Cache.Count >= MaxEntries) Cache.Clear(); // destroyed colliders never come back; start again
                Cache[collider] = found;
            }
            // Enemies only: a hero held at 0 for the Revive screen keeps its contacts, so the horde touching it still hurts
            // once the revival's immunity ends.
            return found is HealthController health && health.CurrentHealth <= 0 && health.Enemy != null ? null : found;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad() => Cache.Clear();
    }
}
