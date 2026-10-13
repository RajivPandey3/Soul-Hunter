using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// The VS Arcana choice (release item P2-01, 2026-10-09), for the start of an Arcana-mode run, an Arcana chest and the
    /// Inverse merchant's Draw a Fate (ArcanaDraft). vampire.survivors.wiki/w/Arcanas (raw, fetched 2026-10-09): the
    /// player chooses "one of any unlocked Arcanas" and is "given the option to select a random one"; a chest offers "four
    /// or more randomly chosen Arcanas" and "The player may spend Rerolls to get new choices until all options have been
    /// displayed"; patch notes: "Arcana panel (on right)", "Get and Random buttons", "Unlocked count now displayed at the
    /// top", "Highlight weapons owned ... in arcana page", "Added option to skip arcana drafts".
    ///
    /// Design (ui-ux-pro-max and frontend-design, both loaded for this screen): one decision per screen, so one strong
    /// element: the cards' tarot numerals, set large in the theme's title face, are the memorable thing; everything else
    /// is quiet theme text. Left, the cards in a grid sized to fit (four large cards for a chest, the whole deck for the
    /// start pick); right, the chosen card's panel with its name, what it does, which held weapons it works with (named
    /// and marked "held", never colour alone) and the actions, the main one first ("Take this card" in the theme's Primary
    /// kind). Every control is keyboard and controller focusable with the theme's focus ring; choosing a card by focus or
    /// click shows it on the right. Plain sentence-case words. Every colour, font and size comes from the active UI theme
    /// (UITheme); nothing here picks a colour. The card faces have no art yet: card art is the owner's.
    ///
    /// Responsive (owner, 2026-10-10: the same screen in landscape and portrait): its own overlay canvas takes the game's
    /// one scale (ResponsiveCanvas); the panel keeps to the safe area; the cards take as many columns as the grid's width
    /// allows and scroll when they are more than the room; the chosen card's panel sits beside the cards on a wide screen
    /// and under them on a tall one (AdaptiveSplit); the actions are one ThemedRow pinned to the panel's bottom edge.
    /// </summary>
    public sealed class ArcanaChoiceScreen : MonoBehaviour
    {
        /// <summary>Above the merchant's shop (900), below Options (1000).</summary>
        public const int ScreenSortingOrder = 950;
        // Responsive (owner, 2026-10-10: landscape and portrait from the same screen). Sizes in canvas units; the panel takes
        // the safe area up to the approved 1760x960, the cards' columns follow the grid's width.
        private const float MaxPanelWidth = 1760f, MaxPanelHeight = 960f, CardGap = 16f;
        /// <summary>The narrowest card, in canvas units (never under the touch target).</summary>
        public const float MinCardWidth = 140f;
        private const float MaxCardWidth = 250f, MaxCardHeight = 340f, CardAspect = 1.36f, MinCardAspect = 0.75f;
        /// <summary>Under this panel width the title and the unlocked count stack (a portrait phone).</summary>
        private const float StackHeaderBelow = 900f;
        /// <summary>A safe area shorter than this is "short" (a landscape phone): tighter margins.</summary>
        private const float ShortBelow = 700f;

        public static ArcanaChoiceScreen Current { get; private set; }

        public sealed class CardView
        {
            public ArcanaData Arcana;
            public Button Button;
            public TextMeshProUGUI Numeral, Name;
        }

        private readonly List<CardView> _cards = new List<CardView>();
        private Canvas _canvas;
        private RectTransform _root, _grid, _panel, _body, _gridArea, _info, _actions;
        private ScrollRect _scroll;
        private GridLayoutGroup _gridLayout;
        private ThemedRow _actionRow;
        private Vector2 _lastRoom = new Vector2(-1f, -1f);
        private float _lastActionsHeight = -1f;
        private ArcanaDraft _draft;
        private Action<ArcanaData> _chosen;
        private ArcanaData _selected;
        private bool _paused, _pauseActionWasEnabled;

        public bool IsOpen => _root != null && _root.gameObject.activeSelf;
        public ArcanaDraft Draft => _draft;
        public ArcanaData Selected => _selected;
        public IReadOnlyList<CardView> Cards => _cards;
        public Canvas Canvas => _canvas;
        /// <summary>The cards' scrolling window: a card is on screen when it lies inside it.</summary>
        public RectTransform CardsViewport => _scroll != null ? _scroll.viewport : null;
        public ScrollRect CardsScroll => _scroll;
        /// <summary>The chosen card's panel (beside the cards when wide, under them when tall).</summary>
        public RectTransform InfoPanel => _info;
        /// <summary>Take / Reroll / Random / Skip, one ThemedRow pinned to the bottom of the panel.</summary>
        public RectTransform ActionRow => _actions;
        /// <summary>True while the cards and the panel are stacked (a tall screen).</summary>
        public bool IsStacked => _body != null && _body.GetComponent<AdaptiveSplit>() is AdaptiveSplit split && split.IsTall;
        public TextMeshProUGUI TitleText { get; private set; }
        public TextMeshProUGUI CountText { get; private set; }
        public TextMeshProUGUI InfoName { get; private set; }
        public TextMeshProUGUI InfoText { get; private set; }
        public TextMeshProUGUI WorksWithText { get; private set; }
        public Button TakeButton { get; private set; }
        public Button RerollButton { get; private set; }
        public Button RandomButton { get; private set; }
        public Button SkipButton { get; private set; }

        /// <summary>Opens the draft (pausing the game); <paramref name="chosen"/> gets the card, or null when skipped.</summary>
        public static bool Show(ArcanaDraft draft, Action<ArcanaData> chosen)
        {
            if (draft == null || draft.Options.Count == 0) return false;
            if (Current == null)
            {
                var go = new GameObject("ArcanaChoiceScreen", typeof(RectTransform));
                // A root object in the run's scene (as the Revive screen), so it never sits in a loading scene.
                var manager = ArcanaManager.Instance;
                if (manager != null && manager.gameObject.scene.IsValid() && manager.gameObject.scene.isLoaded)
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, manager.gameObject.scene);
                Current = go.AddComponent<ArcanaChoiceScreen>();
                Current.Build();
            }
            else if (Current.IsOpen) return false; // one choice at a time; the caller falls back
            Current.Open(draft, chosen);
            return true;
        }

        // ---------- build ----------

        private void Build()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = ScreenSortingOrder;
            SoulHunter.Gameplay.UI.ResponsiveCanvas.Apply(_canvas); // the game's one UI scale (UiScreen), landscape and portrait
            gameObject.AddComponent<GraphicRaycaster>();

            _root = NewRect("Root", (RectTransform)transform);
            Stretch(_root);
            UITheme.AddVeil(_root, "Veil"); // the veil stays full screen; the content keeps to the safe area

            var safe = NewRect("Safe", _root);
            SafeAreaFitter.Fit(safe);
            _panel = NewRect("Panel", safe);
            UITheme.ApplyPanel(_panel.gameObject.AddComponent<Image>());

            TitleText = Label(_panel, "Title", Vector2.zero, Vector2.zero, UITheme.Layout.TitleTextMax,
                ThemedElement.TextRole.TitleArcana, TextAlignmentOptions.MidlineLeft);
            CountText = Label(_panel, "Unlocked", Vector2.zero, Vector2.zero, 28f,
                ThemedElement.TextRole.Soft, TextAlignmentOptions.MidlineRight);

            // The body: the cards and the chosen card's panel, side by side when wide, stacked when tall (AdaptiveSplit).
            _body = NewRect("Body", _panel);
            _gridArea = NewRect("CardsArea", _body);
            _info = NewRect("Info", _body);
            UITheme.ApplySurface(_info.gameObject.AddComponent<Image>(), ThemedElement.SurfaceRole.Row);
            AdaptiveSplit.Between(_body, _gridArea, _info, 0.62f, 0.6f);

            // The cards scroll when they are more than the room (the start pick's whole deck on a phone).
            // Keyboard and controller: the focused card scrolls into view (KeepSelectedInView).
            _scroll = ResponsiveScroll.Create(_gridArea, "CardsViewport", "Cards", out _grid, ResponsiveScroll.Bar.Mark,
                fitContentHeight: true, keepSelectedInView: true);
            _gridLayout = _grid.gameObject.AddComponent<GridLayoutGroup>();
            _gridLayout.spacing = new Vector2(CardGap, CardGap);
            _gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _gridLayout.childAlignment = TextAnchor.UpperCenter;

            InfoName = Label(_info, "Name", Vector2.zero, Vector2.zero, 44f, ThemedElement.TextRole.Accent, TextAlignmentOptions.TopLeft);
            InfoName.textWrappingMode = TextWrappingModes.Normal;
            InfoText = Label(_info, "Effect", Vector2.zero, Vector2.zero, 30f, ThemedElement.TextRole.Body, TextAlignmentOptions.TopLeft);
            InfoText.textWrappingMode = TextWrappingModes.Normal;
            InfoText.lineSpacing = 8f;
            WorksWithText = Label(_info, "WorksWith", Vector2.zero, Vector2.zero, 26f, ThemedElement.TextRole.Soft, TextAlignmentOptions.TopLeft);
            WorksWithText.textWrappingMode = TextWrappingModes.Normal;

            // The actions, the main one first, in one ThemedRow pinned to the bottom of the panel: one line when it fits,
            // more centred lines on a narrow screen, every button at the theme's standard (touch) size.
            _actions = NewRect("Actions", _panel);
            _actions.anchorMin = _actions.anchorMax = _actions.pivot = new Vector2(0.5f, 0f);
            TakeButton = ActionButton(_actions, "Take", "Take this card", UITheme.ButtonKind.Primary, UITheme.IconSlot.Confirm, PressTake);
            RerollButton = ActionButton(_actions, "Reroll", "Reroll", UITheme.ButtonKind.Normal, UITheme.IconSlot.None, () => PressReroll());
            RandomButton = ActionButton(_actions, "Random", "Take a random card", UITheme.ButtonKind.Normal, UITheme.IconSlot.None, PressRandom);
            SkipButton = ActionButton(_actions, "Skip", "Skip", UITheme.ButtonKind.Normal, UITheme.IconSlot.Back, PressSkip);
            _actionRow = _actions.gameObject.AddComponent<ThemedRow>();

            _root.gameObject.SetActive(false);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        // ---------- layout (one code path; wide / tall and roomy / short only pick the pieces' modes) ----------

        private void OnEnable() => UiScreen.Changed += Relayout;

        private void OnDisable() => UiScreen.Changed -= Relayout;

        private void LateUpdate()
        {
            if (!IsOpen) return;
            var safe = (RectTransform)_panel.parent;
            float actions = _actions.rect.height * _actions.localScale.y;
            if (safe.rect.size != _lastRoom || !Mathf.Approximately(actions, _lastActionsHeight)) Relayout();
        }

        /// <summary>Lays the open screen out for the current canvas (a rotation or a window change), keeping its state.</summary>
        public void Relayout()
        {
            if (_panel == null || !IsOpen) return;
            var safe = (RectTransform)_panel.parent;
            Vector2 room = safe.rect.size;
            if (room.x <= 0f || room.y <= 0f) return;
            bool roomy = room.y >= ShortBelow;
            float margin = roomy ? 24f : 12f, pad = roomy ? 28f : 16f;
            float gap = Mathf.Max(8f, UITheme.Layout.ButtonGap);

            float panelWidth = Mathf.Min(room.x - 2f * margin, MaxPanelWidth);
            // The approved 960 tall on a wide screen; a tall screen gives the stacked cards and panel its whole height.
            float panelHeight = Mathf.Min(room.y - 2f * margin, room.x >= room.y ? MaxPanelHeight : float.MaxValue);
            _panel.anchorMin = _panel.anchorMax = _panel.pivot = new Vector2(0.5f, 0.5f);
            _panel.anchoredPosition = Vector2.zero;
            _panel.sizeDelta = new Vector2(panelWidth, panelHeight);

            // Header: the title left and the unlocked count right; stacked on a narrow panel.
            float inner = panelWidth - 2f * pad;
            bool stack = panelWidth < StackHeaderBelow;
            float titleHeight = roomy ? 64f : 52f, countHeight = 36f;
            float headerHeight;
            if (stack)
            {
                TopLeft(TitleText.rectTransform, pad, pad, inner, titleHeight);
                TopLeft(CountText.rectTransform, pad, pad + titleHeight + 4f, inner, countHeight);
                CountText.alignment = TextAlignmentOptions.MidlineLeft;
                headerHeight = titleHeight + 4f + countHeight;
            }
            else
            {
                TopLeft(TitleText.rectTransform, pad, pad, inner * 0.64f, titleHeight);
                TopLeft(CountText.rectTransform, pad + inner * 0.66f, pad, inner * 0.34f, titleHeight);
                CountText.alignment = TextAlignmentOptions.MidlineRight;
                headerHeight = titleHeight;
            }

            // The actions pinned to the bottom edge; the row reports its wrapped height.
            _actions.anchoredPosition = new Vector2(0f, pad);
            _actionRow.Layout();
            float actionsHeight = _actions.rect.height * _actions.localScale.y;
            _lastActionsHeight = actionsHeight;

            // The body between them.
            _body.anchorMin = Vector2.zero; _body.anchorMax = Vector2.one;
            _body.pivot = new Vector2(0.5f, 0.5f);
            _body.offsetMin = new Vector2(pad, pad + actionsHeight + gap);
            _body.offsetMax = new Vector2(-pad, -(pad + headerHeight + gap));
            _body.GetComponent<AdaptiveSplit>().Layout(force: true);

            // The chosen card's text, in the info panel's own room.
            float infoPad = roomy ? 28f : 16f;
            Region(InfoName.rectTransform, 0.76f, 1f, infoPad);
            Region(InfoText.rectTransform, 0.30f, 0.76f, infoPad);
            Region(WorksWithText.rectTransform, 0f, 0.30f, infoPad);
            foreach (var text in new[] { TitleText, CountText, InfoName, InfoText, WorksWithText }) UiTextFloor.Apply(text);

            LayoutCards();
            _lastRoom = room;
            Canvas.ForceUpdateCanvases();
        }

        /// <summary>
        /// The cards' columns from the grid's width (never under <see cref="MinCardWidth"/> or the touch target); a card is
        /// tarot-shaped, shrinking (down to 3:4) so a whole deck fits a roomy screen; more than the room scrolls.
        /// </summary>
        private void LayoutCards()
        {
            if (_scroll == null) return;
            Vector2 view = _scroll.viewport.rect.size;
            int n = _cards.Count;
            if (n == 0 || view.x <= 0f || view.y <= 0f) return;
            float minWidth = Mathf.Max(MinCardWidth, UiScreen.MinTouchUnits);
            int columns = UiScreen.ColumnsFor(view.x, minWidth, CardGap, Mathf.Clamp(n, 1, 6));
            int rows = Mathf.CeilToInt(n / (float)columns);
            float width = Mathf.Floor(Mathf.Min(MaxCardWidth, (view.x - CardGap * (columns - 1)) / columns));
            float fit = (view.y - CardGap * (rows - 1)) / rows;
            float height = Mathf.Floor(Mathf.Max(Mathf.Min(width * CardAspect, fit, MaxCardHeight), width * MinCardAspect, UiScreen.MinTouchUnits));
            _gridLayout.constraintCount = columns;
            _gridLayout.cellSize = new Vector2(width, height);
            foreach (var card in _cards)
            {
                card.Numeral.fontSizeMax = Mathf.Min(96f, height * 0.42f);
                card.Name.fontSizeMax = Mathf.Min(30f, height * 0.16f);
                UiTextFloor.Apply(card.Numeral);
                UiTextFloor.Apply(card.Name);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(_grid);
        }

        /// <summary>A rect measured from its parent's top-left corner, in canvas units.</summary>
        private static void TopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        /// <summary>A text band of the info panel, from <paramref name="bottom"/> to <paramref name="top"/> of its height.</summary>
        private static void Region(RectTransform rect, float bottom, float top, float pad)
        {
            rect.anchorMin = new Vector2(0f, bottom);
            rect.anchorMax = new Vector2(1f, top);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(pad, bottom <= 0f ? pad : 4f);
            rect.offsetMax = new Vector2(-pad, top >= 1f ? -pad : -4f);
        }

        private static RectTransform NewRect(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            return rect;
        }

        private Button ActionButton(RectTransform parent, string name, string text, UITheme.ButtonKind kind, UITheme.IconSlot icon, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f); // ThemedRow places it
            rect.sizeDelta = new Vector2(UITheme.Layout.ButtonWidth, UITheme.Layout.ButtonHeight);
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(onClick);
            var label = Label(rect, "Label", Vector2.zero, rect.sizeDelta, UITheme.Layout.ButtonTextMax, ThemedElement.TextRole.Body, TextAlignmentOptions.Center);
            label.text = text;
            UITheme.ApplyButton(button, go.GetComponent<Image>(), kind, icon);
            return button;
        }

        private static TextMeshProUGUI Label(RectTransform parent, string name, Vector2 position, Vector2 size, float fontSize,
                                             ThemedElement.TextRole role, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var label = go.AddComponent<TextMeshProUGUI>();
            // A size before auto-size, so TextMeshPro's own defaults never replace the limits (character select, 2026-09-30).
            label.fontSize = fontSize;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = fontSize;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            UITheme.ApplyText(label, role);
            ThemedElement.Outline(label, UiThemeAsset.Active);
            return label;
        }

        // ---------- flow ----------

        private void Open(ArcanaDraft draft, Action<ArcanaData> chosen)
        {
            _draft = draft;
            _chosen = chosen;
            bool weapons = draft.DraftKind == ArcanaDraft.Kind.Weapons;
            TitleText.text = weapons ? "Armoury Cache: choose a weapon"
                : draft.DraftKind == ArcanaDraft.Kind.Chest ? "Arcana chest: choose a card"
                : draft.DraftKind == ArcanaDraft.Kind.Merchant ? "Draw a Fate: choose a card" : "Choose your Arcana";
            var manager = ArcanaManager.Instance;
            int unlocked = manager != null ? ArcanaUnlocks.UnlockedCount(manager.AllArcanas) : draft.Options.Count;
            int total = manager != null ? manager.AllArcanas.Count : draft.Options.Count;
            CountText.text = weapons ? $"{draft.Options.Count} to choose from" : $"Unlocked {unlocked} / {total}";
            RerollButton.gameObject.SetActive(draft.DraftKind == ArcanaDraft.Kind.Chest);
            SkipButton.gameObject.SetActive(draft.CanSkip);
            RandomButton.gameObject.SetActive(draft.OffersRandom);
            _root.gameObject.SetActive(true);
            RebuildCards(); // lays the screen out for this canvas (Relayout)
            if (!_paused) { SoulHunter.Core.Services.GameTime.Pause(this); _paused = true; }
            DisablePauseAction();
        }

        private void RebuildCards()
        {
            // Out of the grid at once (Destroy waits for the frame's end), so the grid lays out only the new cards.
            foreach (var card in _cards) if (card.Button != null) { card.Button.transform.SetParent(null, false); Destroy(card.Button.gameObject); }
            _cards.Clear();
            var options = _draft.Options;
            int n = options.Count;
            const float startHeight = 300f; // until Relayout sizes the cells from the grid's room
            for (int i = 0; i < n; i++)
            {
                var arcana = options[i];
                var go = new GameObject("Card_" + (arcana != null ? arcana.CardNumber : "?"), typeof(RectTransform), typeof(Image), typeof(Button));
                var rect = (RectTransform)go.transform;
                rect.SetParent(_grid, false); // the grid places and sizes it (LayoutCards)
                var button = go.GetComponent<Button>();
                // The numeral is the card's one strong mark; the name sits under it.
                var numeral = Label(rect, "Numeral", Vector2.zero, Vector2.zero, Mathf.Min(96f, startHeight * 0.42f), ThemedElement.TextRole.Card, TextAlignmentOptions.Center);
                numeral.rectTransform.anchorMin = new Vector2(0f, 0.42f); numeral.rectTransform.anchorMax = new Vector2(1f, 0.96f);
                numeral.rectTransform.offsetMin = new Vector2(8f, 0f); numeral.rectTransform.offsetMax = new Vector2(-8f, 0f);
                numeral.text = arcana != null ? arcana.CardNumber : "?";
                // A weapon card (the Armoury Cache) has no numeral: its icon stands in the numeral's place.
                if (arcana != null && string.IsNullOrEmpty(arcana.CardNumber) && arcana.CardIcon != null)
                {
                    var iconRect = NewRect("Icon", rect);
                    iconRect.anchorMin = new Vector2(0.18f, 0.46f); iconRect.anchorMax = new Vector2(0.82f, 0.94f);
                    iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;
                    var icon = iconRect.gameObject.AddComponent<Image>();
                    icon.sprite = arcana.CardIcon;
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;
                }
                var name = Label(rect, "Name", Vector2.zero, Vector2.zero, Mathf.Min(30f, startHeight * 0.16f), ThemedElement.TextRole.Card, TextAlignmentOptions.Center);
                name.rectTransform.anchorMin = new Vector2(0f, 0.04f); name.rectTransform.anchorMax = new Vector2(1f, 0.42f);
                name.rectTransform.offsetMin = new Vector2(10f, 0f); name.rectTransform.offsetMax = new Vector2(-10f, 0f);
                name.textWrappingMode = TextWrappingModes.Normal;
                name.text = arcana != null ? UITheme.Bold(arcana.CardName) : "";
                UITheme.ApplyButton(button, go.GetComponent<Image>(), UITheme.ButtonKind.Card, UITheme.IconSlot.None, false, ThemedElement.SizeRole.Free);
                // Applied once more after the theme: card labels keep the card text role (the theme styles labels of
                // buttons with StyleLabels; here they are off, so the roles above stand).
                var captured = arcana;
                button.onClick.AddListener(() => Select(captured));
                go.AddComponent<ArcanaCardFocus>().Selected = () => Select(captured);
                _cards.Add(new CardView { Arcana = arcana, Button = button, Numeral = numeral, Name = name });
            }
            Select(n > 0 ? options[0] : null);
            if (_scroll != null) { _scroll.velocity = Vector2.zero; _scroll.verticalNormalizedPosition = 1f; _grid.anchoredPosition = Vector2.zero; }
            Relayout();
            var events = EventSystem.current;
            if (events != null && _cards.Count > 0) events.SetSelectedGameObject(_cards[0].Button.gameObject);
        }

        /// <summary>Shows <paramref name="arcana"/> in the panel and marks its card (frame, not colour alone).</summary>
        public void Select(ArcanaData arcana)
        {
            if (_draft == null || (arcana != null && !_draft.CanChoose(arcana))) return;
            _selected = arcana;
            var theme = UiThemeAsset.Active;
            foreach (var card in _cards)
            {
                bool chosen = card.Arcana == arcana;
                UITheme.Edge(card.Button.gameObject, chosen ? theme.Cards.SelectedEdge : UITheme.PanelEdge, chosen ? theme.Cards.SelectedEdgeWidth : 1.5f);
            }
            if (arcana == null)
            {
                InfoName.text = InfoText.text = WorksWithText.text = "";
            }
            else
            {
                InfoName.text = UITheme.Bold(string.IsNullOrEmpty(arcana.CardNumber) ? arcana.CardName : $"{arcana.CardNumber}  {arcana.CardName}");
                InfoText.text = arcana.Description;
                WorksWithText.text = _draft.DraftKind == ArcanaDraft.Kind.Weapons ? "" : WorksWith(arcana);
            }
            RefreshButtons();
        }

        /// <summary>
        /// "Works with": the Soul Hunter weapons the card's page lists, the held ones marked "(held)" in the theme's bold
        /// (wiki Arcanas, v1.14.111: "Highlight weapons owned ... in arcana page").
        /// </summary>
        public static string WorksWith(ArcanaData arcana)
        {
            if (arcana == null) return "";
            var weapons = ArcanaCatalog.Of(arcana.Type).Weapons;
            if (weapons.Length == 0) return "";
            var held = new HashSet<ArcanaWeapon>();
            var manager = WeaponManager.Instance;
            if (manager != null)
                foreach (var pair in manager.GetEquippedWeapons())
                    if (pair.Value != null) held.Add(ArcanaManager.Identify(pair.Value));
            var parts = new List<string>();
            foreach (var weapon in weapons)
            {
                string name = ArcanaCatalog.WeaponName(weapon);
                parts.Add(held.Contains(weapon) ? UITheme.Bold(name + " (held)") : name);
            }
            return "Works with: " + string.Join(", ", parts);
        }

        private void RefreshButtons()
        {
            if (_draft == null) return;
            SetUsable(TakeButton, _selected != null);
            if (RerollButton.gameObject.activeSelf)
            {
                int charges = RerollCharges();
                bool free = _draft.FreeRerollsLeft > 0;
                bool can = _draft.HasUnshown && (free || charges > 0);
                SetText(RerollButton, !_draft.HasUnshown ? "No cards left" : free ? "Reroll (free)" : $"Reroll ({charges} left)");
                SetUsable(RerollButton, can);
            }
        }

        private static int RerollCharges()
        {
            var levelUp = FindFirstObjectByType<LevelUpManager>();
            return levelUp != null ? levelUp.RerollsLeft : 0;
        }

        private static void SetText(Button button, string text)
        {
            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null) label.text = text;
        }

        private static void SetUsable(Button button, bool usable)
        {
            button.interactable = usable;
            ThemedElement.For(button.gameObject).ShowUsable(usable);
        }

        public void PressTake()
        {
            if (!IsOpen || _selected == null) return;
            Finish(_selected);
        }

        public bool PressReroll()
        {
            if (!IsOpen || _draft == null) return false;
            if (!_draft.Reroll(ArcanaManager.SpendRerollCharge)) { RefreshButtons(); return false; }
            RebuildCards();
            return true;
        }

        public void PressRandom()
        {
            if (!IsOpen || _draft == null || !_draft.OffersRandom) return;
            Finish(_draft.RandomOption());
        }

        public void PressSkip()
        {
            if (!IsOpen || _draft == null || !_draft.CanSkip) return;
            Finish(null);
        }

        private void Finish(ArcanaData card)
        {
            var chosen = _chosen;
            _chosen = null;
            Close();
            chosen?.Invoke(card);
        }

        private void Close()
        {
            if (_root != null) _root.gameObject.SetActive(false);
            _draft = null;
            _selected = null;
            RestorePauseAction();
            if (_paused) { SoulHunter.Core.Services.GameTime.Resume(this); _paused = false; }
            var events = EventSystem.current;
            if (events != null) events.SetSelectedGameObject(null);
        }

        private void OnDestroy()
        {
            RestorePauseAction();
            if (_paused) SoulHunter.Core.Services.GameTime.Resume(this);
            _paused = false;
            if (Current == this) Current = null;
        }

        // The same switch as the chest and Revive screens: Esc must not open the pause menu over the choice.
        private void DisablePauseAction()
        {
            var pause = PauseAction();
            if (pause != null && pause.enabled) { _pauseActionWasEnabled = true; pause.Disable(); }
        }

        private void RestorePauseAction()
        {
            if (!_pauseActionWasEnabled) return;
            _pauseActionWasEnabled = false;
            PauseAction()?.Enable();
        }

        private static UnityEngine.InputSystem.InputAction PauseAction()
        {
            if (SoulHunter.Core.Services.GameServices.Instance == null) return null;
            SoulHunter.Core.Services.GameServices.Instance.TryGet(out SoulHunter.Core.Input.InputService input);
            return input?.Actions?.FindAction(SoulHunter.Core.Input.InputService.PauseAction);
        }
    }

    /// <summary>Keyboard or controller focus on an Arcana card shows it, as a click does (ArcanaChoiceScreen).</summary>
    public sealed class ArcanaCardFocus : MonoBehaviour, ISelectHandler
    {
        public Action Selected;
        public void OnSelect(BaseEventData eventData) => Selected?.Invoke();
    }
}
