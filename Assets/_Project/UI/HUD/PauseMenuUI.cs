using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SoulHunter.Gameplay.Core;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// VS-style pause menu: Resume, Options and Quit (to the main menu), usable with keyboard or
    /// controller (Resume is focused when the menu opens). Replaces the old panel's dead Restart button and
    /// its volume slider (volume now lives in Options). The HUD attaches it to GameSystemManager's panel.
    /// </summary>
    public sealed class PauseMenuUI : MonoBehaviour
    {
        private GameSystemManager _system;
        private Button _resume;
        private Button _options;
        private Button _quit;
        private Button _evolutions;
        private Button _blindFate;
        private TextMeshProUGUI _blindFateLabel;
        private EvolutionRecipesScreen _recipes;

        public Button ResumeButton => _resume;
        /// <summary>Opens the evolution recipes (release item P3-06; EvolutionRecipesScreen).</summary>
        public Button EvolutionsButton => _evolutions;
        /// <summary>The evolution recipes screen, once it has been opened (null before).</summary>
        public EvolutionRecipesScreen RecipesScreen => _recipes;
        public Button OptionsButton => _options;
        /// <summary>Switches Blind Fate (VS Random LevelUp) on or off during the run (release item P3-05).</summary>
        public Button BlindFateButton => _blindFate;
        public string BlindFateText => _blindFateLabel != null ? _blindFateLabel.text : null;
        public Button QuitButton => _quit;

        public static PauseMenuUI Attach(GameSystemManager system)
        {
            if (system == null || system.PauseMenuPanel == null) return null;
            var ui = system.PauseMenuPanel.GetComponent<PauseMenuUI>();
            if (ui != null) return ui;
            ui = system.PauseMenuPanel.AddComponent<PauseMenuUI>();
            ui._system = system;
            ui.Build();
            system.OnPauseChanged += ui.HandlePauseChanged;
            return ui;
        }

        private void OnDestroy()
        {
            if (_system != null) _system.OnPauseChanged -= HandlePauseChanged;
            if (_recipes != null) Destroy(_recipes.gameObject);
        }

        /// <summary>Opens the evolution recipes over the pause menu; Back returns focus to the Evolutions button.</summary>
        /// <summary>Options over the pause screen, which steps back as it does for the Evolutions list (phone run, 2026-10-10: its title and buttons showed around the Options window).</summary>
        public void OpenOptions()
        {
            var group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            OptionsMenuUI.Open(() =>
            {
                if (group != null) { group.alpha = 1f; group.blocksRaycasts = true; }
                Select(_options);
            });
        }

        public void OpenEvolutions()
        {
            if (_recipes == null) _recipes = EvolutionRecipesScreen.Create();
            // The pause screen (buttons and the stats column) steps back while the list is open, so its stats do not show
            // through the list's veil at the screen edge (build screenshot, 2026-10-09); it comes back when the list closes.
            var group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            _recipes.Open(() =>
            {
                if (group != null) { group.alpha = 1f; group.blocksRaycasts = true; }
                Select(_evolutions);
            });
        }

        private void Build()
        {
            if (_system.VolumeSlider != null) _system.VolumeSlider.gameObject.SetActive(false);
            // Owner (2026-10-04): dark, full contrast. The game behind sits under a dark veil; the old panel image goes.
            var oldBackground = GetComponent<Image>();
            if (oldBackground != null) oldBackground.color = new Color(0f, 0f, 0f, 0f);
            UITheme.AddVeil(transform);
            foreach (var child in GetComponentsInChildren<Button>(true))
                if (child.name.Contains("Restart")) child.gameObject.SetActive(false);

            // Responsive (owner, 2026-10-10: landscape and portrait from the same screen). The content sits in the safe area
            // and splits into the menu and the stats: side by side on a wide screen, stacked on a tall one. The buttons
            // are one column at the theme's size; the stats take the columns their area allows.
            var content = NewArea("PauseContent", transform);
            SafeAreaFitter.Fit(content);
            var menuArea = NewArea("MenuArea", content);
            var statsArea = NewArea("StatsArea", content);
            AdaptiveSplit.Between(content, menuArea, statsArea, 0.64f, 0.56f);
            _buttonColumn = NewArea("Buttons", menuArea);
            _buttonColumn.anchorMax = new Vector2(1f, 0.84f);
            var column = _buttonColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            column.childAlignment = TextAnchor.MiddleCenter;
            column.spacing = UITheme.Layout.ButtonGap;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = column.childForceExpandHeight = false;

            _resume = CreateButton("Resume_Button", Vector2.zero, Vector2.zero, "Resume", () => _system.Resume());
            _options = CreateButton("Options_Button", Vector2.zero, Vector2.zero, "Options",
                OpenOptions);
            // VS Grimoire (wiki Grim_Grimoire): with the Grimoire and the map, an "Open Grimoire" button "just to the left of
            // the "RESUME" button". Soul Hunter has no relics yet, so it is open from the start (EvolutionRecipesScreen).
            _evolutions = CreateButton("Evolutions_Button", Vector2.zero, Vector2.zero, "Evolutions", OpenEvolutions);
            // VS Random LevelUp (wiki Level_up, 1 April 2026 patch note): "'Random LevelUp' can now be toggled during a run if it
            // is already unlocked on the Stage menu". The wiki does not say where; Soul Hunter puts it in the pause menu, under
            // Evolutions, as a button that says its state in words (like FILTER: ON), so the state never rests on colour alone.
            // Owner (2026-10-09): Blind Fate is unlocked from the start, so the button is always there.
            _blindFate = CreateButton("BlindFate_Button", Vector2.zero, Vector2.zero, "", ToggleBlindFate);
            _blindFateLabel = _blindFate.GetComponentInChildren<TextMeshProUGUI>();
            RefreshBlindFate();
            // Quit last, the leaving action at the bottom of the column.
            _quit = CreateButton("Quit_Button", Vector2.zero, Vector2.zero, "Quit", () => _system.QuitToMainMenu());
            // VS: the player's stats are shown while paused (wiki Player_stats): one column beside the menu on a wide
            // screen, two under it on a tall one (StatsPanel picks).
            StatsPanel.Attach(statsArea, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f), 2, 30f);

            // The old "GAME PAUSED" title sat in the middle, behind the buttons: move it above them.
            foreach (var title in GetComponentsInChildren<TMP_Text>(true))
                if (title.text.ToUpperInvariant().Contains("PAUSED"))
                {
                    var rect = title.rectTransform;
                    rect.SetParent(menuArea, false);
                    rect.anchorMin = new Vector2(0.05f, 0.86f);
                    rect.anchorMax = new Vector2(0.95f, 1f);
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                    title.alignment = TextAlignmentOptions.Center;
                    title.enableAutoSizing = true;
                    title.fontSizeMax = 60f;
                    UITheme.ApplyText(title, ThemedElement.TextRole.Title);
                }
        }

        /// <summary>
        /// Turns Blind Fate on or off for the rest of this run and for the next runs: RunModes for now, the saved mode toggle
        /// for the character select screen (the same switch the player set there).
        /// </summary>
        public void ToggleBlindFate()
        {
            bool on = !SoulHunter.Gameplay.Data.RunModes.RandomLevelUp;
            SoulHunter.Gameplay.Data.RunModes.RandomLevelUp = on;
            var data = SoulHunter.Core.Persistence.GameSettings.Data;
            if (data != null)
            {
                SoulHunter.Gameplay.Data.RunModeOptions.Set(data, SoulHunter.Gameplay.Data.RunMode.RandomLevelUp, on);
                SoulHunter.Core.Persistence.GameSettings.Save();
            }
            RefreshBlindFate();
        }

        private void RefreshBlindFate()
        {
            if (_blindFateLabel == null) return;
            _blindFateLabel.text = SoulHunter.Gameplay.Data.RunModeOptions.Name(SoulHunter.Gameplay.Data.RunMode.RandomLevelUp)
                                   + (SoulHunter.Gameplay.Data.RunModes.RandomLevelUp ? ": ON" : ": OFF");
        }

        private void HandlePauseChanged(bool paused)
        {
            if (paused) RefreshBlindFate();
            // Over every HUD label on the same canvas (the stage label was drawn over the title on a phone, 2026-10-10).
            if (paused) transform.SetAsLastSibling();
            if (paused) Select(_resume);
            else if (_recipes != null) _recipes.Close(); // resumed with the pause key while the recipes were open
        }

        private static void Select(Selectable target)
        {
            if (target != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(target.gameObject);
        }

        private RectTransform _buttonColumn;

        private static RectTransform NewArea(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private Button CreateButton(string name, Vector2 min, Vector2 max, string label, System.Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(_buttonColumn != null ? _buttonColumn : transform, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = go.AddComponent<Image>();
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            // The theme decides the look; each button says what it is and which icon it shows.
            var kind = name == "Resume_Button" ? UITheme.ButtonKind.Primary : name == "Quit_Button" ? UITheme.ButtonKind.Danger : UITheme.ButtonKind.Normal;
            // Evolutions has no icon of its own (no new art); the Options gear would say the wrong thing.
            var icon = name == "Resume_Button" ? UITheme.IconSlot.Resume : name == "Quit_Button" ? UITheme.IconSlot.Quit
                : name == "Evolutions_Button" || name == "BlindFate_Button" ? UITheme.IconSlot.None : UITheme.IconSlot.Options;
            button.onClick.AddListener(() =>
            {
                var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
                if (audio != null) audio.PlaySFX(audio.UIConfirmSound);
                onClick();
            });
            var textGo = new GameObject("Text", typeof(RectTransform));
            var textRect = (RectTransform)textGo.transform;
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 16f;
            text.fontSizeMax = 34f;
            text.fontStyle = FontStyles.Bold;
            UITheme.ApplyButton(button, image, kind, icon); // after the label exists, so it styles it too
            text.raycastTarget = false;
            return button;
        }
    }
}
