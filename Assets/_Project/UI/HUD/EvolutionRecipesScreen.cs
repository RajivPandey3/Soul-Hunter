using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Release item P3-06 (2026-10-09): the pause screen's list of evolution recipes, the VS Grimoire in Soul Hunter's own
    /// look and plain words ("Evolutions"; owner, 2026-10-09: no renames, plain English labels).
    ///
    /// vampire.survivors.wiki/w/Grim_Grimoire (raw, fetched 2026-10-09):
    /// - "view all the evolutions and unions that have been discovered on their save file, as well as what weapons and
    ///   passive items are needed to obtain them";
    /// - it "shows evolutions that have been obtained before, and hides any evolutions that have yet to be encountered";
    /// - an obtained item's "sprite in Grim Grimoire will be highlighted with a yellow-greenish square"; an evolution made
    ///   in the run has its equation "entirely highlighted";
    /// - with full slots, recipes that can no longer be completed by levelling up "will look more faded";
    /// - "The list of evolutions is separated by pagination".
    ///
    /// Balance method where the wiki leaves Soul Hunter free (owner rule, 2026-10-09):
    /// - Access. VS unlocks it with the Grim Grimoire relic. Soul Hunter has no relics yet (owner, 2026-09-30: the VS unlock
    ///   system comes later), so it is open from the start, behind an "Evolutions" button just left of Resume: the wiki's
    ///   layout when both the Grimoire and the map are owned ("the "Open Grimoire" button, just to the left of the "RESUME"
    ///   button"). That keeps the existing pause menu and its stats, and a relic can gate the button later without moving it.
    /// - Undiscovered recipes. VS leaves them out; the task asks for them as unknown, so they show as "?" rows after the
    ///   discovered ones. With no Collection screen yet, that is the only place the player sees how many are left to find.
    /// - Held items. The theme's chosen-item frame stands for VS's yellow-green square, and each held slot is also labelled
    ///   "Held" and named on the row, so it is never told by colour alone (ui-ux-pro-max).
    /// - Eight recipes per page.
    ///
    /// Responsive (owner, 2026-10-10: landscape and portrait from the same screen). The window sits in the safe area; Back,
    /// Previous and Next are pinned to its bottom in a ThemedRow (they wrap onto more lines, never shrink); the page's rows
    /// take two columns when the width allows (two columns of four on a PC) and one otherwise, and scroll when they are
    /// taller than the room (a phone). A row shows its equation beside its name when it is wide enough, and under its name
    /// on a narrow screen. Text never goes under the touch floor (12 sp). It lays itself out again on a rotation.
    /// Not built: the Milky Way Map's pause map and its Show/Hide Guides arrows (a separate release item), and the Arcana
    /// tooltip the Grimoire's update history mentions.
    /// </summary>
    public sealed class EvolutionRecipesScreen : MonoBehaviour
    {
        public const string CanvasName = "EvolutionRecipesCanvas";
        /// <summary>Above the HUD and its pause menu, below the level-up (300 is lower, but cannot open while paused here),
        /// chest (400), revive (420), results (450) and unlock notices (500).</summary>
        public const int ScreenSortingOrder = 350;
        public const int Columns = 2, RowsPerColumn = 4, RowsPerPage = Columns * RowsPerColumn;
        public const string TitleLine = "Evolutions";
        public const string NoteLine = "Recipes you have made from a chest. Items you hold are marked Held.";
        public const string UnknownName = "Not found yet";
        public const string UnknownStatus = "Evolve it from a chest to see the recipe.";
        public const string MadeStatus = "Made this run";
        public const string FadedStatus = "Slots full: cannot be made this run";
        public const string HeldLabel = "Held";

        // Sizes in canvas units. A slot shrinks from its full size to its smallest so a long recipe fits a narrow row.
        public const float SlotSize = 76f, MinSlotSize = 52f, SignWidth = 34f, Inset = 16f;
        public const float Margin = 32f, Pad = 24f, TitleHeight = 64f, PageHeight = 36f, HeldHeight = 24f;
        public const float NameHeight = 40f, StatusHeight = 30f, RowPad = 10f, GapX = 12f, GapY = 10f;
        /// <summary>The room a row keeps for its name and status when the equation sits beside them.</summary>
        public const float TextRoom = 240f;
        /// <summary>Under this window width the page number goes under the title.</summary>
        public const float WideHeaderWidth = 760f;

        /// <summary>One shown recipe row.</summary>
        public sealed class RowView
        {
            public EvolutionRecipe Recipe;
            public EvolutionRecipes.RowState State;
            public RectTransform Root;
            public Image Background;
            public TextMeshProUGUI NameText, StatusText;
            /// <summary>The item slots (base weapon first), then the result slot last.</summary>
            public readonly List<Image> Slots = new List<Image>();
            /// <summary>The item types whose slot is marked held.</summary>
            public readonly List<UpgradeData.UpgradeType> HeldItems = new List<UpgradeData.UpgradeType>();
            public readonly List<TextMeshProUGUI> HeldLabels = new List<TextMeshProUGUI>();
            public CanvasGroup SlotGroup;
            /// <summary>The "+" and "=" signs, in order (one after each item slot).</summary>
            public readonly List<RectTransform> Signs = new List<RectTransform>();
            /// <summary>For each held label, the index of the slot it sits under.</summary>
            public readonly List<int> HeldSlot = new List<int>();
            /// <summary>True when the equation sits beside the name (a wide row), false when under it.</summary>
            public bool Beside = true;
        }

        public Button PreviousButton { get; private set; }
        public Button NextButton { get; private set; }
        public Button BackButton { get; private set; }
        public TextMeshProUGUI TitleText { get; private set; }
        public TextMeshProUGUI NoteText { get; private set; }
        public TextMeshProUGUI PageText { get; private set; }
        public RectTransform Window { get; private set; }
        /// <summary>The scrolling area that holds the page's rows.</summary>
        public ScrollRect Scroll { get; private set; }
        /// <summary>The row of Previous / Back / Next, pinned to the bottom of the window.</summary>
        public RectTransform ButtonRow { get; private set; }
        /// <summary>How many columns the rows took at the last layout.</summary>
        public int ShownColumns { get; private set; } = Columns;
        public int Page { get; private set; }
        public int PageCount => Mathf.Max(1, (_recipes.Count + RowsPerPage - 1) / RowsPerPage);
        public bool IsOpen => gameObject.activeSelf;
        /// <summary>The rows on the open page.</summary>
        public readonly List<RowView> Rows = new List<RowView>();
        /// <summary>Every recipe in list order: discovered ones first, then the unknown ones.</summary>
        public IReadOnlyList<EvolutionRecipe> Recipes => _recipes;

        private readonly List<EvolutionRecipe> _recipes = new List<EvolutionRecipe>();
        // Texts this screen made, with their own smallest size (the touch floor may lift it).
        private readonly List<(TextMeshProUGUI Text, float Min)> _texts = new List<(TextMeshProUGUI, float)>();
        private RectTransform _grid;
        private System.Action _onClosed;
        private Vector2 _lastWindow = new Vector2(-1f, -1f);
        private float _lastRow = -1f;
        private int _maxItems = 2;

        /// <summary>Builds the screen once, hidden, on its own root overlay canvas (a nested canvas ignores its scaler).</summary>
        public static EvolutionRecipesScreen Create()
        {
            var go = new GameObject(CanvasName, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = ScreenSortingOrder;
            SoulHunter.Gameplay.UI.ResponsiveCanvas.Apply(canvas); // the game's one UI scale (UiScreen), landscape and portrait
            go.AddComponent<GraphicRaycaster>();
            var screen = go.AddComponent<EvolutionRecipesScreen>();
            screen.Build();
            go.SetActive(false);
            return screen;
        }

        /// <summary>Opens on the first page with the current run and save; <paramref name="onClosed"/> runs on Back.</summary>
        public void Open(System.Action onClosed)
        {
            _onClosed = onClosed;
            gameObject.SetActive(true);
            CollectRecipes();
            ShowPage(0);
            Select(BackButton);
            _pauseKey.Hold(); // Esc goes back here instead of resuming the game (Bug MIS UI-06)
        }

        public void Close()
        {
            if (!gameObject.activeSelf) return;
            gameObject.SetActive(false);
            var closed = _onClosed;
            _onClosed = null;
            closed?.Invoke();
        }

        public void ShowPage(int page)
        {
            Page = Mathf.Clamp(page, 0, PageCount - 1);
            var manager = WeaponManager.Instance;
            var save = EvolutionRecipes.CurrentSave();
            for (int i = 0; i < Rows.Count; i++)
            {
                int index = Page * RowsPerPage + i;
                var row = Rows[i];
                bool used = index < _recipes.Count;
                row.Root.gameObject.SetActive(used);
                if (used) FillRow(row, _recipes[index], manager, save);
            }
            PageText.text = $"Page {Page + 1} of {PageCount}";
            PreviousButton.interactable = Page > 0;
            NextButton.interactable = Page < PageCount - 1;
            ShowUsable(PreviousButton);
            ShowUsable(NextButton);
            Relayout();
            if (Scroll != null) Scroll.verticalNormalizedPosition = 1f; // a new page starts at its top
        }

        private void CollectRecipes()
        {
            _recipes.Clear();
            var manager = WeaponManager.Instance;
            var save = EvolutionRecipes.CurrentSave();
            var unknown = new List<EvolutionRecipe>();
            _maxItems = 2;
            foreach (var recipe in EvolutionRecipes.All(manager))
            {
                bool known = (manager != null && manager.MadeThisRun(recipe.Id)) ||
                             SoulHunter.Core.Persistence.EvolutionDiscovery.IsDiscovered(save, recipe.Id);
                (known ? _recipes : unknown).Add(recipe);
                _maxItems = Mathf.Max(_maxItems, recipe.Items.Count);
            }
            _recipes.AddRange(unknown);
        }

        private void OnEnable() => UiScreen.Changed += Relayout;

        private void OnDisable()
        {
            UiScreen.Changed -= Relayout;
            _pauseKey.Release(); // every way out puts the Pause action back (Bug MIS UI-06)
        }

        private void OnDestroy() => _pauseKey.Release();

        /// <summary>Esc or gamepad B goes back to the pause menu, as Options does (Bug MIS UI-06).</summary>
        private void Update()
        {
            if (PauseKeyGate.BackPressed()) Close();
        }

        private readonly PauseKeyGate _pauseKey = new PauseKeyGate();
        /// <summary>True while this screen keeps the Pause action off (tests).</summary>
        public bool HoldsPauseKey => _pauseKey.Holding;

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
            Window = Panel(content, "Window", Vector2.zero, Vector2.one);
            Window.offsetMin = new Vector2(Margin, Margin);
            Window.offsetMax = new Vector2(-Margin, -Margin);
            UITheme.ApplyPanel(Window.GetComponent<Image>(), 1f);

            TitleText = Text(Window, "Title", Vector2.zero, Vector2.one, TitleLine, 54f, TextAlignmentOptions.MidlineLeft);
            UITheme.ApplyText(TitleText, ThemedElement.TextRole.Title);
            PageText = Text(Window, "Page", Vector2.zero, Vector2.one, "", 28f, TextAlignmentOptions.MidlineRight);
            NoteText = Text(Window, "Note", Vector2.zero, Vector2.one, NoteLine, 26f, TextAlignmentOptions.TopLeft);
            NoteText.enableAutoSizing = false; // one size, wrapped: its height follows the width
            UITheme.ApplyText(NoteText, ThemedElement.TextRole.Soft);

            // The page's rows, in a scroll area (no bar: on a PC the page fits; a phone scrolls it by dragging).
            var area = Area("Grid", Window);
            Scroll = ResponsiveScroll.Create(area, "Viewport", "Rows", out _grid);
            for (int i = 0; i < RowsPerPage; i++) Rows.Add(BuildRow(i));

            ButtonRow = new GameObject("Buttons", typeof(RectTransform)).GetComponent<RectTransform>();
            ButtonRow.SetParent(Window, false);
            ButtonRow.anchorMin = ButtonRow.anchorMax = new Vector2(0.5f, 0f);
            ButtonRow.pivot = new Vector2(0.5f, 0f);
            ButtonRow.anchoredPosition = new Vector2(0f, Pad);
            ButtonRow.sizeDelta = new Vector2(0f, UITheme.Layout.ButtonHeight);
            PreviousButton = MakeButton(ButtonRow, "Previous_Button", "Previous", UITheme.ButtonKind.Normal, UITheme.IconSlot.None, () => ShowPage(Page - 1));
            BackButton = MakeButton(ButtonRow, "Back_Button", "Back", UITheme.ButtonKind.Primary, UITheme.IconSlot.Back, Close);
            NextButton = MakeButton(ButtonRow, "Next_Button", "Next", UITheme.ButtonKind.Normal, UITheme.IconSlot.None, () => ShowPage(Page + 1));
            ButtonRow.gameObject.AddComponent<ThemedRow>();
        }

        private RowView BuildRow(int index)
        {
            var view = new RowView();
            view.Root = Panel(_grid, "Recipe_" + index, Vector2.zero, Vector2.zero);
            view.Background = view.Root.GetComponent<Image>();
            UITheme.ApplySurface(view.Background, ThemedElement.SurfaceRole.Row);

            var slots = Area("Slots", view.Root);
            view.SlotGroup = slots.gameObject.AddComponent<CanvasGroup>();
            view.SlotGroup.interactable = view.SlotGroup.blocksRaycasts = false;

            view.NameText = Text(view.Root, "Name", Vector2.zero, Vector2.one, "", 30f, TextAlignmentOptions.BottomLeft);
            view.StatusText = Text(view.Root, "Status", Vector2.zero, Vector2.one, "", 22f, TextAlignmentOptions.TopLeft);
            UITheme.ApplyText(view.StatusText, ThemedElement.TextRole.Soft);
            return view;
        }

        // ---------------------------------------------------------------- layout

        /// <summary>
        /// Places the header, the button row, the scroll area and the page's rows for the window's size. Runs on open, on a
        /// page change, on a rotation or window change (UiScreen.Changed) and when the window's size changes. It only moves
        /// things, so the page and its rows stay as they are.
        /// </summary>
        public void Relayout()
        {
            if (Window == null) return;
            _lastWindow = Window.rect.size;
            _lastRow = RowHeight();
            ApplyTextFloor();
            float width = Window.rect.width - 2f * Pad;
            bool wide = width >= WideHeaderWidth;

            // Header: the title on top with the page number on its right (wide) or under it (narrow); the note under both.
            float top = Pad;
            PlaceTop(TitleText.rectTransform, top, TitleHeight, 0f, wide ? 0.35f : 0f);
            if (wide) PlaceTop(PageText.rectTransform, top, TitleHeight, 0.65f, 0f);
            top += TitleHeight;
            if (!wide)
            {
                PlaceTop(PageText.rectTransform, top, PageHeight, 0f, 0f);
                top += PageHeight;
            }
            PageText.alignment = wide ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            float note = Mathf.Ceil(NoteText.GetPreferredValues(NoteText.text, Mathf.Max(1f, width), 0f).y) + 4f;
            PlaceTop(NoteText.rectTransform, top, note, 0f, 0f);
            top += note;

            // The rows fill the room between the note and the buttons.
            var area = (RectTransform)Scroll.transform;
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(Pad, Pad + _lastRow + UITheme.Layout.ButtonGap);
            area.offsetMax = new Vector2(-Pad, -(top + GapY));
            Canvas.ForceUpdateCanvases();
            LayoutRows(Scroll.viewport.rect.size);
        }

        /// <summary>The room under a slot for its "Held" label: a line of the smallest text a phone may show (UiScreen).</summary>
        private static float HeldRoom => Mathf.Max(HeldHeight, UiScreen.MinTextUnits * 1.3f + 2f);

        /// <summary>The page's rows: two columns when each is wide enough for an equation beside its name, otherwise one.</summary>
        private void LayoutRows(Vector2 view)
        {
            if (view.x <= 0f || view.y <= 0f) return;
            float besideNeed = 2f * Inset + (_maxItems + 1) * MinSlotSize + _maxItems * SignWidth + TextRoom;
            int columns = UiScreen.ColumnsFor(view.x, besideNeed, GapX, Columns);
            ShownColumns = columns;
            float columnWidth = (view.x - GapX * (columns - 1)) / columns;
            bool beside = columnWidth >= besideNeed;
            int perColumn = Mathf.CeilToInt(RowsPerPage / (float)columns);
            float stackedSlot = SlotFor(columnWidth, _maxItems, false);
            float minHeight = beside ? SlotSize + HeldRoom + 2f * RowPad + 16f
                : 2f * RowPad + NameHeight + StatusHeight + 8f + stackedSlot + HeldRoom;
            float rowHeight = Mathf.Max(minHeight, (view.y - GapY * (perColumn - 1)) / perColumn);

            int lines = 0;
            for (int i = 0; i < Rows.Count; i++)
            {
                var row = Rows[i];
                int column = i / perColumn, line = i % perColumn;
                var rect = row.Root;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
                rect.sizeDelta = new Vector2(columnWidth, rowHeight);
                rect.anchoredPosition = new Vector2(column * (columnWidth + GapX), -line * (rowHeight + GapY));
                if (!row.Root.gameObject.activeSelf || row.Recipe == null) continue;
                lines = Mathf.Max(lines, line + 1);
                LayoutRow(row, columnWidth, rowHeight, beside);
            }
            _grid.sizeDelta = new Vector2(0f, Mathf.Max(0f, lines * rowHeight + Mathf.Max(0, lines - 1) * GapY));
        }

        /// <summary>A slot size that fits <paramref name="items"/> items and the result in <paramref name="width"/>.</summary>
        private static float SlotFor(float width, int items, bool beside)
        {
            float room = width - 2f * Inset - items * SignWidth - (beside ? TextRoom : 0f);
            return Mathf.Clamp(Mathf.Floor(room / (items + 1)), MinSlotSize, SlotSize);
        }

        /// <summary>One row: the equation beside the name and status (wide), or under them (narrow).</summary>
        private void LayoutRow(RowView row, float width, float height, bool beside)
        {
            row.Beside = beside;
            int items = row.Recipe.Items.Count;
            float slot = SlotFor(width, items, beside);
            // Under the name and status, a short recipe's slots may not grow into the text above them.
            if (!beside) slot = Mathf.Min(slot, height - (2f * RowPad + NameHeight + StatusHeight + 8f + HeldRoom));
            float slotBottom = beside ? (height - slot - HeldRoom) * 0.5f + HeldRoom : RowPad + HeldRoom;
            float x = Inset;
            for (int i = 0; i < row.Slots.Count; i++)
            {
                Place((RectTransform)row.Slots[i].transform, x, slotBottom, slot, slot);
                for (int h = 0; h < row.HeldLabels.Count; h++)
                    if (row.HeldSlot[h] == i) Place(row.HeldLabels[h].rectTransform, x, slotBottom - HeldRoom, slot, HeldRoom - 2f);
                x += slot;
                if (i < row.Signs.Count)
                {
                    Place(row.Signs[i], x, slotBottom, SignWidth, slot);
                    x += SignWidth;
                }
            }
            x += Inset;

            var name = row.NameText.rectTransform;
            var status = row.StatusText.rectTransform;
            if (beside)
            {
                // The name and status sit beside the equation.
                name.anchorMin = new Vector2(0f, 0.5f);
                name.anchorMax = new Vector2(1f, 0.92f);
                status.anchorMin = new Vector2(0f, 0.08f);
                status.anchorMax = new Vector2(1f, 0.48f);
                name.offsetMin = status.offsetMin = new Vector2(x, 0f);
                name.offsetMax = status.offsetMax = new Vector2(-Inset, 0f);
            }
            else
            {
                // On a narrow row the name and status read first, across the top, and the equation sits under them.
                name.anchorMin = status.anchorMin = new Vector2(0f, 1f);
                name.anchorMax = status.anchorMax = Vector2.one;
                name.offsetMin = new Vector2(Inset, -(RowPad + NameHeight));
                name.offsetMax = new Vector2(-Inset, -RowPad);
                status.offsetMin = new Vector2(Inset, -(RowPad + NameHeight + StatusHeight));
                status.offsetMax = new Vector2(-Inset, -(RowPad + NameHeight));
            }
            name.pivot = status.pivot = new Vector2(0.5f, 0.5f);
            row.NameText.alignment = beside ? TextAlignmentOptions.BottomLeft : TextAlignmentOptions.MidlineLeft;
            row.StatusText.alignment = beside ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
        }

        private static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(w, h);
            rect.anchoredPosition = new Vector2(x, y);
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

        // ---------------------------------------------------------------- one row

        private void FillRow(RowView row, EvolutionRecipe recipe, WeaponManager manager, SoulHunter.Core.Persistence.GameData save)
        {
            row.Recipe = recipe;
            row.State = EvolutionRecipes.StateOf(recipe, manager, save);
            bool known = row.State != EvolutionRecipes.RowState.Unknown;
            bool made = row.State == EvolutionRecipes.RowState.Made;
            row.HeldItems.Clear();

            var slotsParent = (RectTransform)row.SlotGroup.transform;
            for (int i = slotsParent.childCount - 1; i >= 0; i--) Destroy(slotsParent.GetChild(i).gameObject);
            slotsParent.DetachChildren();
            row.Slots.Clear();
            row.HeldLabels.Clear();
            row.HeldSlot.Clear();
            row.Signs.Clear();

            for (int i = 0; i < recipe.Items.Count; i++)
            {
                var item = recipe.Items[i];
                bool held = known && EvolutionRecipes.Holds(manager, item);
                if (held) row.HeldItems.Add(item);
                row.Slots.Add(Slot(row, slotsParent, "Item_" + i, known ? EvolutionRecipes.IconOf(item) : null, known, held || made, false));
                row.Signs.Add(Sign(slotsParent, i < recipe.Items.Count - 1 ? "+" : "="));
            }
            row.Slots.Add(Slot(row, slotsParent, "Result", known ? EvolutionRecipes.ResultIconOf(recipe) : null, known, made, true));

            row.NameText.text = UITheme.Bold(known ? recipe.Name : UnknownName);
            row.StatusText.text = StatusOf(row);

            // Wiki: an evolution made in the run has its whole equation highlighted; recipes no longer possible look faded.
            var cards = UiThemeAsset.Active.Cards;
            UITheme.Edge(row.Root.gameObject, cards.SelectedEdge, made ? cards.SelectedEdgeWidth : 0f);
            row.SlotGroup.alpha = row.State == EvolutionRecipes.RowState.Faded ? 0.45f : 1f;
        }

        /// <summary>The row's state in plain words (never by colour alone).</summary>
        public static string StatusOf(RowView row)
        {
            switch (row.State)
            {
                case EvolutionRecipes.RowState.Unknown: return UnknownStatus;
                case EvolutionRecipes.RowState.Made: return MadeStatus;
                case EvolutionRecipes.RowState.Faded: return FadedStatus;
            }
            if (row.HeldItems.Count == 0) return "Held: none yet";
            var names = new List<string>();
            foreach (var item in row.HeldItems) names.Add(UpgradeText.NameOf(item));
            return "Held: " + EvolutionRecipes.JoinNames(names);
        }

        private Image Slot(RowView row, RectTransform parent, string name, Sprite icon, bool known, bool held, bool result)
        {
            var frame = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            frame.SetParent(parent, false);
            frame.sizeDelta = new Vector2(SlotSize, SlotSize);
            var image = frame.GetComponent<Image>();
            image.raycastTarget = false;
            UITheme.ApplySurface(image, known ? ThemedElement.SurfaceRole.SlotFilled : ThemedElement.SurfaceRole.SlotEmpty);
            // Held items wear the theme's chosen-item frame (VS: a yellow-green square); the result the HUD's evolved frame.
            var cards = UiThemeAsset.Active.Cards;
            if (held) UITheme.Edge(frame.gameObject, cards.SelectedEdge, cards.SelectedEdgeWidth * 0.8f);
            else UITheme.Edge(frame.gameObject, result && known ? UITheme.Hud.SlotEvolved : UITheme.Hud.SlotBorder, result && known ? 3f : 1.5f);

            if (known)
            {
                var picture = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                picture.SetParent(frame, false);
                picture.anchorMin = new Vector2(0.1f, 0.1f);
                picture.anchorMax = new Vector2(0.9f, 0.9f);
                picture.offsetMin = picture.offsetMax = Vector2.zero;
                var pictureImage = picture.GetComponent<Image>();
                pictureImage.sprite = icon;
                pictureImage.preserveAspect = true;
                pictureImage.enabled = icon != null;
                pictureImage.raycastTarget = false;
            }
            else
            {
                var mark = Text(frame, "Unknown", Vector2.zero, Vector2.one, "?", 44f, TextAlignmentOptions.Center);
                UITheme.ApplyText(mark, ThemedElement.TextRole.Body);
            }

            if (held && !result)
            {
                var label = Text(parent, "Held_" + name, Vector2.zero, Vector2.zero, HeldLabel, 18f, TextAlignmentOptions.Center);
                UITheme.ApplyText(label, ThemedElement.TextRole.Accent);
                row.HeldLabels.Add(label);
                row.HeldSlot.Add(row.Slots.Count); // the slot being made now
            }
            ApplyTextFloor();
            return image;
        }

        private RectTransform Sign(RectTransform parent, string sign)
        {
            var text = Text(parent, "Sign", Vector2.zero, Vector2.zero, sign, 34f, TextAlignmentOptions.Center);
            return text.rectTransform;
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

        private static RectTransform Panel(RectTransform parent, string name, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().raycastTarget = false;
            return rect;
        }

        private TextMeshProUGUI Text(RectTransform parent, string name, Vector2 min, Vector2 max, string content, float size, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
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
            tmp.richText = true;
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
            var text = Text(rect, "Text", Vector2.zero, Vector2.one, label, 34f, TextAlignmentOptions.Center);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            _texts.RemoveAt(_texts.Count - 1); // a button's label is sized by the theme (ThemedElement), not here
            UITheme.ApplyButton(button, image, kind, icon); // after the label exists, so it styles it too
            return button;
        }

        private static void ShowUsable(Button button)
        {
            var element = button != null ? button.GetComponent<ThemedElement>() : null;
            if (element != null) element.ShowUsable(button.interactable);
        }

        private static void Select(Selectable target)
        {
            if (target != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(target.gameObject);
        }
    }
}
