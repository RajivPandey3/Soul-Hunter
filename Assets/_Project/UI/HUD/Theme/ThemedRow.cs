using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// A row of themed buttons or tabs at the theme's one size (owner, 2026-10-07): its children sit side by side, centred,
    /// with the theme's button gap. When its parent is too narrow for the whole row (a portrait phone, a narrow window), the
    /// row wraps onto more lines, each centred, so every control keeps its full size and touch target (owner, 2026-10-10:
    /// "the same screen's UI elements auto adjust themselves"). Only a single control wider than its parent is scaled down.
    /// The row's own height grows with its lines; its centre stays where its screen placed it.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class ThemedRow : MonoBehaviour
    {
        private float _lastParentWidth = -1f;

        /// <summary>How many lines the row took at its last layout.</summary>
        public int Lines { get; private set; } = 1;

        private void OnEnable()
        {
            UiThemeAsset.Changed += Layout;
            UiScreen.Changed += Layout;
            Layout();
        }

        private void OnDisable()
        {
            UiThemeAsset.Changed -= Layout;
            UiScreen.Changed -= Layout;
        }

        private int _lastShown = -1, _lastPadding = -1;

        private void LateUpdate()
        {
            var parent = transform.parent as RectTransform;
            int shown = 0;
            foreach (Transform child in transform) if (child.gameObject.activeSelf) shown++;
            var group = parent != null ? parent.GetComponent<UnityEngine.UI.LayoutGroup>() : null;
            int padding = group != null ? group.padding.left + group.padding.right : 0;
            int labels = LabelStamp();
            if (shown != _lastShown || padding != _lastPadding || labels != _lastLabels || (parent != null && !Mathf.Approximately(parent.rect.width, _lastParentWidth)))
            {
                _lastShown = shown;
                _lastPadding = padding;
                Layout();
            }
        }

        private int _lastLabels;

        /// <summary>Changes when a child's label does: on a touch screen a tab's width follows its label.</summary>
        private int LabelStamp()
        {
            if (!UiScreen.IsTouch) return 0;
            int stamp = 17;
            foreach (Transform child in transform)
            {
                var element = child.GetComponent<ThemedElement>();
                string text = element != null ? element.LabelText : null;
                stamp = stamp * 31 + (text != null ? text.GetHashCode() : 0);
            }
            return stamp;
        }

        /// <summary>
        /// The row's tabs share one width, the widest label's (a row of unequal chips reads as a mistake): each tab is
        /// told the width and takes it.
        /// </summary>
        private static void ShareTabWidth(List<RectTransform> items, UiThemeAsset theme)
        {
            var tabs = new List<ThemedElement>();
            float widest = 0f;
            foreach (var item in items)
            {
                var element = item.GetComponent<ThemedElement>();
                if (element == null || element.Size != ThemedElement.SizeRole.Tab) continue;
                element.RowWidth = 0f;
                if (element.StandardSize(theme) is Vector2 own) { tabs.Add(element); widest = Mathf.Max(widest, own.x); }
            }
            foreach (var tab in tabs)
            {
                tab.RowWidth = widest;
                tab.FitSize(theme);
            }
        }

        /// <summary>Places the children in as many centred lines as the parent's width needs.</summary>
        public void Layout()
        {
            if (this == null) return;
            var row = (RectTransform)transform;
            var parent = row.parent as RectTransform;
            var theme = UiThemeAsset.Active;
            float gap = theme.Layouts.ButtonGap;

            var items = new List<RectTransform>();
            foreach (RectTransform child in row) if (child.gameObject.activeSelf) items.Add(child);
            if (items.Count == 0) return;
            _lastLabels = LabelStamp();
            ShareTabWidth(items, theme);

            _lastParentWidth = parent != null ? parent.rect.width : -1f;
            float room = parent != null && parent.rect.width > 0f ? parent.rect.width - gap : float.MaxValue; // a gap of air at the ends
            // Inside a layout group the row gets the group's inner width (the level-up column leaves room for the stats).
            var group = parent != null ? parent.GetComponent<UnityEngine.UI.LayoutGroup>() : null;
            if (group != null && group.enabled && room < float.MaxValue) room -= group.padding.left + group.padding.right;

            // Break into lines that fit the room.
            var lines = new List<List<RectTransform>>();
            var widths = new List<float>();
            var heights = new List<float>();
            var line = new List<RectTransform>();
            float lineWidth = 0f, lineHeight = 0f;
            foreach (var item in items)
            {
                Vector2 size = Size(item, theme);
                float next = line.Count == 0 ? size.x : lineWidth + gap + size.x;
                if (line.Count > 0 && next > room)
                {
                    lines.Add(line); widths.Add(lineWidth); heights.Add(lineHeight);
                    line = new List<RectTransform>(); lineWidth = 0f; lineHeight = 0f;
                    next = size.x;
                }
                line.Add(item);
                lineWidth = next;
                lineHeight = Mathf.Max(lineHeight, size.y);
            }
            lines.Add(line); widths.Add(lineWidth); heights.Add(lineHeight);
            Lines = lines.Count;

            float totalHeight = gap * (lines.Count - 1);
            float maxWidth = 0f;
            for (int i = 0; i < lines.Count; i++) { totalHeight += heights[i]; maxWidth = Mathf.Max(maxWidth, widths[i]); }

            float y = totalHeight * 0.5f;
            for (int i = 0; i < lines.Count; i++)
            {
                float x = -widths[i] * 0.5f;
                float cy = y - heights[i] * 0.5f;
                foreach (var item in lines[i])
                {
                    float w = Size(item, theme).x;
                    item.anchorMin = item.anchorMax = new Vector2(0.5f, 0.5f);
                    item.pivot = new Vector2(0.5f, 0.5f);
                    item.anchoredPosition = new Vector2(x + w * 0.5f, cy);
                    x += w + gap;
                }
                y -= heights[i] + gap;
            }

            row.sizeDelta = new Vector2(maxWidth, totalHeight);
            // Inside a layout group (the level-up column) the group sizes the row: tell it the wrapped height.
            var element = row.GetComponent<UnityEngine.UI.LayoutElement>();
            if (element != null)
            {
                element.minHeight = element.preferredHeight = totalHeight;
                element.flexibleHeight = 0f;
            }
            // Only a single control wider than the room is scaled; wrapped lines keep their full size.
            float scale = room > 0f && room < float.MaxValue && maxWidth > room ? room / maxWidth : 1f;
            row.localScale = new Vector3(scale, scale, 1f);
        }

        private static Vector2 Size(RectTransform child, UiThemeAsset theme)
        {
            var element = child.GetComponent<ThemedElement>();
            var size = element != null ? element.StandardSize(theme) : null;
            return size ?? child.rect.size;
        }
    }
}
