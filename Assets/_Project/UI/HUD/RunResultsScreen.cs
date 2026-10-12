using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SoulHunter.Gameplay.Core;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// The VS Results screen (vampire.survivors.wiki/w/Results, fetched 2026-10-02), in Soul Hunter's own look: the
    /// stage, mode, gold bonus, time survived, gold, level and kills; the weapon table (level, damage, time used, DPS; the
    /// most damage and the most DPS in yellow; damage with SI prefixes); the character and Soul Eggs with the items picked
    /// up (levels yellow when max); the PowerUps, the pickups and the Arcanas. A green button shows or hides the run's
    /// unlocks when there are any, and DONE goes back to the main menu. The layout is Soul Hunter's; the wiki describes
    /// only what each corner holds.
    ///
    /// Responsive (owner, 2026-10-10: the same screen in landscape and portrait, no second layout). One set of content:
    /// - the content sits in the safe area; the backdrop stays full screen and opaque;
    /// - the four areas (summary, character and items, weapons, lists) are two nested <see cref="AdaptiveSplit"/>s: a 2x2
    ///   grid when there is room for two columns, one column on a narrow (portrait) screen;
    /// - every area takes the height its content needs; when they need more than the screen has, the areas scroll;
    /// - the weapon table keeps its number columns at the width their numbers need; on a narrow screen a weapon's numbers
    ///   drop to a second line under its name, so no number is ever cut;
    /// - DONE and SHOW/HIDE UNLOCKS sit in a <see cref="ThemedRow"/> pinned to the bottom of the safe area;
    /// - on a touch screen no text is under <see cref="UiScreen.MinTextUnits"/>;
    /// - it lays out again on <see cref="UiScreen.Changed"/> (a rotation, a window change) without rebuilding.
    /// </summary>
    public sealed class RunResultsScreen : MonoBehaviour
    {
        public const int SortingOrder = 450;
        /// <summary>The VS yellow of the best values (theme: Rewards > Best).</summary>
        public static Color Yellow => UITheme.Rewards.Best;

        /// <summary>The narrowest an area column may be (canvas units) before the four areas go into one column.</summary>
        public const float MinColumnWidth = 520f;
        /// <summary>The narrowest a weapon name may be on its row before the numbers drop to a second line.</summary>
        public const float MinNameWidth = 150f;

        // Base text sizes (canvas units); on a touch screen none is drawn under UiScreen.MinTextUnits.
        private const float TitleSize = 54f, SummarySize = 30f, CellSize = 26f, CharacterSize = 34f, ItemLevelSize = 20f,
            ListsSize = 24f, UnlocksSize = 32f, ButtonLabelSize = 34f, PanelPadding = 22f, TitleHeight = 84f;
        private static readonly Vector2 ItemCell = new Vector2(76f, 86f);
        private static readonly Vector2 ItemSpacing = new Vector2(10f, 8f);

        public RunResults Results { get; private set; }
        public Button DoneButton { get; private set; }
        public Button UnlocksButton { get; private set; }
        public GameObject UnlocksPanel { get; private set; }
        public TextMeshProUGUI TitleText { get; private set; }
        public TextMeshProUGUI SummaryText { get; private set; }
        public TextMeshProUGUI ListsText { get; private set; }
        /// <summary>One level label per item icon, in the items order (yellow when max).</summary>
        public readonly List<TextMeshProUGUI> ItemLevelTexts = new List<TextMeshProUGUI>();
        /// <summary>The scrolling area that holds the four blocks (between the title and the bottom row).</summary>
        public ScrollRect Scroll { get; private set; }
        /// <summary>The bottom row with DONE (and SHOW/HIDE UNLOCKS), pinned to the bottom of the safe area.</summary>
        public RectTransform ButtonBar { get; private set; }
        /// <summary>The four areas: summary, weapons, character and items, lists.</summary>
        public RectTransform SummaryArea { get; private set; }
        public RectTransform WeaponsArea { get; private set; }
        public RectTransform CharacterArea { get; private set; }
        public RectTransform ListsArea { get; private set; }
        /// <summary>True when the four areas are in two columns (a 2x2 grid), false when they are one column.</summary>
        public bool TwoColumns { get; private set; }
        /// <summary>True when each weapon's numbers sit on a second line under its name (a narrow table).</summary>
        public bool WeaponRowsOnTwoLines { get; private set; }
        public event System.Action Done;

        private TMP_FontAsset _font;
        private RectTransform _root, _safe, _body, _leftColumn, _rightColumn, _unlocksBody;
        private ThemedRow _row;
        private AdaptiveSplit _columns, _leftSplit, _rightSplit;
        private TextMeshProUGUI _characterText, _unlocksText;
        private RectTransform _itemsGrid;
        private GridLayoutGroup _itemsLayout;
        private WeaponRowView _header;
        private TextMeshProUGUI _emptyRow;
        private readonly Dictionary<TMP_Text, float> _baseSizes = new Dictionary<TMP_Text, float>();
        private Vector2 _lastViewport = new Vector2(-1f, -1f);
        private float _lastBar = -1f, _lastFloor = -1f;
        private bool _dirty;

        /// <summary>Builds the screen on its own overlay canvas under <paramref name="owner"/>.</summary>
        public static RunResultsScreen Show(Transform owner, RunResults results, TMP_FontAsset font)
        {
            var go = new GameObject("RunResultsCanvas", typeof(RectTransform));
            go.transform.SetParent(owner, false);
            // A canvas nested in another canvas keeps its own small rect and ignores its scaler (the Revive screen's veil
            // was a small square, 2026-10-09): under a canvas, the screen becomes a root object of the owner's scene.
            if (owner != null && owner.GetComponentInParent<Canvas>() != null)
            {
                go.transform.SetParent(null, false);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, owner.gameObject.scene);
            }
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortingOrder;
            ResponsiveCanvas.Apply(canvas); // the game's one UI scale (UiScreen), landscape and portrait
            go.AddComponent<GraphicRaycaster>();
            var screen = go.AddComponent<RunResultsScreen>();
            screen._font = font;
            screen.Build(results);
            return screen;
        }

        private void OnEnable() => UiScreen.Changed += MarkDirty;
        private void OnDisable() => UiScreen.Changed -= MarkDirty;
        private void OnDestroy() => UiScreen.Changed -= MarkDirty;

        private void MarkDirty()
        {
            _dirty = true;
            Relayout(); // at once, so a rotation never shows a frame of the old layout
        }

        private void LateUpdate()
        {
            if (Scroll == null) return;
            Vector2 viewport = ((RectTransform)Scroll.transform).rect.size;
            float bar = _row != null ? ((RectTransform)_row.transform).sizeDelta.y : 0f;
            if (_dirty || viewport != _lastViewport || !Mathf.Approximately(bar, _lastBar) || !Mathf.Approximately(UiScreen.MinTextUnits, _lastFloor))
                Relayout();
        }

        // ---------- build (once) ----------

        private void Build(RunResults results)
        {
            Results = results;
            _root = (RectTransform)transform;
            // Opaque and full screen: the UI blends in linear space, so even a 0.97 dark layer let the game show through.
            var backdrop = Panel(_root, "Backdrop");
            UITheme.ApplySurface(backdrop.GetComponent<Image>(), ThemedElement.SurfaceRole.Backdrop);
            backdrop.GetComponent<Image>().raycastTarget = true;

            _safe = Area("SafeContent", _root);
            SafeAreaFitter.Fit(_safe);

            TitleText = Text(_safe, "Title", TitleLine(results), TitleSize, TextAlignmentOptions.Center);
            TitleText.fontStyle = FontStyles.Bold;
            UITheme.ApplyText(TitleText, ThemedElement.TextRole.Title);
            var titleRect = TitleText.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);

            // The four areas scroll in one masked viewport (a drag anywhere on them scrolls; the panels take no clicks).
            Scroll = ResponsiveScroll.Create(Area("Scroll", _safe), null, "Content", out _body);

            // Two columns of two areas each (nested splits); the columns stack on a narrow screen.
            _leftColumn = Area("LeftColumn", _body);
            _rightColumn = Area("RightColumn", _body);
            _columns = AdaptiveSplit.Between(_body, _leftColumn, _rightColumn, 0.5f, 0.5f);
            SummaryArea = Panel(_leftColumn, "Summary");
            WeaponsArea = Panel(_leftColumn, "Weapons");
            CharacterArea = Panel(_rightColumn, "Character");
            ListsArea = Panel(_rightColumn, "Lists");
            _leftSplit = AdaptiveSplit.Between(_leftColumn, SummaryArea, WeaponsArea, 0.5f, 0.4f);
            _rightSplit = AdaptiveSplit.Between(_rightColumn, CharacterArea, ListsArea, 0.5f, 0.4f);
            _leftSplit.TallWhenAspectBelow = _rightSplit.TallWhenAspectBelow = float.MaxValue; // each column is always stacked

            SummaryText = Text(SummaryArea, "SummaryText", SummaryLines(results), SummarySize, TextAlignmentOptions.TopLeft);
            Inset(SummaryText.rectTransform);
            BuildWeaponTable(WeaponsArea, results);
            BuildCharacter(CharacterArea, results);
            ListsText = Text(ListsArea, "ListsText", Lists(results), ListsSize, TextAlignmentOptions.TopLeft);
            Inset(ListsText.rectTransform);

            if (results.Unlocks.Count > 0)
            {
                // Over the four areas, under the bottom row; it scrolls too when the list is long.
                var panel = Panel(_safe, "UnlocksPanel");
                UnlocksPanel = panel.gameObject;
                ResponsiveScroll.Create(Area("UnlocksScroll", panel), null, "Content", out _unlocksBody);
                _unlocksText = Text(_unlocksBody, "UnlocksText",
                    $"<color=#{Gold}>{UITheme.Bold("UNLOCKED THIS RUN")}</color>\n\n" + string.Join("\n", results.Unlocks), UnlocksSize, TextAlignmentOptions.Top);
                UnlocksPanel.SetActive(false);
            }

            // The bottom row: the button that ends the screen is always on screen.
            ButtonBar = Area("ButtonBar", _safe);
            ButtonBar.anchorMin = new Vector2(0f, 0f);
            ButtonBar.anchorMax = new Vector2(1f, 0f);
            ButtonBar.pivot = new Vector2(0.5f, 0f);
            var row = Area("Row", ButtonBar);
            row.anchorMin = row.anchorMax = row.pivot = new Vector2(0.5f, 0.5f);
            _row = row.gameObject.AddComponent<ThemedRow>();
            if (results.Unlocks.Count > 0)
            {
                UnlocksButton = MakeButton(row, "Unlocks_Button", $"SHOW UNLOCKS ({results.Unlocks.Count})", UITheme.ButtonKind.Positive);
                UnlocksButton.onClick.AddListener(ToggleUnlocks);
            }
            DoneButton = MakeButton(row, "Done_Button", "DONE", UITheme.ButtonKind.Primary);
            DoneButton.onClick.AddListener(() => Done?.Invoke());

            Canvas.ForceUpdateCanvases();
            Relayout();
            Focus(DoneButton);
        }

        private static string TitleLine(RunResults results) => results.End switch
        {
            RunResults.EndKind.Victory => $"<color=#{Gold}>SHADOW KAEL DESTROYED</color>",
            RunResults.EndKind.Death => "GAME OVER",
            _ => "RESULTS"
        };

        private static string SummaryLines(RunResults results)
        {
            var text = new StringBuilder();
            if (results.End == RunResults.EndKind.Victory)
                // The owner's story, word for word (Docs/Game_Design_Document.md).
            {
                text.Append("<i>").Append(SoulHunter.Gameplay.Data.CampaignStory.FinalLine).Append("</i>\n");
                // Owner-approved story layer (2026-10-13): one more line once every keepsake has been found.
                if (SoulHunter.Gameplay.Data.StoryTwist.AllKeepsakesFound)
                    text.Append("<i>").Append(SoulHunter.Gameplay.Data.StoryTwist.TrueEnding).Append("</i>\n");
            }
            text.Append(UITheme.Bold(results.StageLine)).Append("\n");
            text.Append("Mode: ").Append(results.Mode).Append("     Gold bonus: ")
                .Append(results.GoldBonusPercent >= 0 ? "+" : "").Append(results.GoldBonusPercent).Append("%\n");
            text.Append($"Survived: <color=#{Gold}>").Append(RunResultsFormat.Time(results.SecondsSurvived)).Append("</color>\n");
            text.Append($"Gold earned: <color=#{Gold}>").Append(results.Gold).Append("</color>\n");
            text.Append($"Level reached: <color=#{Gold}>").Append(results.Level).Append("</color>\n");
            text.Append($"Enemies defeated: <color=#{Gold}>").Append(results.Kills).Append("</color>");
            return text.ToString();
        }

        /// <summary>One weapon table row: its name, level, damage, time used and DPS cells.</summary>
        public sealed class WeaponRowView
        {
            public TextMeshProUGUI Name, Level, Damage, Time, Dps;
            public RectTransform Row, Icon;
            internal IEnumerable<TextMeshProUGUI> Numbers { get { yield return Level; yield return Damage; yield return Time; yield return Dps; } }
        }

        /// <summary>The weapon table's rows, in the results' weapon order.</summary>
        public readonly List<WeaponRowView> WeaponRows = new List<WeaponRowView>();

        private void BuildWeaponTable(RectTransform table, RunResults results)
        {
            _header = NewRow(table, "Header", null, "WEAPON", "LV", "DAMAGE", "TIME", "DPS");
            foreach (var cell in new[] { _header.Name, _header.Level, _header.Damage, _header.Time, _header.Dps })
                UITheme.ApplyText(cell, ThemedElement.TextRole.Dim);
            for (int i = 0; i < results.Weapons.Count; i++)
            {
                var data = results.Weapons[i];
                var view = NewRow(table, "Row_" + data.Name, data.Icon, data.Name, data.Level > 0 ? data.Level.ToString() : "-",
                    RunResultsFormat.Damage(data.Damage), RunResultsFormat.Time(data.SecondsUsed), RunResultsFormat.Damage(data.Dps));
                if (data.Maxed) UITheme.ApplyText(view.Level, ThemedElement.TextRole.Best);
                if (i == results.TopDamageRow) UITheme.ApplyText(view.Damage, ThemedElement.TextRole.Best);
                if (i == results.TopDpsRow) UITheme.ApplyText(view.Dps, ThemedElement.TextRole.Best);
                WeaponRows.Add(view);
            }
            if (results.Weapons.Count == 0)
            {
                _emptyRow = Text(table, "Empty", "No damage dealt.", CellSize, TextAlignmentOptions.Left);
                TopLeft(_emptyRow.rectTransform);
            }
        }

        private WeaponRowView NewRow(RectTransform table, string name, Sprite icon, string weapon, string level, string damage, string time, string dps)
        {
            var row = Area(name, table);
            TopLeft(row);
            var view = new WeaponRowView { Row = row };
            if (icon != null)
            {
                var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                view.Icon = (RectTransform)go.transform;
                view.Icon.SetParent(row, false);
                TopLeft(view.Icon);
                var image = go.GetComponent<Image>();
                image.sprite = icon;
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
            view.Name = Cell(row, "Name", weapon, TextAlignmentOptions.Left, number: false);
            view.Level = Cell(row, "Level", level, TextAlignmentOptions.Center, number: true);
            view.Damage = Cell(row, "Damage", damage, TextAlignmentOptions.Right, number: true);
            view.Time = Cell(row, "Time", time, TextAlignmentOptions.Right, number: true);
            view.Dps = Cell(row, "Dps", dps, TextAlignmentOptions.Right, number: true);
            return view;
        }

        private TextMeshProUGUI Cell(RectTransform row, string name, string content, TextAlignmentOptions align, bool number)
        {
            var cell = Text(row, name, content, CellSize, align);
            cell.textWrappingMode = TextWrappingModes.NoWrap;
            // A name may end in "..."; a number never does (the table sizes its columns to the widest number).
            cell.overflowMode = number ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;
            TopLeft(cell.rectTransform);
            return cell;
        }

        private void BuildCharacter(RectTransform side, RunResults results)
        {
            _characterText = Text(side, "CharacterText",
                UITheme.Bold(results.Character) + (results.SoulEggs > 0 ? $"     <color=#{Gold}>Soul Eggs: {results.SoulEggs}</color>" : ""),
                CharacterSize, TextAlignmentOptions.TopLeft);
            TopLeft(_characterText.rectTransform);

            // Items: icons in as many columns as the area's width allows, each with its level (yellow when max).
            _itemsGrid = Area("Items", side);
            TopLeft(_itemsGrid);
            _itemsLayout = _itemsGrid.gameObject.AddComponent<GridLayoutGroup>();
            _itemsLayout.cellSize = ItemCell;
            _itemsLayout.spacing = ItemSpacing;
            _itemsLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            _itemsLayout.childAlignment = TextAnchor.UpperLeft;
            _itemsLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _itemsLayout.constraintCount = 9;
            foreach (var item in results.Items)
            {
                var cell = new GameObject(item.Name, typeof(RectTransform), typeof(Image));
                cell.transform.SetParent(_itemsGrid, false);
                var frame = cell.GetComponent<Image>();
                UITheme.ApplySurface(frame, item.IsWeapon ? ThemedElement.SurfaceRole.SlotFilled : ThemedElement.SurfaceRole.SlotEmpty);
                UITheme.Edge(cell, UITheme.Hud.SlotBorder, 1.5f);
                frame.raycastTarget = false;
                var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                var iconRect = (RectTransform)icon.transform;
                iconRect.SetParent(cell.transform, false);
                iconRect.anchorMin = new Vector2(0.1f, 0.3f);
                iconRect.anchorMax = new Vector2(0.9f, 0.95f);
                iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;
                var iconImage = icon.GetComponent<Image>();
                iconImage.sprite = item.Icon;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
                if (item.Icon == null) iconImage.enabled = false;
                var level = Text((RectTransform)cell.transform, "Level", item.Level > 0 ? "Lv " + item.Level : "-", ItemLevelSize, TextAlignmentOptions.Center);
                level.rectTransform.anchorMax = new Vector2(1f, 0.3f);
                UITheme.ApplyText(level, item.Maxed ? ThemedElement.TextRole.Best : ThemedElement.TextRole.Body);
                ItemLevelTexts.Add(level);
            }
        }

        // ---------- layout (every screen change; nothing is rebuilt) ----------

        /// <summary>Lays the screen out for the canvas it is on now.</summary>
        public void Relayout()
        {
            if (this == null || Scroll == null) return;
            _dirty = false;
            float gap = UITheme.Layout.ButtonGap;
            float floor = Mathf.Max(12f, UiScreen.MinTextUnits);
            _lastFloor = UiScreen.MinTextUnits;
            foreach (var pair in _baseSizes)
            {
                if (pair.Key == null) continue;
                pair.Key.fontSizeMax = Mathf.Max(pair.Value, floor);
                pair.Key.fontSizeMin = floor;
                if (pair.Key.fontSize < floor) pair.Key.fontSize = floor;
            }

            // Title at the top, the button row at the bottom, the scrolling areas between them.
            float titleHeight = Mathf.Max(TitleHeight, floor * 1.6f);
            TitleText.rectTransform.offsetMin = new Vector2(gap * 2f, -titleHeight);
            TitleText.rectTransform.offsetMax = new Vector2(-gap * 2f, 0f);
            _row.Layout();
            float rowHeight = ((RectTransform)_row.transform).sizeDelta.y;
            float barHeight = rowHeight + gap * 2f;
            ButtonBar.sizeDelta = new Vector2(0f, barHeight);
            ButtonBar.anchoredPosition = Vector2.zero;
            _lastBar = rowHeight;

            var viewport = (RectTransform)Scroll.transform;
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(gap * 1.5f, barHeight);
            viewport.offsetMax = new Vector2(-gap * 1.5f, -titleHeight);
            if (UnlocksPanel != null)
            {
                var unlocks = (RectTransform)UnlocksPanel.transform;
                unlocks.anchorMin = Vector2.zero;
                unlocks.anchorMax = Vector2.one;
                unlocks.offsetMin = new Vector2(gap * 4f, barHeight + gap * 2f);
                unlocks.offsetMax = new Vector2(-gap * 4f, -titleHeight - gap * 2f);
                LayoutScrollText(_unlocksBody, _unlocksText);
            }

            Vector2 room = viewport.rect.size;
            _lastViewport = room;
            TwoColumns = room.x >= MinColumnWidth * 2f + gap;
            float columnWidth = TwoColumns ? (room.x - gap) * 0.5f : room.x;
            float inner = Mathf.Max(1f, columnWidth - PanelPadding * 2f);

            float summary = PanelPadding * 2f + Preferred(SummaryText, inner);
            float weapons = PanelPadding * 2f + LayoutWeaponTable(inner, gap);
            float character = PanelPadding * 2f + LayoutCharacter(inner, gap);
            float lists = PanelPadding * 2f + Preferred(ListsText, inner);

            float total;
            if (TwoColumns)
            {
                // A 2x2 grid: the rows line up; extra room goes to the lower row.
                float top = Mathf.Max(summary, character), bottom = Mathf.Max(weapons, lists);
                total = top + gap + bottom;
                if (total < room.y) total = room.y;
                float share = Share(top, total, gap);
                _leftSplit.TallShare = _rightSplit.TallShare = share;
                _columns.TallWhenAspectBelow = 0f; // never stacked
            }
            else
            {
                // One column: summary, weapons, character and items, lists; extra room goes to the lists.
                float left = summary + gap + weapons, right = character + gap + lists;
                total = left + gap + right;
                if (total < room.y) { right += room.y - total; total = room.y; }
                _columns.TallShare = Share(left, total, gap);
                _leftSplit.TallShare = Share(summary, left, gap);
                _rightSplit.TallShare = Share(character, right, gap);
                _columns.TallWhenAspectBelow = float.MaxValue; // always stacked
            }
            _columns.Gap = _leftSplit.Gap = _rightSplit.Gap = gap;
            _body.anchorMin = new Vector2(0f, 1f);
            _body.anchorMax = new Vector2(1f, 1f);
            _body.pivot = new Vector2(0.5f, 1f);
            _body.sizeDelta = new Vector2(0f, total);
            float scrollable = Mathf.Max(0f, total - room.y);
            _body.anchoredPosition = new Vector2(0f, Mathf.Clamp(_body.anchoredPosition.y, 0f, scrollable));
            _columns.Layout(force: true);
            _leftSplit.Layout(force: true);
            _rightSplit.Layout(force: true);
            Scroll.vertical = scrollable > 0.5f;
        }

        /// <summary>The share of <paramref name="total"/> the first of two stacked areas takes (AdaptiveSplit halves the gap).</summary>
        private static float Share(float first, float total, float gap) => Mathf.Clamp((first + gap * 0.5f) / Mathf.Max(1f, total), 0.05f, 0.95f);

        /// <summary>Places the weapon table's rows inside <paramref name="width"/>; returns the table's height.</summary>
        private float LayoutWeaponTable(float width, float gap)
        {
            var views = new List<WeaponRowView> { _header };
            views.AddRange(WeaponRows);
            float font = _header.Name.fontSizeMax;
            float line = Mathf.Ceil(font * 1.6f);
            float cellGap = Mathf.Max(8f, gap);

            // Each number column is as wide as its widest number (or heading).
            var numberWidths = new float[4];
            foreach (var view in views)
            {
                int c = 0;
                foreach (var cell in view.Numbers) numberWidths[c] = Mathf.Max(numberWidths[c++], PreferredWidth(cell));
            }
            float numbers = numberWidths[0] + numberWidths[1] + numberWidths[2] + numberWidths[3] + cellGap * 3f;
            float icon = line;
            WeaponRowsOnTwoLines = width - numbers - cellGap - icon - cellGap < MinNameWidth;
            float rowHeight = WeaponRowsOnTwoLines ? line * 2f : line;

            float y = PanelPadding;
            foreach (var view in views)
            {
                Place(view.Row, PanelPadding, y, width, rowHeight);
                if (view.Icon != null) Place(view.Icon, 0f, line * 0.08f, icon, line * 0.84f);
                float nameLeft = icon + cellGap;
                if (!WeaponRowsOnTwoLines)
                {
                    // One line: icon, name, then the four numbers at the right edge.
                    float x = width;
                    var cells = new List<TextMeshProUGUI>(view.Numbers);
                    for (int c = 3; c >= 0; c--)
                    {
                        x -= numberWidths[c];
                        Place(cells[c].rectTransform, x, 0f, numberWidths[c], line);
                        x -= cellGap;
                    }
                    Place(view.Name.rectTransform, nameLeft, 0f, Mathf.Max(1f, x - nameLeft), line);
                }
                else
                {
                    // Two lines: icon and name across the row, the numbers spread under them, each at least its width.
                    Place(view.Name.rectTransform, nameLeft, 0f, Mathf.Max(1f, width - nameLeft), line);
                    float spare = Mathf.Max(0f, width - numbers) / 4f;
                    float x = 0f;
                    int c = 0;
                    foreach (var cell in view.Numbers)
                    {
                        float w = numberWidths[c++] + spare;
                        Place(cell.rectTransform, x, line, w, line);
                        x += w + cellGap;
                    }
                }
                y += rowHeight;
            }
            if (_emptyRow != null)
            {
                Place(_emptyRow.rectTransform, PanelPadding, y, width, line);
                y += line;
            }
            return y - PanelPadding;
        }

        /// <summary>Places the character line and the items grid inside <paramref name="width"/>; returns their height.</summary>
        private float LayoutCharacter(float width, float gap)
        {
            float text = Preferred(_characterText, width);
            Place(_characterText.rectTransform, PanelPadding, PanelPadding, width, text);
            int count = ItemLevelTexts.Count;
            int columns = UiScreen.ColumnsFor(width, ItemCell.x, ItemSpacing.x, 12);
            _itemsLayout.constraintCount = columns;
            int rows = count == 0 ? 0 : Mathf.CeilToInt(count / (float)columns);
            float grid = rows == 0 ? 0f : rows * ItemCell.y + (rows - 1) * ItemSpacing.y;
            Place(_itemsGrid, PanelPadding, PanelPadding + text + gap, width, grid);
            return text + (rows == 0 ? 0f : gap + grid);
        }

        private void LayoutScrollText(RectTransform body, TextMeshProUGUI text)
        {
            if (text == null) return;
            var viewport = (RectTransform)body.parent;
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(PanelPadding, PanelPadding);
            viewport.offsetMax = new Vector2(-PanelPadding, -PanelPadding);
            Vector2 room = viewport.rect.size;
            float height = Mathf.Max(room.y, Preferred(text, Mathf.Max(1f, room.x)));
            body.anchorMin = new Vector2(0f, 1f);
            body.anchorMax = new Vector2(1f, 1f);
            body.pivot = new Vector2(0.5f, 1f);
            body.sizeDelta = new Vector2(0f, height);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            var scroll = viewport.GetComponent<ScrollRect>();
            if (scroll != null) scroll.vertical = height > room.y + 0.5f;
        }

        /// <summary>The height <paramref name="text"/> needs at its largest size in <paramref name="width"/>.</summary>
        private static float Preferred(TMP_Text text, float width)
        {
            if (text == null) return 0f;
            float size = text.fontSize;
            text.fontSize = text.fontSizeMax; // measured at full size, so auto-size never has to shrink it
            float height = Mathf.Ceil(text.GetPreferredValues(text.text, width, 0f).y) + 4f;
            text.fontSize = size;
            return height;
        }

        /// <summary>The width <paramref name="text"/> needs on one line at its largest size.</summary>
        private static float PreferredWidth(TMP_Text text)
        {
            float size = text.fontSize;
            text.fontSize = text.fontSizeMax;
            float width = Mathf.Ceil(text.GetPreferredValues(text.text).x) + 4f;
            text.fontSize = size;
            return width;
        }

        /// <summary>A top-left anchored rect at (x, y) below its parent's top-left corner.</summary>
        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void TopLeft(RectTransform rect) => Place(rect, 0f, 0f, 1f, 1f);

        private static void Inset(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(PanelPadding, PanelPadding);
            rect.offsetMax = new Vector2(-PanelPadding, -PanelPadding);
        }

        /// <summary>The theme's gold and dim text colours for rich-text highlights (no colour of this screen's own).</summary>
        private static string Gold => ColorUtility.ToHtmlStringRGB(UITheme.Accent);
        private static string Dim => ColorUtility.ToHtmlStringRGB(UITheme.TextDim);

        /// <summary>The PowerUps, pickups and Arcanas lists, in the wiki's order.</summary>
        public static string Lists(RunResults results)
        {
            var text = new StringBuilder();
            text.Append(UITheme.Bold($"<color=#{Dim}>POWERUPS</color>")).Append("\n");
            text.Append(results.PowerUps.Count == 0 ? "None" : Join(results.PowerUps, "  "));
            text.Append("\n\n").Append(UITheme.Bold($"<color=#{Dim}>PICKUPS</color>")).Append("\n");
            text.Append(results.Pickups.Count == 0 ? "None" : Join(results.Pickups, " x"));
            text.Append("\n\n").Append(UITheme.Bold($"<color=#{Dim}>ARCANAS</color>")).Append("\n");
            text.Append(results.Arcanas.Count == 0 ? "None" : string.Join(",   ", results.Arcanas));
            return text.ToString();
        }

        private static string Join(List<KeyValuePair<string, int>> pairs, string between)
        {
            var parts = new List<string>(pairs.Count);
            foreach (var pair in pairs) parts.Add(pair.Key + between + pair.Value);
            return string.Join(",   ", parts);
        }

        public void ToggleUnlocks()
        {
            if (UnlocksPanel == null) return;
            UnlocksPanel.SetActive(!UnlocksPanel.activeSelf);
            var label = UnlocksButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = (UnlocksPanel.activeSelf ? "HIDE UNLOCKS" : "SHOW UNLOCKS") + $" ({Results.Unlocks.Count})";
            // The panel was built after the areas and before the button row, so it draws over the areas and under the buttons.
            if (UnlocksPanel.activeSelf) Relayout();
        }

        private static void Focus(Selectable target)
        {
            var events = UnityEngine.EventSystems.EventSystem.current;
            if (events != null && target != null) events.SetSelectedGameObject(target.gameObject);
        }

        // ---------- building blocks ----------

        private static RectTransform Area(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private RectTransform Panel(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            UITheme.ApplyPanel(image); // the theme's panel; the backdrop and buttons restyle it
            return rect;
        }

        private TextMeshProUGUI Text(RectTransform parent, string name, string content, float size, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (_font != null) tmp.font = _font;
            // A size before auto-size: TMP's defaults (up to 72) replaced the limits on screens built at runtime.
            tmp.fontSize = size;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = Mathf.Min(12f, size);
            tmp.fontSizeMax = size;
            tmp.alignment = align;
            UITheme.ApplyText(tmp, ThemedElement.TextRole.Body);
            tmp.richText = true;
            tmp.raycastTarget = false;
            tmp.text = content;
            _baseSizes[tmp] = size; // the touch floor is applied on every layout
            return tmp;
        }

        private Button MakeButton(RectTransform parent, string name, string label, UITheme.ButtonKind kind)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(UITheme.Layout.ButtonWidth, UITheme.Layout.ButtonHeight);
            var image = go.GetComponent<Image>();
            image.raycastTarget = true;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            var text = Text(rect, "Label", label, ButtonLabelSize, TextAlignmentOptions.Center);
            _baseSizes.Remove(text); // the theme sizes button labels
            text.fontStyle = FontStyles.Bold;
            UITheme.ApplyButton(button, image, kind, kind == UITheme.ButtonKind.Primary ? UITheme.IconSlot.Confirm : UITheme.IconSlot.None);
            return button;
        }
    }
}
