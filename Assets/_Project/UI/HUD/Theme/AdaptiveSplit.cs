using UnityEngine;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Two areas of one screen that sit side by side when the screen is wide and one above the other when it is tall
    /// (owner, 2026-10-10: the same screens in landscape and portrait, "UI elements auto adjust themselves", no second
    /// layout). The screen keeps one set of content; only these two rects move.
    ///
    /// Wide (landscape): <see cref="First"/> takes the left <see cref="WideShare"/> of the width, <see cref="Second"/> the rest.
    /// Tall (portrait):  <see cref="First"/> takes the top <see cref="TallShare"/> of the height, <see cref="Second"/> the rest
    /// (or the bottom, with <see cref="SecondOnTopWhenTall"/>).
    /// "Tall" means this rect is taller than it is wide times <see cref="TallWhenAspectBelow"/>.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class AdaptiveSplit : MonoBehaviour
    {
        public RectTransform First;
        public RectTransform Second;
        [Range(0.1f, 0.9f)] public float WideShare = 0.5f;
        [Range(0.1f, 0.9f)] public float TallShare = 0.5f;
        public bool SecondOnTopWhenTall;
        [Tooltip("Width / height under this counts as tall (1 = portrait).")]
        public float TallWhenAspectBelow = 1f;
        [Tooltip("Space between the two areas, in canvas units (negative = the theme's button gap).")]
        public float Gap = -1f;

        private Vector2 _last = new Vector2(-1f, -1f);

        /// <summary>True when the areas are stacked (the rect is tall).</summary>
        public bool IsTall { get; private set; }

        /// <summary>Makes <paramref name="parent"/> split into <paramref name="first"/> and <paramref name="second"/>.</summary>
        public static AdaptiveSplit Between(RectTransform parent, RectTransform first, RectTransform second, float wideShare, float tallShare)
        {
            var split = parent.GetComponent<AdaptiveSplit>() ?? parent.gameObject.AddComponent<AdaptiveSplit>();
            split.First = first;
            split.Second = second;
            split.WideShare = wideShare;
            split.TallShare = tallShare;
            split.Layout(force: true);
            return split;
        }

        public static bool IsTallAspect(Vector2 size, float threshold) => size.y > 0f && size.x / size.y < threshold;

        private void OnEnable() => Layout(force: true);

        private void LateUpdate() => Layout(force: false);

        public void Layout(bool force)
        {
            var rect = (RectTransform)transform;
            Vector2 size = rect.rect.size;
            if (!force && size == _last) return;
            _last = size;
            IsTall = IsTallAspect(size, TallWhenAspectBelow);
            float gap = Gap >= 0f ? Gap : (UiThemeAsset.Active != null ? UiThemeAsset.Active.Layouts.ButtonGap : 12f);
            float half = gap * 0.5f;
            if (!IsTall)
            {
                Place(First, new Vector2(0f, 0f), new Vector2(WideShare, 1f), new Vector2(0f, 0f), new Vector2(-half, 0f));
                Place(Second, new Vector2(WideShare, 0f), new Vector2(1f, 1f), new Vector2(half, 0f), Vector2.zero);
            }
            else
            {
                float topShare = SecondOnTopWhenTall ? 1f - TallShare : TallShare;
                RectTransform top = SecondOnTopWhenTall ? Second : First, bottom = SecondOnTopWhenTall ? First : Second;
                Place(top, new Vector2(0f, 1f - topShare), new Vector2(1f, 1f), new Vector2(0f, half), Vector2.zero);
                Place(bottom, new Vector2(0f, 0f), new Vector2(1f, 1f - topShare), Vector2.zero, new Vector2(0f, -half));
            }
        }

        private static void Place(RectTransform r, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            if (r == null) return;
            r.anchorMin = min;
            r.anchorMax = max;
            r.offsetMin = offsetMin;
            r.offsetMax = offsetMax;
        }
    }
}
