using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// Pools the four light-source pickups that PickupPoolManager has no scene fields for (Soul Toll, Hellfire Breath,
    /// Luck Charm, Gilded Charm). Prefabs come from GameContentCatalog (built by ExtraPickupBuilder); when one is not
    /// wired yet a coloured sphere with the right component stands in, so a drop is never silently lost. Pooled objects
    /// return through AutoReturnToPool like every other pickup; destroyed ones (scene change) are skipped.
    /// </summary>
    public static class ExtraPickupPool
    {
        public enum Kind { ScreenWipe, FireBreath, LuckCharm, GildedCharm }

        private static readonly Dictionary<Kind, Stack<GameObject>> Pools = new Dictionary<Kind, Stack<GameObject>>();
        private static readonly Dictionary<Kind, GameObject> Fallbacks = new Dictionary<Kind, GameObject>();
        private static GameObject _fallbackHolder;

        /// <summary>The catalog prefab for a kind (null when not built yet).</summary>
        public static GameObject Prefab(Kind kind)
        {
            var catalog = GameContentCatalog.Load();
            if (catalog == null) return null;
            switch (kind)
            {
                case Kind.ScreenWipe: return catalog.ScreenWipeItem;
                case Kind.FireBreath: return catalog.FireBreathItem;
                case Kind.LuckCharm: return catalog.LuckCharmItem;
                case Kind.GildedCharm: return catalog.GildedCharmItem;
            }
            return null;
        }

        public static GameObject Spawn(Kind kind, Vector3 position)
        {
            var prefab = Prefab(kind);
            if (prefab == null) prefab = Fallback(kind);
            if (prefab == null) return null;

            if (!Pools.TryGetValue(kind, out var pool)) Pools[kind] = pool = new Stack<GameObject>();
            GameObject item = null;
            while (pool.Count > 0 && item == null)
            {
                var candidate = pool.Pop();
                if (candidate != null && !candidate.activeSelf) item = candidate;
            }
            if (item == null)
            {
                item = Object.Instantiate(prefab);
                SoulHunter.Gameplay.Combat.WeaponPoolManager.NoShadows(item);
                var autoReturn = item.GetComponent<AutoReturnToPool>();
                if (autoReturn == null) autoReturn = item.AddComponent<AutoReturnToPool>();
                autoReturn.TargetPool = pool;
            }

            item.transform.position = position;
            item.SetActive(true);
            var pickup = item.GetComponent<Pickup>();
            if (pickup != null) pickup.ResetPickup();
            return item;
        }

        /// <summary>Placeholder stand-in used only when GameContentCatalog has no prefab for the kind.</summary>
        private static GameObject Fallback(Kind kind)
        {
            if (Fallbacks.TryGetValue(kind, out var existing) && existing != null) return existing;
            if (_fallbackHolder == null)
            {
                _fallbackHolder = new GameObject("ExtraPickup_Fallbacks");
                _fallbackHolder.SetActive(false);
            }
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = kind + "_Pickup_Fallback";
            go.transform.SetParent(_fallbackHolder.transform, false);
            go.transform.localScale = Vector3.one * 0.6f;
            var collider = go.GetComponent<Collider>();
            collider.isTrigger = true;
            var renderer = go.GetComponent<Renderer>();
            switch (kind)
            {
                case Kind.ScreenWipe: go.AddComponent<ScreenWipePickup>(); renderer.material.color = new Color(0.85f, 0.95f, 1f); break;
                case Kind.FireBreath: go.AddComponent<FireBreathPickup>(); renderer.material.color = new Color(1f, 0.35f, 0.1f); break;
                case Kind.LuckCharm: go.AddComponent<LuckCharmPickup>(); renderer.material.color = new Color(0.3f, 0.9f, 0.35f); break;
                case Kind.GildedCharm: go.AddComponent<GildedCharmPickup>(); renderer.material.color = new Color(1f, 0.8f, 0.2f); break;
            }
            Fallbacks[kind] = go;
            return go;
        }
    }
}
