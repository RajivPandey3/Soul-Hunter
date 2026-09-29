using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Gameplay.Pickups
{
    /// <summary>
    /// VS Pentagram (vampire.survivors.wiki/w/Pentagram): the glyph erases "every enemy, their drops, and all items on the
    /// screen, except for stage items". Gems, coins, bags, chickens, magnets, freezes and chests in the camera view are
    /// removed (pooled ones go back to their pools).
    /// </summary>
    public static class GroundItemEraser
    {
        private static readonly List<ITickedPickup> Ticked = new List<ITickedPickup>();
        private static readonly List<GameObject> Doomed = new List<GameObject>();

        /// <summary>
        /// VS Pentagram item roll: x ~ U[0,1] rounded to 2 decimals; items are erased when x / totalLuck is above the
        /// keep threshold (10% at level 1, raised at levels 3, 5 and 7).
        /// </summary>
        public static bool ErasesItems(float keepThreshold, float totalLuck, float roll01)
        {
            float x = Mathf.Round(Mathf.Clamp01(roll01) * 100f) / 100f;
            return x / Mathf.Max(0.0001f, totalLuck) > keepThreshold;
        }

        /// <summary>Erases every item in <paramref name="view"/>'s screen; returns how many went.</summary>
        public static int EraseOnScreen(Camera view)
        {
            if (view == null) return 0;
            Doomed.Clear();
            foreach (var gem in XPGem.ActiveGems) if (gem != null) Consider(gem.gameObject, view);
            PickupTicker.CopyTo(Ticked);
            foreach (var pickup in Ticked) if (pickup is Component c && c != null) Consider(c.gameObject, view);
            foreach (var chest in Object.FindObjectsByType<ChestPickup>(FindObjectsSortMode.None)) Consider(chest.gameObject, view);
            foreach (var go in Doomed) go.SetActive(false);
            return Doomed.Count;
        }

        public static bool OnScreen(Camera view, Vector3 position)
        {
            Vector3 v = view.WorldToViewportPoint(position);
            return v.z > 0f && v.x >= 0f && v.x <= 1f && v.y >= 0f && v.y <= 1f;
        }

        private static void Consider(GameObject go, Camera view)
        {
            if (!go.activeInHierarchy || Doomed.Contains(go)) return;
            if (go.GetComponent<StageItemMark>() != null || go.GetComponent<StageItemPickup>() != null) return;
            if (OnScreen(view, go.transform.position)) Doomed.Add(go);
        }
    }
}
