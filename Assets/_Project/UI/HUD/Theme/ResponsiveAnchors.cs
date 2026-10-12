using UnityEngine;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Moves elements authored for the 1920x1080 layout (prefab and scene UI) onto the corner they belong to, so they stay
    /// in place on any canvas, landscape or portrait (owner, 2026-10-10: the same screens adjust themselves). Before, the
    /// HUD's kill and gold labels hung -800 units from a box at the screen centre and the timer +793 from the top centre:
    /// on a 1132-unit-wide phone canvas they fell off the screen. The prefab files are not edited.
    /// </summary>
    public static class ResponsiveAnchors
    {
        public static readonly Vector2 Reference = new Vector2(1920f, 1080f);

        /// <summary>
        /// Where <paramref name="rect"/> sits in the 1920x1080 layout it was authored for (bottom-left origin), by the
        /// same anchor and offset rules uGUI uses, walking up to its canvas.
        /// </summary>
        public static Rect ReferenceRect(RectTransform rect)
        {
            var parent = rect.parent as RectTransform;
            Rect p = parent == null || parent.GetComponent<Canvas>() != null && parent.GetComponent<Canvas>().isRootCanvas
                ? new Rect(Vector2.zero, Reference)
                : ReferenceRect(parent);
            Vector2 min = p.min + Vector2.Scale(rect.anchorMin, p.size) + rect.offsetMin;
            Vector2 max = p.min + Vector2.Scale(rect.anchorMax, p.size) + rect.offsetMax;
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        /// <summary>The corner (0/1 on each axis) nearest the rect's centre in the reference layout.</summary>
        public static Vector2 NearestCorner(Rect reference)
        {
            Vector2 c = reference.center;
            return new Vector2(c.x < Reference.x * 0.5f ? 0f : 1f, c.y < Reference.y * 0.5f ? 0f : 1f);
        }

        /// <summary>
        /// Re-parents <paramref name="rect"/> under <paramref name="root"/> (a full-screen or safe-area rect) anchored to its
        /// nearest corner, keeping its size and its distance from that corner in the reference layout.
        /// </summary>
        public static void PinToNearestCorner(RectTransform rect, RectTransform root)
        {
            if (rect == null || root == null) return;
            Rect r = ReferenceRect(rect);
            Vector2 corner = NearestCorner(r);
            Vector2 cornerPoint = Vector2.Scale(corner, Reference);
            Vector2 size = r.size;
            Vector2 pivot = rect.pivot;
            Vector2 pivotPoint = r.min + Vector2.Scale(pivot, size);
            rect.SetParent(root, false);
            rect.anchorMin = rect.anchorMax = corner;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = pivotPoint - cornerPoint;
        }

        /// <summary>A full-screen child of <paramref name="canvas"/> fitted to the safe area, made once by name.</summary>
        public static RectTransform SafeRoot(Canvas canvas, string name = "SafeArea")
        {
            if (canvas == null) return null;
            var root = canvas.transform.Find(name) as RectTransform;
            if (root == null)
            {
                root = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
                root.SetParent(canvas.transform, false);
                root.SetAsFirstSibling(); // under the menus drawn on the same canvas
            }
            SafeAreaFitter.Fit(root);
            return root;
        }
    }
}
