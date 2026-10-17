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

        public static IDamageable Of(Collider collider)
        {
            if (collider == null) return null;
            if (Cache.TryGetValue(collider, out var found) && found is Object alive && alive != null) return found;
            found = collider.GetComponentInParent<IDamageable>();
            if (found == null) { Cache.Remove(collider); return null; }
            if (Cache.Count >= MaxEntries) Cache.Clear(); // destroyed colliders never come back; start again
            Cache[collider] = found;
            return found;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad() => Cache.Clear();
    }
}
