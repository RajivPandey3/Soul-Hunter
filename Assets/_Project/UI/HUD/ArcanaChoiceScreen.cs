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
    /// (UITheme); nothing here picks a colour. Its own overlay canvas (Expand 1920x1080, like the chest and Revive
    /// screens) keeps the whole layout inside any window. The card faces have no art yet: card art is the owner's.
    /// </summary>
    public sealed class ArcanaChoiceScreen : MonoBehaviour
    {
        /// <summary>Above the merchant's shop (900), below Options (1000).</summary>
        public const int ScreenSortingOrder = 950;
        private const float PanelWidth = 1760f, PanelHeight = 960f;
        private const float GridLeft = -840f, GridTop = 330f, GridWidth = 1040f, GridHeight = 740f, CardGap = 16f;
        private const float InfoLeft = 240f, InfoWidth = 600f, InfoTop = 330f, InfoHeight = 740f;

        public static ArcanaChoiceScreen Current { get; private set; }

        public sealed class CardView
        {
            public ArcanaData Arcana;
            public Button Button;
            public TextMeshProUGUI Numeral, Name;
        }

        private readonly List<CardView> _cards = new List<CardView>();
        private Canvas _canvas;
        private RectTransform _root, _grid;
        private ArcanaDraft _draft;
        private Action<ArcanaData> _chosen;
        private ArcanaData _selected;
        private bool _paused, _pauseActionWasEnabled;

        public bool IsOpen => _root != null && _root.gameObject.activeSelf;
        public ArcanaDraft Draft => _draft;
        public ArcanaData Selected => _selected;
        public IReadOnlyList<CardView> Cards => _cards;
        public Canvas Canvas => _canvas;
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
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            gameObject.AddComponent<GraphicRaycaster>();

            _root = NewRect("Root", (RectTransform)transform);
            _root.anchorMin = Vector2.zero; _root.anchorMax = Vector2.one;
            _root.offsetMin = _root.offsetMax = Vector2.zero;
            UITheme.AddVeil(_root, "Veil");

            var panel = NewRect("Panel", _root);
            panel.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            UITheme.ApplyPanel(panel.gameObject.AddComponent<Image>());

            TitleText = Label(panel, "Title", new Vector2(-PanelWidth / 2f + 40f + 550f, 440f), new Vector2(1100f, 70f), UITheme.Layout.TitleTextMax,
                ThemedElement.TextRole.TitleArcana, TextAlignmentOptions.MidlineLeft);
            CountText = Label(panel, "Unlocked", new Vector2(PanelWidth / 2f - 40f - 260f, 440f), new Vector2(520f, 50f), 28f,
                ThemedElement.TextRole.Soft, TextAlignmentOptions.MidlineRight);

            _grid = NewRect("Cards", panel);
            _grid.pivot = new Vector2(0f, 1f);
            _grid.anchoredPosition = new Vector2(GridLeft, GridTop);
            _grid.sizeDelta = new Vector2(GridWidth, GridHeight);

            var info = NewRect("Info", panel);
            info.pivot = new Vector2(0f, 1f);
            info.anchoredPosition = new Vector2(InfoLeft, InfoTop);
            info.sizeDelta = new Vector2(InfoWidth, InfoHeight);
            UITheme.ApplySurface(info.gameObject.AddComponent<Image>(), ThemedElement.SurfaceRole.Row);

            InfoName = Label(info, "Name", Vector2.zero, Vector2.zero, 44f, ThemedElement.TextRole.Accent, TextAlignmentOptions.TopLeft);
            Place(InfoName.rectTransform, 32f, 28f, InfoWidth - 64f, 110f);
            InfoName.textWrappingMode = TextWrappingModes.Normal;
            InfoText = Label(info, "Effect", Vector2.zero, Vector2.zero, 30f, ThemedElement.TextRole.Body, TextAlignmentOptions.TopLeft);
            Place(InfoText.rectTransform, 32f, 150f, InfoWidth - 64f, 200f);
            InfoText.textWrappingMode = TextWrappingModes.Normal;
            InfoText.lineSpacing = 8f;
            WorksWithText = Label(info, "WorksWith", Vector2.zero, Vector2.zero, 26f, ThemedElement.TextRole.Soft, TextAlignmentOptions.TopLeft);
            Place(WorksWithText.rectTransform, 32f, 360f, InfoWidth - 64f, 110f);
            WorksWithText.textWrappingMode = TextWrappingModes.Normal;

            // The actions, the main one first, stacked with the theme's gap at the theme's standard size.
            float gap = Mathf.Max(8f, UITheme.Layout.ButtonGap);
            float h = UITheme.Layout.ButtonHeight;
            float y = 480f;
            TakeButton = ActionButton(info, "Take", "Take this card", UITheme.ButtonKind.Primary, UITheme.IconSlot.Confirm, y, PressTake);
            y += h + gap;
            RerollButton = ActionButton(info, "Reroll", "Reroll", UITheme.ButtonKind.Normal, UITheme.IconSlot.None, y, () => PressReroll());
            RandomButton = ActionButton(info, "Random", "Take a random card", UITheme.ButtonKind.Normal, UITheme.IconSlot.None, y, PressRandom);
            y += h + gap;
            SkipButton = ActionButton(info, "Skip", "Skip", UITheme.ButtonKind.Normal, UITheme.IconSlot.Back, y, PressSkip);

            _root.gameObject.SetActive(false);
        }

        private static RectTransform NewRect(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            return rect;
        }

        /// <summary>Places a child of the top-left-pivoted info panel: x and y measured from its top-left corner.</summary>
        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private Button ActionButton(RectTransform parent, string name, string text, UITheme.ButtonKind kind, UITheme.IconSlot icon, float y, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -y);
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
            TitleText.text = draft.DraftKind == ArcanaDraft.Kind.Chest ? "Arcana chest: choose a card"
                : draft.DraftKind == ArcanaDraft.Kind.Merchant ? "Draw a Fate: choose a card" : "Choose your Arcana";
            var manager = ArcanaManager.Instance;
            int unlocked = manager != null ? ArcanaUnlocks.UnlockedCount(manager.AllArcanas) : draft.Options.Count;
            int total = manager != null ? manager.AllArcanas.Count : draft.Options.Count;
            CountText.text = $"Unlocked {unlocked} / {total}";
            RerollButton.gameObject.SetActive(draft.DraftKind == ArcanaDraft.Kind.Chest);
            SkipButton.gameObject.SetActive(draft.CanSkip);
            RandomButton.gameObject.SetActive(draft.OffersRandom);
            RebuildCards();
            _root.gameObject.SetActive(true);
            if (!_paused) { SoulHunter.Core.Services.GameTime.Pause(this); _paused = true; }
            DisablePauseAction();
        }

        private void RebuildCards()
        {
            foreach (var card in _cards) if (card.Button != null) Destroy(card.Button.gameObject);
            _cards.Clear();
            var options = _draft.Options;
            int n = options.Count;
            int columns = n <= 6 ? Mathf.Max(1, n) : 6;
            int rows = Mathf.CeilToInt(n / (float)columns);
            float cardWidth = Mathf.Min(250f, (GridWidth - CardGap * (columns - 1)) / columns);
            float cardHeight = Mathf.Min(340f, (GridHeight - CardGap * (rows - 1)) / rows);
            float usedWidth = columns * cardWidth + (columns - 1) * CardGap;
            float startX = (GridWidth - usedWidth) / 2f;
            for (int i = 0; i < n; i++)
            {
                var arcana = options[i];
                var go = new GameObject("Card_" + (arcana != null ? arcana.CardNumber : "?"), typeof(RectTransform), typeof(Image), typeof(Button));
                var rect = (RectTransform)go.transform;
                rect.SetParent(_grid, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(startX + (i % columns) * (cardWidth + CardGap), -(i / columns) * (cardHeight + CardGap));
                rect.sizeDelta = new Vector2(cardWidth, cardHeight);
                var button = go.GetComponent<Button>();
                // The numeral is the card's one strong mark; the name sits under it.
                var numeral = Label(rect, "Numeral", Vector2.zero, Vector2.zero, Mathf.Min(96f, cardHeight * 0.42f), ThemedElement.TextRole.Card, TextAlignmentOptions.Center);
                numeral.rectTransform.anchorMin = new Vector2(0f, 0.42f); numeral.rectTransform.anchorMax = new Vector2(1f, 0.96f);
                numeral.rectTransform.offsetMin = new Vector2(8f, 0f); numeral.rectTransform.offsetMax = new Vector2(-8f, 0f);
                numeral.text = arcana != null ? arcana.CardNumber : "?";
                var name = Label(rect, "Name", Vector2.zero, Vector2.zero, Mathf.Min(30f, cardHeight * 0.16f), ThemedElement.TextRole.Card, TextAlignmentOptions.Center);
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
                InfoName.text = UITheme.Bold($"{arcana.CardNumber}  {arcana.CardName}");
                InfoText.text = arcana.Description;
                WorksWithText.text = WorksWith(arcana);
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
