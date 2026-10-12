using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SoulHunter.Gameplay.Pickups;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// The pause screen's map, the VS Milky Way Map in Soul Hunter's own look and plain words ("Map").
    ///
    /// vampire.survivors.wiki/w/Milky_Way_Map (raw, fetched 2026-10-12):
    /// - "it permanently unlocks a viewable map in the pause menu. The map displays the player's location as well as the
    ///   locations of pickups, stage items, relics, coffins, and Treasure Chests in any stage";
    /// - "A "Show Guides"/"Hide Guides" button is also added to the pause menu, which, when toggled on, displays an arrow
    ///   pointing to each item with that item's icon" (the arrows are <see cref="MapGuides"/>);
    /// - "Once the Milky Way Map has been collected, it will no longer appear on the map": a collected thing leaves the map;
    /// - gallery: a question mark sprite is "used to mark coffin locations and other "unknown" things";
    /// - trivia: "A square or rectangle depicted in the map is called a tileset"; its "Interface" section is "TBA", so the
    ///   wiki does not describe how the map is drawn.
    /// What is gathered, and what is left out, is in <see cref="MapMarkers"/>.
    ///
    /// Balance method where the wiki leaves Soul Hunter free (owner rule, 2026-10-09):
    /// - Access. VS unlocks it with the Milky Way Map relic. As with the Evolutions list (2026-10-09) and the run modes
    ///   (owner: "shuru se khule"), the map is open from the start, behind a "Map" button in the pause menu; a relic can
    ///   gate the button later without moving it.
    /// - The drawing. A VS stage is a finite map; Soul Hunter's ground has no edge, so the map is a square of ground with
    ///   the hero always at its centre, north (up the screen, world +Z) at the top. The lines are the 20-unit floor tiles.
    ///   The square shows 60 to 120 units each way, enough for every mark when it can (a level's items lie 39 to 60 units
    ///   out per axis); a mark farther than that sits on the border, in its direction, and the note says so.
    /// - Marks. Each kind has its own shape and letter, and the legend names it in words with its count, so nothing is
    ///   told by colour alone (ui-ux-pro-max). A stage weapon or passive shows its own icon, as it lies on the ground.
    ///   Caskets and unknown things are the "?" the wiki describes.
    /// - "Items left: n" counts the stage items still on the ground (the wiki has no such line; it is the map's one
    ///   number, so the player need not count marks).
    /// - The Guides button sits on this screen beside Back, not in the pause column: the column is full on a phone held
    ///   sideways, and the arrows belong to the map.
    ///
    /// Responsive (owner, 2026-10-10: landscape and portrait from the same screen): the window sits in the safe area; the
    /// map and the legend sit side by side on a wide screen and stacked on a tall one (AdaptiveSplit); the map stays
    /// square; the legend scrolls when it is taller than its room; Back and Guides are a ThemedRow pinned to the bottom.
    /// The marks and legend pictures are shapes from the theme; the design waits for the owner's approval.
    /// Not built: per-level zoom (VS 1.2.0 zooms three stages in; Soul Hunter's levels share one layout).
    /// </summary>
    public sealed class PauseMapScreen : MonoBehaviour
    {
        public const string CanvasName = "PauseMapCanvas";
        /// <summary>The same layer as the Evolutions list (only one of the two is open at a time).</summary>
        public const int ScreenSortingOrder = 350;
        public const string TitleLine = "Map";
        public const string NoteLine = "You are at the centre. A mark on the border is farther away than the map shows.";
        public const string ItemsLeftPrefix = "Items left: ";
        public const string GuidesOn = "Guides: ON", GuidesOff = "Guides: OFF";

        /// <summary>World units between the map's lines: one floor tile.</summary>
        public const float TileUnits = 20f;
        /// <summary>The least and most ground the map shows each way from the hero (world units).</summary>
        public const float MinRange = 60f, MaxRange = 120f;
        // Sizes in canvas units.
        public const float Margin = 32f, Pad = 24f, TitleHeight = 64f, LineHeight = 36f, GapY = 10f;
        public const float MarkSize = 40f, HeroScale = 1.25f, LegendRowHeight = 52f, LineWidth = 2f;

        private enum Shape { Circle, Square, Diamond }

        /// <summary>One mark on the map.</summary>
        public sealed class MarkerView
        {
            public MapMarkerKind Kind;
            public Vector3 World;
            public Object Source;
            public RectTransform Root;
            /// <summary>The letter drawn on the mark ("" when it shows a picture instead).</summary>
            public string Letter;
            /// <summary>True when the thing is farther than the map shows and the mark sits on the border.</summary>
            public bool OnBorder;
        }

        /// <summary>One line of the legend: a kind, how many there are, and its words.</summary>
        public sealed class LegendRow
        {
            public MapMarkerKind Kind;
            public int Count;
            public RectTransform Root;
            public RectTransform Sample;
            public TextMeshProUGUI Text;
        }

        public RectTransform Window { get; private set; }
        /// <summary>The square of ground: the hero at its centre, north at the top.</summary>
        public RectTransform Field { get; private set; }
        public Button BackButton { get; private set; }
        /// <summary>Turns the in-play guide arrows on or off (VS "Show Guides"/"Hide Guides").</summary>
        public Button GuidesButton { get; private set; }
        public TextMeshProUGUI TitleText { get; private set; }
        public TextMeshProUGUI ItemsLeftText { get; private set; }
        public TextMeshProUGUI NoteText { get; private set; }
        public ScrollRect LegendScroll { get; private set; }
        public RectTransform ButtonRow { get; private set; }
        public readonly List<MarkerView> Markers = new List<MarkerView>();
        public readonly List<LegendRow> Legend = new List<LegendRow>();
        /// <summary>World units from the hero to the map's border.</summary>
        public float Range { get; private set; } = MinRange;
        /// <summary>Stage items still on the ground when the map was opened.</summary>
        public int ItemsLeft { get; private set; }
        public bool IsOpen => gameObject.activeSelf;
        /// <summary>True when the map and the legend are stacked (a tall screen).</summary>
        public bool IsStacked => _split != null && _split.IsTall;
        public string GuidesText => _guidesLabel != null ? _guidesLabel.text : null;

        private readonly List<(TextMeshProUGUI Text, float Min)> _texts = new List<(TextMeshProUGUI, float)>();
        private readonly List<RectTransform> _lines = new List<RectTransform>();
        private RectTransform _body, _mapArea, _legendArea, _legendContent, _markLayer, _lineLayer;
        private AdaptiveSplit _split;
        private TextMeshProUGUI _guidesLabel;
        private System.Action _onClosed;
        private Vector3 _centre;
        private Vector2 _lastWindow = new Vector2(-1f, -1f);
        private float _lastRow = -1f;

        /// <summary>Builds the screen once, hidden, on its own root overlay canvas (a nested canvas ignores its scaler).</summary>
        public static PauseMapScreen Create()
        {
            var go = new GameObject(CanvasName, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = ScreenSortingOrder;
            ResponsiveCanvas.Apply(canvas); // the game's one UI scale (UiScreen), landscape and portrait
            go.AddComponent<GraphicRaycaster>();
            var screen = go.AddComponent<PauseMapScreen>();
            screen.Build();
            go.SetActive(false);
            return screen;
        }

        /// <summary>Opens the map as the world is now; <paramref name="onClosed"/> runs on Back.</summary>
        public void Open(System.Action onClosed)
        {
            _onClosed = onClosed;
            gameObject.SetActive(true);
            Refresh();
            Select(BackButton);
        }

        public void Close()
        {
            if (!gameObject.activeSelf) return;
            gameObject.SetActive(false);
            var closed = _onClosed;
            _onClosed = null;
            closed?.Invoke();
        }

        /// <summary>Reads the world again and redraws the marks, the count and the legend (the game is paused, so once is enough).</summary>
        public void Refresh()
        {
            var marks = MapMarkers.Collect();
            _centre = Vector3.zero;
            foreach (var mark in marks)
                if (mark.Kind == MapMarkerKind.Hero) { _centre = mark.Position; break; }

            // The ground shown: enough for the farthest mark when it can, in whole tiles, within the limits.
            float farthest = 0f;
            foreach (var mark in marks)
                farthest = Mathf.Max(farthest, Mathf.Max(Mathf.Abs(mark.Position.x - _centre.x), Mathf.Abs(mark.Position.z - _centre.z)));
            Range = Mathf.Clamp(Mathf.Ceil(farthest * 1.08f / TileUnits) * TileUnits, MinRange, MaxRange);

            foreach (var view in Markers) if (view.Root != null) Destroy(view.Root.gameObject);
            Markers.Clear();
            ItemsLeft = 0;
            var counts = new Dictionary<MapMarkerKind, int>();
            var names = new Dictionary<MapMarkerKind, List<string>>();
            // The hero is drawn last, on top of whatever lies under their feet.
            for (int pass = 0; pass < 2; pass++)
                foreach (var mark in marks)
                {
                    bool hero = mark.Kind == MapMarkerKind.Hero;
                    if (hero != (pass == 1)) continue;
                    var view = new MarkerView { Kind = mark.Kind, World = mark.Position, Source = mark.Source };
                    view.Root = MakeMark(_markLayer, mark.Kind, mark.Icon, out view.Letter);
                    view.Root.name = "Mark_" + mark.Kind;
                    Markers.Add(view);
                    counts[mark.Kind] = counts.TryGetValue(mark.Kind, out int n) ? n + 1 : 1;
                    if (mark.Kind == MapMarkerKind.StageItem) ItemsLeft++;
                    if (!string.IsNullOrEmpty(mark.Name) && mark.Kind == MapMarkerKind.Pickup)
                    {
                        if (!names.TryGetValue(mark.Kind, out var list)) names[mark.Kind] = list = new List<string>();
                        if (!list.Contains(mark.Name)) list.Add(mark.Name);
                    }
                }

            ItemsLeftText.text = ItemsLeftPrefix + ItemsLeft;
            BuildLegend(counts, names);
            RefreshGuides();
            ApplyTextFloor();
            Relayout();
        }

        /// <summary>
        /// Where a thing <paramref name="offset"/> from the hero (world X and Z) sits on a square map of
        /// <paramref name="fieldSize"/> units that shows <paramref name="range"/> world units each way: from the map's
        /// centre, right for +X and up for +Z. A thing past the range is held on the border, in its direction.
        /// </summary>
        public static Vector2 FieldPoint(Vector3 offset, float range, float fieldSize, out bool onBorder)
        {
            var flat = new Vector2(offset.x, offset.z);
            float reach = Mathf.Max(Mathf.Abs(flat.x), Mathf.Abs(flat.y));
            onBorder = range > 0f && reach > range;
            if (onBorder) flat *= range / reach;
            return range > 0f ? flat / range * (fieldSize * 0.5f) : Vector2.zero;
        }

        private void OnEnable()
        {
            UiScreen.Changed += Relayout;
            UiThemeAsset.Changed += Recolour;
        }

        private void OnDisable()
        {
            UiScreen.Changed -= Relayout;
            UiThemeAsset.Changed -= Recolour;
        }

        private void LateUpdate()
        {
            if (Window == null) return;
            if (Window.rect.size != _lastWindow || !Mathf.Approximately(RowHeight(), _lastRow)) Relayout();
        }

        private float RowHeight() => ButtonRow != null ? ButtonRow.rect.height * ButtonRow.localScale.y : 0f;

        // ---------------------------------------------------------------- building

        private void Build()
        {
            var root = (RectTransform)transform;
            var veil = UITheme.AddVeil(root); // full screen, under the safe content
            if (veil != null) veil.raycastTarget = true; // the pause menu underneath takes no clicks while this is open

            var content = Area("Content", root);
            SafeAreaFitter.Fit(content);
            Window = Area("Window", content);
            UITheme.ApplyPanel(Window.gameObject.AddComponent<Image>(), 1f);
            Window.GetComponent<Image>().raycastTarget = false;
            Window.offsetMin = new Vector2(Margin, Margin);
            Window.offsetMax = new Vector2(-Margin, -Margin);

            TitleText = Text(Window, "Title", TitleLine, 54f, TextAlignmentOptions.MidlineLeft);
            UITheme.ApplyText(TitleText, ThemedElement.TextRole.Title);
            ItemsLeftText = Text(Window, "ItemsLeft", ItemsLeftPrefix + "0", 30f, TextAlignmentOptions.MidlineRight);
            UITheme.ApplyText(ItemsLeftText, ThemedElement.TextRole.Accent);
            NoteText = Text(Window, "Note", NoteLine, 24f, TextAlignmentOptions.TopLeft);
            NoteText.enableAutoSizing = false; // one size, wrapped: its height follows the width
            UITheme.ApplyText(NoteText, ThemedElement.TextRole.Soft);

            _body = Area("Body", Window);
            _mapArea = Area("MapArea", _body);
            _legendArea = Area("LegendArea", _body);
            _split = AdaptiveSplit.Between(_body, _mapArea, _legendArea, 0.6f, 0.6f);
            _split.TallWhenAspectBelow = 1.2f; // the map is square: side by side only when the room is clearly wider than tall

            Field = Area("Field", _mapArea);
            var ground = Field.gameObject.AddComponent<Image>();
            ground.raycastTarget = false;
            UITheme.ApplySurface(ground, ThemedElement.SurfaceRole.SlotEmpty);
            Field.gameObject.AddComponent<RectMask2D>(); // lines and marks never draw past the map's edge
            _lineLayer = Area("Lines", Field);
            _markLayer = Area("Marks", Field);

            var legendList = Area("Legend", _legendArea);
            LegendScroll = ResponsiveScroll.Create(legendList, "LegendViewport", "LegendRows", out _legendContent, ResponsiveScroll.Bar.Touch);

            ButtonRow = new GameObject("Buttons", typeof(RectTransform)).GetComponent<RectTransform>();
            ButtonRow.SetParent(Window, false);
            ButtonRow.anchorMin = ButtonRow.anchorMax = new Vector2(0.5f, 0f);
            ButtonRow.pivot = new Vector2(0.5f, 0f);
            ButtonRow.anchoredPosition = new Vector2(0f, Pad);
            ButtonRow.sizeDelta = new Vector2(0f, UITheme.Layout.ButtonHeight);
            BackButton = MakeButton(ButtonRow, "Back_Button", "Back", UITheme.ButtonKind.Primary, UITheme.IconSlot.Back, Close);
            GuidesButton = MakeButton(ButtonRow, "Guides_Button", GuidesOff, UITheme.ButtonKind.Normal, UITheme.IconSlot.None, ToggleGuides);
            _guidesLabel = GuidesButton.GetComponentInChildren<TextMeshProUGUI>();
            ButtonRow.gameObject.AddComponent<ThemedRow>();
        }

        /// <summary>Turns the guide arrows on or off; the button says the state in words (never by colour alone).</summary>
        public void ToggleGuides()
        {
            MapGuides.Enabled = !MapGuides.Enabled;
            RefreshGuides();
        }

        private void RefreshGuides()
        {
            if (_guidesLabel != null) _guidesLabel.text = MapGuides.Enabled ? GuidesOn : GuidesOff;
        }

        // ---------------------------------------------------------------- marks

        private static void StyleOf(MapMarkerKind kind, out Shape shape, out string letter, out ThemedElement.SurfaceRole plate,
            out ThemedElement.TextRole text, out float scale)
        {
            // One shape-and-letter pair per kind. Light plates (the round knob) take the theme's dark text, dark plates its light text.
            scale = 1f;
            plate = ThemedElement.SurfaceRole.SlotFilled;
            text = ThemedElement.TextRole.Body;
            switch (kind)
            {
                case MapMarkerKind.Hero: shape = Shape.Circle; letter = "You"; plate = ThemedElement.SurfaceRole.Knob; text = ThemedElement.TextRole.Card; scale = HeroScale; break;
                case MapMarkerKind.StageItem: shape = Shape.Square; letter = "S"; break;
                case MapMarkerKind.Pickup: shape = Shape.Diamond; letter = "P"; break;
                case MapMarkerKind.Chest: shape = Shape.Square; letter = "C"; plate = ThemedElement.SurfaceRole.Card; text = ThemedElement.TextRole.Card; break;
                case MapMarkerKind.RunePlate: shape = Shape.Circle; letter = "R"; plate = ThemedElement.SurfaceRole.Knob; text = ThemedElement.TextRole.Card; scale = 0.85f; break;
                case MapMarkerKind.Lantern: shape = Shape.Circle; letter = "L"; plate = ThemedElement.SurfaceRole.Knob; text = ThemedElement.TextRole.Card; scale = 0.7f; break;
                case MapMarkerKind.Merchant: shape = Shape.Diamond; letter = "M"; plate = ThemedElement.SurfaceRole.Card; text = ThemedElement.TextRole.Card; scale = 1.1f; break;
                case MapMarkerKind.Boss: shape = Shape.Diamond; letter = "B"; text = ThemedElement.TextRole.Danger; scale = 1.3f; break;
                case MapMarkerKind.Relic: shape = Shape.Square; letter = "!"; text = ThemedElement.TextRole.Accent; scale = 1.1f; break;
                default: shape = Shape.Square; letter = "?"; text = ThemedElement.TextRole.Accent; break; // caskets and unknown things (wiki: the question mark)
            }
        }

        /// <summary>The legend's words for a kind (Soul Hunter's own names).</summary>
        public static string WordsFor(MapMarkerKind kind)
        {
            switch (kind)
            {
                case MapMarkerKind.Hero: return "You";
                case MapMarkerKind.StageItem: return "Stage item (its own icon, or S)";
                case MapMarkerKind.Pickup: return "Pickup";
                case MapMarkerKind.Chest: return "Soul Reliquary";
                case MapMarkerKind.RunePlate: return "Rune Plate";
                case MapMarkerKind.Lantern: return "Soul Lantern";
                case MapMarkerKind.Merchant: return "Wandering Merchant";
                case MapMarkerKind.Boss: return "Level boss";
                case MapMarkerKind.Relic: return "Relic";
                case MapMarkerKind.Casket: return "Soul Casket";
                default: return "Unknown";
            }
        }

        /// <summary>The size of a mark's box: big enough for its letter at the smallest text a phone may show.</summary>
        private static float MarkUnits => Mathf.Max(MarkSize, UiScreen.MinTextUnits * 2.2f);

        /// <summary>A mark: the kind's shape with its letter, or the thing's own picture on the shape.</summary>
        private RectTransform MakeMark(RectTransform parent, MapMarkerKind kind, Sprite icon, out string letter)
        {
            StyleOf(kind, out var shape, out letter, out var plateRole, out var textRole, out float scale);
            float size = MarkUnits * scale;
            var root = new GameObject("Mark", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(parent, false);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(size, size);

            var plate = new GameObject("Plate", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            plate.SetParent(root, false);
            plate.anchorMin = plate.anchorMax = plate.pivot = new Vector2(0.5f, 0.5f);
            // A diamond is a square turned on its corner; it is drawn smaller so its corners stay inside the mark's box.
            float plateSize = shape == Shape.Diamond ? size * 0.72f : size;
            plate.sizeDelta = new Vector2(plateSize, plateSize);
            if (shape == Shape.Diamond) plate.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var plateImage = plate.GetComponent<Image>();
            plateImage.raycastTarget = false;
            UITheme.ApplySurface(plateImage, plateRole);
            UITheme.Edge(plate.gameObject, kind == MapMarkerKind.Hero ? UITheme.Hud.SlotEvolved : UITheme.Hud.SlotBorder, kind == MapMarkerKind.Hero ? 3f : 1.5f);

            if (icon != null)
            {
                letter = "";
                var picture = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                picture.SetParent(root, false);
                picture.anchorMin = new Vector2(0.1f, 0.1f);
                picture.anchorMax = new Vector2(0.9f, 0.9f);
                picture.offsetMin = picture.offsetMax = Vector2.zero;
                var pictureImage = picture.GetComponent<Image>();
                pictureImage.sprite = icon;
                pictureImage.preserveAspect = true;
                pictureImage.raycastTarget = false;
            }
            else
            {
                var label = Text(root, "Letter", letter, size * 0.62f, TextAlignmentOptions.Center);
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Overflow;
                UITheme.ApplyText(label, textRole);
            }
            return root;
        }

        private void BuildLegend(Dictionary<MapMarkerKind, int> counts, Dictionary<MapMarkerKind, List<string>> names)
        {
            foreach (var row in Legend) if (row.Root != null) Destroy(row.Root.gameObject);
            Legend.Clear();
            foreach (MapMarkerKind kind in System.Enum.GetValues(typeof(MapMarkerKind)))
            {
                if (!counts.TryGetValue(kind, out int count)) continue;
                var row = new LegendRow { Kind = kind, Count = count };
                row.Root = Area("Legend_" + kind, _legendContent);
                var back = row.Root.gameObject.AddComponent<Image>();
                back.raycastTarget = false;
                UITheme.ApplySurface(back, ThemedElement.SurfaceRole.Row);
                row.Sample = MakeMark(row.Root, kind, null, out _);
                row.Sample.name = "Sample";
                string words = WordsFor(kind);
                if (kind != MapMarkerKind.Hero) words += " x" + count;
                if (names.TryGetValue(kind, out var list) && list.Count > 0) words += ": " + string.Join(", ", list);
                row.Text = Text(row.Root, "Words", words, 26f, TextAlignmentOptions.MidlineLeft);
                Legend.Add(row);
            }
        }

        // ---------------------------------------------------------------- layout

        /// <summary>
        /// Places the header, the buttons, the map and the legend for the window's size, then every mark on the map. Runs
        /// on open, on a rotation or window change (UiScreen.Changed) and when the window's size changes.
        /// </summary>
        public void Relayout()
        {
            if (Window == null) return;
            _lastWindow = Window.rect.size;
            _lastRow = RowHeight();
            ApplyTextFloor();
            float width = Window.rect.width - 2f * Pad;
            bool wide = width >= 760f;

            // Header: the title with the count on its right (wide) or under it (narrow), then the note.
            float top = Pad;
            PlaceTop(TitleText.rectTransform, top, TitleHeight, 0f, wide ? 0.4f : 0f);
            if (wide) PlaceTop(ItemsLeftText.rectTransform, top, TitleHeight, 0.6f, 0f);
            top += TitleHeight;
            if (!wide)
            {
                PlaceTop(ItemsLeftText.rectTransform, top, LineHeight, 0f, 0f);
                top += LineHeight;
            }
            ItemsLeftText.alignment = wide ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            float note = Mathf.Ceil(NoteText.GetPreferredValues(NoteText.text, Mathf.Max(1f, width), 0f).y) + 4f;
            PlaceTop(NoteText.rectTransform, top, note, 0f, 0f);
            top += note;

            // The map and the legend fill the room between the note and the buttons.
            _body.anchorMin = Vector2.zero;
            _body.anchorMax = Vector2.one;
            _body.offsetMin = new Vector2(Pad, Pad + _lastRow + UITheme.Layout.ButtonGap);
            _body.offsetMax = new Vector2(-Pad, -(top + GapY));
            Canvas.ForceUpdateCanvases();
            _split.Layout(force: true);
            Canvas.ForceUpdateCanvases();

            // The map: the largest square its area holds, centred.
            Vector2 room = _mapArea.rect.size;
            float side = Mathf.Max(0f, Mathf.Floor(Mathf.Min(room.x, room.y)));
            Field.anchorMin = Field.anchorMax = Field.pivot = new Vector2(0.5f, 0.5f);
            Field.sizeDelta = new Vector2(side, side);
            Field.anchoredPosition = Vector2.zero;

            LayoutLines(side);
            foreach (var view in Markers)
            {
                if (view.Root == null) continue;
                view.Root.anchoredPosition = Clamped(FieldPoint(view.World - _centre, Range, side, out view.OnBorder), side, view.Root.sizeDelta.x);
            }

            ResponsiveScroll.SizeTouchBar(LegendScroll);
            Canvas.ForceUpdateCanvases();
            LayoutLegend(LegendScroll.viewport.rect.width);
        }

        /// <summary>A mark's centre kept far enough inside the map that the whole mark shows.</summary>
        private static Vector2 Clamped(Vector2 point, float side, float markSize)
        {
            float limit = Mathf.Max(0f, side * 0.5f - markSize * 0.5f);
            return new Vector2(Mathf.Clamp(point.x, -limit, limit), Mathf.Clamp(point.y, -limit, limit));
        }

        /// <summary>The floor tiles as thin lines, where they lie around the hero.</summary>
        private void LayoutLines(float side)
        {
            int used = 0;
            if (side > 0f && Range > 0f)
            {
                float unit = side * 0.5f / Range; // canvas units per world unit
                for (int axis = 0; axis < 2; axis++)
                {
                    float centre = axis == 0 ? _centre.x : _centre.z;
                    float first = Mathf.Ceil((centre - Range) / TileUnits) * TileUnits;
                    for (float world = first; world <= centre + Range; world += TileUnits)
                    {
                        var line = Line(used++);
                        float at = (world - centre) * unit;
                        line.sizeDelta = axis == 0 ? new Vector2(LineWidth, side) : new Vector2(side, LineWidth);
                        line.anchoredPosition = axis == 0 ? new Vector2(at, 0f) : new Vector2(0f, at);
                    }
                }
            }
            for (int i = 0; i < _lines.Count; i++) _lines[i].gameObject.SetActive(i < used);
        }

        private RectTransform Line(int index)
        {
            while (_lines.Count <= index)
            {
                var line = new GameObject("Line", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                line.SetParent(_lineLayer, false);
                line.anchorMin = line.anchorMax = line.pivot = new Vector2(0.5f, 0.5f);
                var image = line.GetComponent<Image>();
                image.raycastTarget = false;
                image.color = LineColour();
                _lines.Add(line);
            }
            return _lines[index];
        }

        /// <summary>The theme's slot border, fainter: the lines are a quiet backdrop for the marks.</summary>
        private static Color LineColour()
        {
            Color colour = UITheme.Hud.SlotBorder;
            colour.a *= 0.45f;
            return colour;
        }

        private void Recolour()
        {
            foreach (var line in _lines)
                if (line != null) line.GetComponent<Image>().color = LineColour();
        }

        private void LayoutLegend(float width)
        {
            float rowHeight = Mathf.Max(LegendRowHeight, MarkUnits * 1.3f + 8f);
            float sampleRoom = MarkUnits * 1.3f + 2f * GapY;
            float y = 0f;
            foreach (var row in Legend)
            {
                var rect = row.Root;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(0f, rowHeight);
                rect.anchoredPosition = new Vector2(0f, -y);
                row.Sample.anchorMin = row.Sample.anchorMax = new Vector2(0f, 0.5f);
                row.Sample.pivot = new Vector2(0.5f, 0.5f);
                row.Sample.anchoredPosition = new Vector2(sampleRoom * 0.5f, 0f);
                var words = row.Text.rectTransform;
                words.anchorMin = Vector2.zero;
                words.anchorMax = Vector2.one;
                words.offsetMin = new Vector2(sampleRoom, 2f);
                words.offsetMax = new Vector2(-GapY, -2f);
                y += rowHeight + GapY;
            }
            _legendContent.sizeDelta = new Vector2(0f, Mathf.Max(0f, y - GapY));
        }

        private static void PlaceTop(RectTransform rect, float fromTop, float height, float leftShare, float rightShare)
        {
            rect.anchorMin = new Vector2(leftShare, 1f);
            rect.anchorMax = new Vector2(1f - rightShare, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(Pad, -(fromTop + height));
            rect.offsetMax = new Vector2(-Pad, -fromTop);
        }

        /// <summary>On a touch screen no text is under 12 sp (UiScreen.MinTextUnits); on a PC each keeps its own sizes.</summary>
        private void ApplyTextFloor()
        {
            float floor = UiScreen.MinTextUnits;
            for (int i = _texts.Count - 1; i >= 0; i--)
            {
                var (text, min) = _texts[i];
                if (text == null) { _texts.RemoveAt(i); continue; }
                if (text.enableAutoSizing) text.fontSizeMin = Mathf.Max(min, floor);
                else text.fontSize = Mathf.Max(text.fontSizeMax, floor);
                if (text.fontSizeMax < text.fontSizeMin) text.fontSizeMax = text.fontSizeMin;
            }
        }

        // ---------------------------------------------------------------- helpers

        private static RectTransform Area(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
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
            // A size before auto-size: TMP's defaults (up to 72) replaced the limits on screens built at runtime.
            tmp.fontSize = size;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = Mathf.Min(14f, size);
            tmp.fontSizeMax = size;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            UITheme.ApplyText(tmp, ThemedElement.TextRole.Body);
            tmp.raycastTarget = false;
            tmp.text = content;
            _texts.Add((tmp, tmp.fontSizeMin));
            return tmp;
        }

        private Button MakeButton(RectTransform parent, string name, string label, UITheme.ButtonKind kind, UITheme.IconSlot icon, System.Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(UITheme.Layout.ButtonWidth, UITheme.Layout.ButtonHeight);
            var image = go.GetComponent<Image>();
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() =>
            {
                var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
                if (audio != null) audio.PlaySFX(audio.UIConfirmSound);
                onClick();
            });
            var text = Text(rect, "Text", label, 34f, TextAlignmentOptions.Center);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            _texts.RemoveAt(_texts.Count - 1); // a button's label is sized by the theme (ThemedElement), not here
            UITheme.ApplyButton(button, image, kind, icon); // after the label exists, so it styles it too
            return button;
        }

        private static void Select(Selectable target)
        {
            if (target != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(target.gameObject);
        }
    }
}
