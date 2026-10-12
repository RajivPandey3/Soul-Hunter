using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Keeps the HUD's overlays (the Omens wheel, Blind Fate's pick, the story card) inside the safe area and clear of the
    /// touch pads and the HUD's own labels, on every screen (owner, 2026-10-10: landscape and portrait from the same
    /// screens). Everything is measured in the space of the canvas the overlay sits on, in canvas units.
    /// </summary>
    public static class HudClearance
    {
        /// <summary>The air kept between an overlay and what it moves away from (canvas units).</summary>
        public const float Gap = 12f;

        private static TouchControlsUI _touch;

        /// <summary><paramref name="rect"/>'s box in <paramref name="space"/>'s local units.</summary>
        public static Rect RectIn(RectTransform rect, RectTransform space)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 a = space.InverseTransformPoint(corners[0]), b = space.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        /// <summary><paramref name="space"/>'s safe area (notch, gesture bar) in its own units, inset by <paramref name="margin"/>.</summary>
        public static Rect SafeRect(RectTransform space, float margin)
        {
            Rect r = space.rect, s = UiScreen.SafeAreaNormalized;
            return Rect.MinMaxRect(r.xMin + s.xMin * r.width + margin, r.yMin + s.yMin * r.height + margin,
                r.xMin + s.xMax * r.width - margin, r.yMin + s.yMax * r.height - margin);
        }

        /// <summary>The touch stick (where it rests), Dash and Pause, when the touch controls are shown; none otherwise.</summary>
        public static List<Rect> TouchPads(RectTransform space)
        {
            var pads = new List<Rect>();
            if (!TouchControlsUI.ShouldShow) return pads;
            if (_touch == null) _touch = Object.FindFirstObjectByType<TouchControlsUI>();
            if (_touch == null || _touch.Root == null) return pads;
            foreach (Transform child in _touch.Root.transform)
            {
                // The floating stick's catch area is invisible and may be covered; only the pads themselves count.
                if (child.name == "StickZone" || !(child is RectTransform pad)) continue;
                pads.Add(RectIn(pad, space));
            }
            return pads;
        }

        /// <summary>True when the boxes overlap or come closer than <paramref name="gap"/>.</summary>
        public static bool Near(Rect a, Rect b, float gap) =>
            a.xMin < b.xMax + gap - 0.01f && b.xMin < a.xMax + gap - 0.01f && a.yMin < b.yMax + gap - 0.01f && b.yMin < a.yMax + gap - 0.01f;

        public static bool Inside(Rect inner, Rect outer) =>
            inner.xMin >= outer.xMin - 0.5f && inner.yMin >= outer.yMin - 0.5f && inner.xMax <= outer.xMax + 0.5f && inner.yMax <= outer.yMax + 0.5f;

        /// <summary>
        /// <paramref name="box"/> kept inside <paramref name="bounds"/> and clear of every obstacle: unchanged when it already
        /// is, otherwise moved (same size) to the nearest free spot found by sliding up, left, down or right past what is in
        /// the way. When no spot is free it stays inside the bounds where it was.
        /// </summary>
        public static Rect Clear(Rect box, Rect bounds, IList<Rect> obstacles, float gap)
        {
            box = Clamp(box, bounds);
            if (FirstNear(box, obstacles, gap) == null) return box;
            Rect best = box;
            float bestCost = float.MaxValue;
            var directions = new[] { Vector2.up, Vector2.left, Vector2.down, Vector2.right };
            foreach (var dir in directions)
            {
                Rect c = box;
                for (int i = 0; i <= obstacles.Count; i++)
                {
                    var hit = FirstNear(c, obstacles, gap);
                    if (hit == null) break;
                    Rect h = hit.Value;
                    if (dir == Vector2.up) c.y = h.yMax + gap;
                    else if (dir == Vector2.down) c.y = h.yMin - gap - c.height;
                    else if (dir == Vector2.left) c.x = h.xMin - gap - c.width;
                    else c.x = h.xMax + gap;
                }
                if (!Inside(c, bounds) || FirstNear(c, obstacles, gap) != null) continue;
                float cost = (c.position - box.position).sqrMagnitude;
                if (cost < bestCost) { bestCost = cost; best = c; }
            }
            return best;
        }

        private static Rect? FirstNear(Rect box, IList<Rect> obstacles, float gap)
        {
            foreach (var o in obstacles) if (Near(box, o, gap)) return o;
            return null;
        }

        /// <summary>Moves <paramref name="box"/> (same size) inside <paramref name="bounds"/> as far as it fits.</summary>
        public static Rect Clamp(Rect box, Rect bounds)
        {
            box.x = Mathf.Clamp(box.x, bounds.xMin, Mathf.Max(bounds.xMin, bounds.xMax - box.width));
            box.y = Mathf.Clamp(box.y, bounds.yMin, Mathf.Max(bounds.yMin, bounds.yMax - box.height));
            return box;
        }
    }
}
