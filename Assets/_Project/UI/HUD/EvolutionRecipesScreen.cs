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
    /// - Eight recipes per page (two columns of four), which fits 1920x1080 and any window the Expand scaler gives.
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

        private const float SlotSize = 76f, SignWidth = 34f, Inset = 16f;

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
        }

        public Button PreviousButton { get; private set; }
        public Button NextButton { get; private set; }
        public Button BackButton { get; private set; }
        public TextMeshProUGUI TitleText { get; private set; }
        public TextMeshProUGUI NoteText { get; private set; }
        public TextMeshProUGUI PageText { get; private set; }
        public RectTransform Window { get; private set; }
        public int Page { get; private set; }
        public int PageCount => Mathf.Max(1, (_recipes.Count + RowsPerPage - 1) / RowsPerPage);
        public bool IsOpen => gameObject.activeSelf;
        /// <summary>The rows on the open page.</summary>
        public readonly List<RowView> Rows = new List<RowView>();
        /// <summary>Every recipe in list order: discovered ones first, then the unknown ones.</summary>
        public IReadOnlyList<EvolutionRecipe> Recipes => _recipes;

        private readonly List<EvolutionRecipe> _recipes = new List<EvolutionRecipe>();
        private RectTransform _grid;
        private System.Action _onClosed;

        /// <summary>Builds the screen once, hidden, on its own root overlay canvas (a nested canvas ignores its scaler).</summary>
        public static EvolutionRecipesScreen Create()
        {
            var go = new GameObject(CanvasName, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = ScreenSortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            // Expand: the whole layout fits any window, short and wide included (the chest screen lost OPEN in a 21:9 window).
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
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
        }

        private void CollectRecipes()
        {
            _recipes.Clear();
            var manager = WeaponManager.Instance;
            var save = EvolutionRecipes.CurrentSave();
            var unknown = new List<EvolutionRecipe>();
            foreach (var recipe in EvolutionRecipes.All(manager))
            {
                bool known = (manager != null && manager.MadeThisRun(recipe.Id)) ||
                             SoulHunter.Core.Persistence.EvolutionDiscovery.IsDiscovered(save, recipe.Id);
                (known ? _recipes : unknown).Add(recipe);
            }
            _recipes.AddRange(unknown);
        }

        // ---------------------------------------------------------------- building

        private void Build()
        {
            var root = (RectTransform)transform;
            var veil = UITheme.AddVeil(root);
            if (veil != null) veil.raycastTarget = true; // the pause menu underneath takes no clicks while this is open

            Window = Panel(root, "Window", new Vector2(0.06f, 0.07f), new Vector2(0.94f, 0.93f));
            UITheme.ApplyPanel(Window.GetComponent<Image>(), 1f);

            TitleText = Text(Window, "Title", new Vector2(0.03f, 0.88f), new Vector2(0.7f, 0.98f), TitleLine, 54f, TextAlignmentOptions.MidlineLeft);
            UITheme.ApplyText(TitleText, ThemedElement.TextRole.Title);
            PageText = Text(Window, "Page", new Vector2(0.7f, 0.88f), new Vector2(0.97f, 0.98f), "", 28f, TextAlignmentOptions.MidlineRight);
            NoteText = Text(Window, "Note", new Vector2(0.03f, 0.82f), new Vector2(0.97f, 0.88f), NoteLine, 26f, TextAlignmentOptions.MidlineLeft);
            UITheme.ApplyText(NoteText, ThemedElement.TextRole.Soft);

            _grid = new GameObject("Grid", typeof(RectTransform)).GetComponent<RectTransform>();
            _grid.SetParent(Window, false);
            _grid.anchorMin = new Vector2(0.03f, 0.16f);
            _grid.anchorMax = new Vector2(0.97f, 0.81f);
            _grid.offsetMin = _grid.offsetMax = Vector2.zero;
            for (int i = 0; i < RowsPerPage; i++) Rows.Add(BuildRow(i));

            var bar = new GameObject("Buttons", typeof(RectTransform)).GetComponent<RectTransform>();
            bar.SetParent(Window, false);
            bar.anchorMin = new Vector2(0f, 0.025f);
            bar.anchorMax = new Vector2(1f, 0.135f);
            bar.offsetMin = bar.offsetMax = Vector2.zero;
            var row = new GameObject("Row", typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(bar, false);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 0.5f);
            row.sizeDelta = new Vector2(0f, UITheme.Layout.ButtonHeight);
            PreviousButton = MakeButton(row, "Previous_Button", "Previous", UITheme.ButtonKind.Normal, UITheme.IconSlot.None, () => ShowPage(Page - 1));
            BackButton = MakeButton(row, "Back_Button", "Back", UITheme.ButtonKind.Primary, UITheme.IconSlot.Back, Close);
            NextButton = MakeButton(row, "Next_Button", "Next", UITheme.ButtonKind.Normal, UITheme.IconSlot.None, () => ShowPage(Page + 1));
            row.gameObject.AddComponent<ThemedRow>();
        }

        private RowView BuildRow(int index)
        {
            int column = index / RowsPerColumn, line = index % RowsPerColumn;
            const float gapX = 0.01f, gapY = 0.015f;
            var view = new RowView();
            view.Root = Panel(_grid, "Recipe_" + index,
                new Vector2(column * 0.5f + (column == 1 ? gapX : 0f), 1f - (line + 1) / (float)RowsPerColumn + gapY),
                new Vector2(column * 0.5f + 0.5f - (column == 0 ? gapX : 0f), 1f - line / (float)RowsPerColumn - gapY));
            view.Background = view.Root.GetComponent<Image>();
            UITheme.ApplySurface(view.Background, ThemedElement.SurfaceRole.Row);

            var slots = new GameObject("Slots", typeof(RectTransform)).GetComponent<RectTransform>();
            slots.SetParent(view.Root, false);
            slots.anchorMin = Vector2.zero;
            slots.anchorMax = Vector2.one;
            slots.offsetMin = slots.offsetMax = Vector2.zero;
            view.SlotGroup = slots.gameObject.AddComponent<CanvasGroup>();
            view.SlotGroup.interactable = view.SlotGroup.blocksRaycasts = false;

            view.NameText = Text(view.Root, "Name", Vector2.zero, Vector2.one, "", 30f, TextAlignmentOptions.BottomLeft);
            view.StatusText = Text(view.Root, "Status", Vector2.zero, Vector2.one, "", 22f, TextAlignmentOptions.TopLeft);
            UITheme.ApplyText(view.StatusText, ThemedElement.TextRole.Soft);
            return view;
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

            float x = Inset;
            for (int i = 0; i < recipe.Items.Count; i++)
            {
                var item = recipe.Items[i];
                bool held = known && EvolutionRecipes.Holds(manager, item);
                if (held) row.HeldItems.Add(item);
                row.Slots.Add(Slot(row, slotsParent, "Item_" + i, x, known ? EvolutionRecipes.IconOf(item) : null, known, held || made, false));
                x += SlotSize;
                Sign(slotsParent, i < recipe.Items.Count - 1 ? "+" : "=", x);
                x += SignWidth;
            }
            row.Slots.Add(Slot(row, slotsParent, "Result", x, known ? EvolutionRecipes.ResultIconOf(recipe) : null, known, made, true));
            x += SlotSize + Inset;

            // The name and status sit beside the equation.
            foreach (var text in new[] { row.NameText, row.StatusText })
            {
                var rect = text.rectTransform;
                rect.offsetMin = new Vector2(x, 0f);
                rect.offsetMax = new Vector2(-Inset, 0f);
            }
            row.NameText.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            row.NameText.rectTransform.anchorMax = new Vector2(1f, 0.92f);
            row.StatusText.rectTransform.anchorMin = new Vector2(0f, 0.08f);
            row.StatusText.rectTransform.anchorMax = new Vector2(1f, 0.48f);
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

        private Image Slot(RowView row, RectTransform parent, string name, float x, Sprite icon, bool known, bool held, bool result)
        {
            var frame = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            frame.SetParent(parent, false);
            frame.anchorMin = frame.anchorMax = new Vector2(0f, 0.5f);
            frame.pivot = new Vector2(0f, 0.5f);
            frame.sizeDelta = new Vector2(SlotSize, SlotSize);
            frame.anchoredPosition = new Vector2(x, 10f); // room under it for the "Held" label
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
                var label = Text(parent, "Held_" + name, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), HeldLabel, 18f, TextAlignmentOptions.Center);
                var rect = label.rectTransform;
                rect.pivot = new Vector2(0f, 1f);
                rect.sizeDelta = new Vector2(SlotSize, 24f);
                rect.anchoredPosition = new Vector2(x, 10f - SlotSize * 0.5f - 2f);
                UITheme.ApplyText(label, ThemedElement.TextRole.Accent);
                row.HeldLabels.Add(label);
            }
            return image;
        }

        private void Sign(RectTransform parent, string sign, float x)
        {
            var text = Text(parent, "Sign", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), sign, 34f, TextAlignmentOptions.Center);
            var rect = text.rectTransform;
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(SignWidth, SlotSize);
            rect.anchoredPosition = new Vector2(x, 10f);
        }

        // ---------------------------------------------------------------- helpers

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

        private static TextMeshProUGUI Text(RectTransform parent, string name, Vector2 min, Vector2 max, string content, float size, TextAlignmentOptions align)
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
