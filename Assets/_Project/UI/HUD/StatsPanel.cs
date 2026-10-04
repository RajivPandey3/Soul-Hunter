using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// VS stats panel (vampire.survivors.wiki/w/Player_stats: stats "can be seen ... in-game when paused, or when leveling
    /// up"). Owner request (2026-09-30), after Hollow Heart looked like it did nothing: VS Hollow Heart does not fill the
    /// new Max Health, and nothing on screen showed the new maximum. The panel sits inside the pause menu and the level-up
    /// screen, so it shows and hides with them, and reads the live values in the wiki's order (CharacterStatSheet.InGameRows).
    /// Changed stats are highlighted as on the character select screen. The look is Soul Hunter's own.
    /// </summary>
    public sealed class StatsPanel : MonoBehaviour
    {
        public const string ObjectName = "Stats_Panel";
        private const string BonusColor = "#FFD940";
        private const float RefreshSeconds = 0.25f;

        private TextMeshProUGUI[] _columns;
        private TextMeshProUGUI[] _values;
        private float _maxFontSize;
        private RectTransform _above;
        private Selectable[] _below;
        private float _minX, _maxX;
        private float _fontSize;
        private int _lines;
        private float _bandHeight = -1f;

        /// <summary>The row font size in use (tests).</summary>
        public float FontSize => _fontSize;
        private float _nextRefresh;
        private LevelUpManager _levelUp;

        /// <summary>The rows last shown (tests).</summary>
        public CharacterStatSheet.Row[] Rows { get; private set; }

        /// <summary>Adds the panel inside <paramref name="parent"/> (once), laid out in <paramref name="columns"/> columns.</summary>
        public static StatsPanel Attach(Transform parent, Vector2 anchorMin, Vector2 anchorMax, int columns, float maxFontSize)
        {
            if (parent == null) return null;
            var existing = parent.Find(ObjectName);
            if (existing != null) return existing.GetComponent<StatsPanel>();

            var go = new GameObject(ObjectName, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            // Never laid out by a layout group in the parent (the level-up screen stacks its heading and cards).
            go.AddComponent<LayoutElement>().ignoreLayout = true;
            var image = go.GetComponent<Image>();
            UITheme.ApplyPanel(image, 0.92f);
            image.raycastTarget = false;

            var panel = go.AddComponent<StatsPanel>();
            TMP_FontAsset font = null;
            foreach (var text in parent.GetComponentsInChildren<TMP_Text>(true)) { if (text.font != null) { font = text.font; break; } }
            // Each column holds a label text (left) and a value text (right), so a long label never runs into its value.
            int count = Mathf.Max(1, columns);
            panel._columns = new TextMeshProUGUI[count];
            panel._values = new TextMeshProUGUI[count];
            panel._maxFontSize = maxFontSize;
            for (int i = 0; i < count; i++)
            {
                var col = new GameObject("Column" + i, typeof(RectTransform));
                var colRect = (RectTransform)col.transform;
                colRect.SetParent(rect, false);
                float w = 1f / count;
                colRect.anchorMin = new Vector2(i * w + 0.03f / count, 0.04f);
                colRect.anchorMax = new Vector2((i + 1) * w - 0.03f / count, 0.96f);
                colRect.offsetMin = colRect.offsetMax = Vector2.zero;
                panel._columns[i] = MakeText(colRect, "Labels", font, 0f, 0.74f, TextAlignmentOptions.TopLeft);
                panel._values[i] = MakeText(colRect, "Values", font, 0.74f, 1f, TextAlignmentOptions.TopRight);
            }
            panel.Refresh();
            return panel;
        }

        private static TextMeshProUGUI MakeText(RectTransform column, string name, TMP_FontAsset font, float xMin, float xMax, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(column, false);
            rect.anchorMin = new Vector2(xMin, 0f);
            rect.anchorMax = new Vector2(xMax, 1f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.richText = true;
            tmp.raycastTarget = false;
            tmp.alignment = align;
            tmp.enableAutoSizing = false; // one size for every column, set in Refresh
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Truncate;
            UITheme.ApplyText(tmp, ThemedElement.TextRole.Body);
            return tmp;
        }

        /// <summary>
        /// Keeps the panel in the free band under <paramref name="above"/> (the level-up heading) and over the highest of
        /// <paramref name="below"/> (the cards), between <paramref name="minX"/> and <paramref name="maxX"/> of the parent's
        /// width, whatever the window's size or aspect.
        /// </summary>
        public void FitBetween(RectTransform above, Selectable[] below, float minX, float maxX)
        {
            _above = above;
            _below = below;
            _minX = minX;
            _maxX = maxX;
            Fit();
        }

        private Vector2 _sizeAtRefresh;

        private void LateUpdate()
        {
            Fit();
            // The font size follows the panel's size: refresh as soon as the size settles (the level-up screen lays out a frame later).
            if (_columns != null && _columns.Length > 0 && ((RectTransform)_columns[0].transform.parent).rect.size != _sizeAtRefresh) Refresh();
        }

        private void Fit()
        {
            if (_below == null || !(transform.parent is RectTransform parent)) return;
            var corners = new Vector3[4];
            float cardsTop = float.MinValue;
            foreach (var card in _below)
            {
                if (card == null || !card.gameObject.activeInHierarchy) continue;
                ((RectTransform)card.transform).GetWorldCorners(corners);
                cardsTop = Mathf.Max(cardsTop, corners[1].y);
            }
            if (cardsTop == float.MinValue) return;
            parent.GetWorldCorners(corners);
            float parentBottom = corners[0].y, parentTop = corners[1].y;
            float top = parentTop;
            if (_above != null && _above.gameObject.activeInHierarchy)
            {
                // Under the heading's drawn text: its rect is tall and reaches down to the cards.
                var text = _above.GetComponent<TMP_Text>();
                if (text != null && text.textBounds.size.y > 0f) top = _above.TransformPoint(new Vector3(0f, text.textBounds.min.y, 0f)).y;
                else { _above.GetWorldCorners(corners); top = corners[0].y; }
            }
            float span = Mathf.Max(1f, parentTop - parentBottom);
            float gap = span * 0.015f;
            float yMin = Mathf.Clamp01((cardsTop + gap - parentBottom) / span);
            float yMax = Mathf.Clamp01((top - gap - parentBottom) / span);
            if (yMax <= yMin) yMax = yMin; // no room: collapse rather than cover a card
            // The font is sized from this whole free band (not from the panel, which then shrinks to the rows): sizing it from
            // the shrunken panel fed back on itself and shrank the text every frame (seen in editor play, 2026-09-30).
            _bandHeight = (yMax - yMin) * parent.rect.height;
            // Bug MIS B09 (2026-09-30): the panel filled the whole band, leaving its lower half empty. It is only as tall
            // as its rows need (the columns use 92% of the panel's height), hanging under the heading.
            if (_fontSize > 0f && _lines > 0)
            {
                float needLocal = _lines * _fontSize * 1.25f / 0.92f;
                float needWorld = needLocal * parent.lossyScale.y;
                yMin = Mathf.Max(yMin, yMax - needWorld / span);
            }
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(_minX, yMin);
            rect.anchorMax = new Vector2(_maxX, yMax);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private void OnEnable()
        {
            _nextRefresh = 0f;
            Refresh();
        }

        private void Update()
        {
            // Unscaled: the panel is shown while the game is paused. Refreshes while open (several level-ups in a row).
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + RefreshSeconds;
            Refresh();
        }

        public void Refresh()
        {
            if (_columns == null) return;
            var player = PlayerController.Instance;
            if (player == null) return;
            if (_levelUp == null) _levelUp = FindFirstObjectByType<LevelUpManager>();
            var health = player.GetComponent<HealthController>();
            var sheet = CharacterStatSheet.Live(player.Stats, health != null ? health.MaxHealth : 100,
                _levelUp != null ? _levelUp.RerollsLeft : 0, _levelUp != null ? _levelUp.SkipsLeft : 0, _levelUp != null ? _levelUp.BanishesLeft : 0);
            Rows = sheet.InGameRows();

            int perColumn = Mathf.CeilToInt(Rows.Length / (float)_columns.Length);
            _sizeAtRefresh = ((RectTransform)_columns[0].transform.parent).rect.size;
            bool title = _columns.Length == 1;
            int lines = perColumn + (title ? 1 : 0);
            // One font size for all columns: fits the column's height and its longest "label value" line.
            var columnRect = ((RectTransform)_columns[0].transform.parent).rect;
            float columnHeight = _bandHeight > 0f ? _bandHeight * 0.92f : columnRect.height; // see Fit
            int longest = 0;
            foreach (var row in Rows) longest = Mathf.Max(longest, row.Label.Length + row.Value.Length + 2);
            float size = Mathf.Min(_maxFontSize, columnHeight / (lines * 1.25f), columnRect.width / Mathf.Max(1, longest * 0.55f));
            size = Mathf.Max(8f, size);
            _fontSize = size;
            _lines = lines;
            for (int c = 0; c < _columns.Length; c++)
            {
                var labels = new System.Text.StringBuilder();
                var values = new System.Text.StringBuilder();
                if (c == 0 && title) { labels.Append("<b>STATS</b>\n"); values.Append("\n"); }
                for (int r = c * perColumn; r < Mathf.Min(Rows.Length, (c + 1) * perColumn); r++)
                {
                    var row = Rows[r];
                    labels.Append(row.Label).Append('\n');
                    values.Append(row.Changed ? $"<color={BonusColor}>{row.Value}</color>" : row.Value).Append('\n');
                }
                _columns[c].fontSize = _values[c].fontSize = size;
                _columns[c].text = labels.ToString();
                _values[c].text = values.ToString();
            }
        }
    }
}
