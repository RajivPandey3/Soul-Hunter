using UnityEngine;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// A row of themed buttons or tabs at the theme's one size (owner, 2026-10-07): its children sit side by side, centred,
    /// with the theme's button gap. When its parent is too narrow for the whole row (a narrow window), the row shrinks as one
    /// piece, like the screen's own scaling, so the controls keep their standard proportions and never overlap.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class ThemedRow : MonoBehaviour
    {
        private float _lastParentWidth = -1f;

        private void OnEnable()
        {
            UiThemeAsset.Changed += Layout;
            Layout();
        }

        private void OnDisable() => UiThemeAsset.Changed -= Layout;

        private void LateUpdate()
        {
            var parent = transform.parent as RectTransform;
            if (parent != null && !Mathf.Approximately(parent.rect.width, _lastParentWidth)) Layout();
        }

        /// <summary>Places the children and fits the row to its parent's width.</summary>
        public void Layout()
        {
            if (this == null) return;
            var row = (RectTransform)transform;
            var parent = row.parent as RectTransform;
            var theme = UiThemeAsset.Active;
            float gap = theme.Layouts.ButtonGap;

            float total = 0f;
            int count = 0;
            foreach (RectTransform child in row)
            {
                if (!child.gameObject.activeSelf) continue;
                total += Width(child, theme);
                count++;
            }
            if (count == 0) return;
            total += gap * (count - 1);

            float x = -total * 0.5f;
            foreach (RectTransform child in row)
            {
                if (!child.gameObject.activeSelf) continue;
                float w = Width(child, theme);
                child.anchorMin = child.anchorMax = new Vector2(0.5f, 0.5f);
                child.pivot = new Vector2(0.5f, 0.5f);
                child.anchoredPosition = new Vector2(x + w * 0.5f, 0f);
                x += w + gap;
            }

            row.sizeDelta = new Vector2(total, row.sizeDelta.y);
            _lastParentWidth = parent != null ? parent.rect.width : -1f;
            float room = parent != null ? parent.rect.width - gap : total; // a gap of air at the ends
            float scale = room > 0f && total > room ? room / total : 1f;
            row.localScale = new Vector3(scale, scale, 1f);
        }

        private static float Width(RectTransform child, UiThemeAsset theme)
        {
            var element = child.GetComponent<ThemedElement>();
            var size = element != null ? element.StandardSize(theme) : null;
            return size.HasValue ? size.Value.x : child.rect.width;
        }
    }
}
