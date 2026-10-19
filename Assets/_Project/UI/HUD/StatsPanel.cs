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

        /// <summary>
        /// Adds the panel inside <paramref name="parent"/> (once), with up to <paramref name="columns"/> columns. Responsive
        /// (owner, 2026-10-10): the panel uses as many of them as give the largest text in the room it has, so a wide panel
        /// spreads out and a narrow one (portrait) stacks into fewer, taller columns.
        /// </summary>
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
                // Values get a third of the column: "+25%" wrapped its "%" onto the next row in a narrow column (2026-10-10).
                panel._columns[i] = MakeText(colRect, "Labels", font, 0f, 0.66f, TextAlignmentOptions.TopLeft);
                panel._values[i] = MakeText(colRect, "Values", font, 0.64f, 1f, TextAlignmentOptions.TopRight);
            }
            panel.Refresh();
            return panel;
        }

        /// <summary>The text size <paramref name="n"/> columns allow in a panel of that size (one size for every column).</summary>
        private float SizeFor(int n, float width, float height, int longest)
        {
            int perColumn = Mathf.CeilToInt(Rows.Length / (float)n);
            int lines = perColumn + (n == 1 ? 1 : 0);
            float columnWidth = width / n * 0.94f;
            float columnHeight = height * 0.92f;
            return Mathf.Min(_maxFontSize, columnHeight / (lines * 1.25f), columnWidth / Mathf.Max(1, longest * 0.55f));
        }

        private int _laidOut = -1;

        /// <summary>Spreads the first <paramref name="n"/> columns over the panel's width; the rest are hidden.</summary>
        private void LayoutColumns(int n)
        {
            if (n == _laidOut) return;
            _laidOut = n;
            for (int i = 0; i < _columns.Length; i++)
            {
                var colRect = (RectTransform)_columns[i].transform.parent;
                bool on = i < n;
                colRect.gameObject.SetActive(on);
                if (!on) continue;
                float w = 1f / n;
                colRect.anchorMin = new Vector2(i * w + 0.03f / n, 0.04f);
                colRect.anchorMax = new Vector2((i + 1) * w - 0.03f / n, 0.96f);
                colRect.offsetMin = colRect.offsetMax = Vector2.zero;
            }
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
            UITheme.ApplyText(tmp, ThemedElement.TextRole.Body);
            tmp.textWrappingMode = TextWrappingModes.NoWrap; // after the theme, which may set its own
            tmp.overflowMode = TextOverflowModes.Overflow;
            return tmp;
        }

        /// <summary>
        /// Keeps the panel in the free band under <paramref name="above"/> (the level-up heading) and over the highest of
        /// <paramref name="below"/> (the cards), between <paramref name="minX"/> and <paramref name="maxX"/> of the parent's
        /// width, whatever the window's size or aspect.
        /// </summary>
        /// <summary>A rect the panel keeps clear of (the HUD's item slots): it moves right of it, or under it when narrow.</summary>
        public RectTransform Avoid;

        public void FitBetween(RectTransform above, Selectable[] below, float minX, float maxX)
        {
            _above = above;
            _below = below;
            _minX = minX;
            _maxX = maxX;
            Fit();
        }

        private Vector2 _sizeAtRefresh;

        // Bug MIS UI-09: Fit runs every frame; its corner buffer is kept instead of allocated each time.
        private readonly Vector3[] _corners = new Vector3[4];

        private void LateUpdate()
        {
            Fit();
            // The font size follows the panel's size: refresh as soon as the size settles (the level-up screen lays out a frame later).
            if (_columns != null && _columns.Length > 0 && ((RectTransform)_columns[0].transform.parent).rect.size != _sizeAtRefresh) Refresh();
        }

        private void Fit()
        {
            if (_below == null || !(transform.parent is RectTransform parent)) return;
            var corners = _corners;
            float cardsTop = float.MinValue, cardsBottom = float.MaxValue, cardsLeft = float.MaxValue;
            foreach (var card in _below)
            {
                if (card == null || !card.gameObject.activeInHierarchy) continue;
                ((RectTransform)card.transform).GetWorldCorners(corners);
                cardsTop = Mathf.Max(cardsTop, corners[1].y);
                cardsBottom = Mathf.Min(cardsBottom, corners[0].y);
                cardsLeft = Mathf.Min(cardsLeft, corners[0].x);
            }
            if (cardsTop == float.MinValue) return;
            parent.GetWorldCorners(corners);
            float parentBottom = corners[0].y, parentTop = corners[1].y;
            float top = parentTop;
            if (_above != null && _above.gameObject.activeInHierarchy)
            {
                RaiseHeadingIfRowsDoNotFit(parent, cardsTop, parentTop - parentBottom);
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

            // Responsive (owner, 2026-10-10): on a short screen (a phone held sideways) the band over the cards cannot hold
            // the rows at a readable size. The panel then takes the room the cards leave on their left (LevelUpUI keeps it
            // free on short screens), as tall as the cards.
            parent.GetWorldCorners(corners);
            float parentLeft = corners[0].x, parentWidth = Mathf.Max(1f, corners[2].x - corners[0].x);
            float unitsPerWorld = parent.rect.height / span;
            int rowCount = Rows != null && Rows.Length > 0 ? Rows.Length : 19;
            int minLines = Mathf.CeilToInt(rowCount / (float)Mathf.Max(1, _columns != null ? _columns.Length : 1));
            float needUnits = minLines * SoulHunter.Gameplay.UI.UiScreen.MinTextUnits * 1.25f / 0.92f;
            float sideUnits = (cardsLeft - parentLeft) * unitsPerWorld;
            if ((yMax - yMin) * parent.rect.height < needUnits && sideUnits >= 200f && cardsBottom < float.MaxValue)
            {
                float xGap = gap / parentWidth;
                var side = (RectTransform)transform;
                side.anchorMin = new Vector2(xGap, Mathf.Clamp01((cardsBottom - parentBottom) / span));
                side.anchorMax = new Vector2(Mathf.Clamp01((cardsLeft - parentLeft) / parentWidth - xGap), yMax);
                side.offsetMin = side.offsetMax = Vector2.zero;
                _bandHeight = (side.anchorMax.y - side.anchorMin.y) * parent.rect.height;
                return;
            }
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
            float xMin = _minX;
            if (Avoid != null && Avoid.gameObject.activeInHierarchy)
            {
                // Responsive (2026-10-10): the HUD's item slots keep their size in canvas units, so on a narrow canvas they
                // reach past the panel's left edge. Move right of them when that leaves room, else under them.
                Avoid.GetWorldCorners(corners);
                float aLeft = corners[0].x, aRight = corners[2].x, aBottom = corners[0].y, aTop = corners[1].y;
                float pXMin = parentLeft + xMin * parentWidth, pXMax = parentLeft + _maxX * parentWidth;
                float pYMin = parentBottom + yMin * span, pYMax = parentBottom + yMax * span;
                bool overlaps = aRight > pXMin && aLeft < pXMax && aTop > pYMin && aBottom < pYMax;
                if (overlaps)
                {
                    float right = (aRight + gap - parentLeft) / parentWidth;
                    if (_maxX - right >= 0.35f) xMin = right;
                    else
                    {
                        // Under them, only if the rows still fit readably; else the opaque panel covers the dimmed slots.
                        float under = Mathf.Max(yMin, Mathf.Min(yMax, (aBottom - gap - parentBottom) / span));
                        if ((under - yMin) * parent.rect.height >= needUnits) yMax = under;
                    }
                }
            }
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(_maxX, yMax);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private Vector2 _raisedFor = new Vector2(-1f, -1f);

        /// <summary>
        /// Touch screens only (a PC keeps the approved look). The heading sits at the bottom of a tall box, just over the
        /// panel. On a phone held upright the rows cannot shrink under 12 sp, and the band between the heading and the
        /// cards was one row short: "Cooldown" was drawn under the panel, on the first card (owner's phone, 2026-10-12).
        /// The heading then moves to the middle of its box, which gives the rows their room. It stays there until the
        /// screen changes size, so the two do not chase each other.
        /// </summary>
        private void RaiseHeadingIfRowsDoNotFit(RectTransform parent, float cardsTop, float span)
        {
            var heading = _above.GetComponent<TMP_Text>();
            if (heading == null || SoulHunter.Gameplay.UI.UiScreen.MinTouchUnits <= 0f || _lines <= 0) return;
            Vector2 size = parent.rect.size;
            if (size != _raisedFor && _raisedFor.x >= 0f) { heading.alignment = TextAlignmentOptions.Bottom; _raisedFor = new Vector2(-1f, -1f); }
            if (_raisedFor.x >= 0f)
            {
                // The level-up screen sets the heading low again each time it opens.
                if (heading.alignment != TextAlignmentOptions.Center) { heading.alignment = TextAlignmentOptions.Center; heading.ForceMeshUpdate(); }
                return;
            }
            var corners = _corners; // shared with Fit, which fills it again before its next read
            _above.GetWorldCorners(corners);
            float unitsPerWorld = parent.rect.height / Mathf.Max(1f, span);
            float bandUnits = (corners[0].y - cardsTop) * unitsPerWorld - parent.rect.height * 0.03f; // the two gaps
            float needUnits = _lines * Mathf.Max(_fontSize, SoulHunter.Gameplay.UI.UiScreen.MinTextUnits) * 1.3f / 0.92f;
            if (bandUnits >= needUnits) return;
            heading.alignment = TextAlignmentOptions.Center;
            heading.ForceMeshUpdate();
            _raisedFor = size;
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

            int longest = 0;
            foreach (var row in Rows) longest = Mathf.Max(longest, row.Label.Length + row.Value.Length + 2);
            var panelRect = ((RectTransform)transform).rect;
            float panelHeight = _bandHeight > 0f ? _bandHeight : panelRect.height; // see Fit
            // The column count that gives the largest text (1..the columns built).
            int used = 1;
            float best = -1f;
            for (int n = 1; n <= _columns.Length; n++)
            {
                float s = SizeFor(n, panelRect.width, panelHeight, longest);
                if (s > best + 0.01f) { best = s; used = n; }
            }
            LayoutColumns(used);
            int perColumn = Mathf.CeilToInt(Rows.Length / (float)used);
            _sizeAtRefresh = ((RectTransform)_columns[0].transform.parent).rect.size;
            bool title = used == 1;
            int lines = perColumn + (title ? 1 : 0);
            float size = Mathf.Max(SoulHunter.Gameplay.UI.UiScreen.MinTextUnits, best);
            _fontSize = size;
            _lines = lines;
            for (int c = 0; c < _columns.Length; c++)
            {
                if (c >= used) { _columns[c].text = _values[c].text = ""; continue; }
                var labels = new System.Text.StringBuilder();
                var values = new System.Text.StringBuilder();
                if (c == 0 && title) { labels.Append(UITheme.Bold("STATS")).Append("\n"); values.Append("\n"); }
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
