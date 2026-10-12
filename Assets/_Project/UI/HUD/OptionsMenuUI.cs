using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using SoulHunter.Core.Input;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// VS-style Options screen (sections in the order of the VS wiki options page: Favorites, Display, Sound, Gameplay),
    /// opened from the main menu and the pause menu. Favorites: copies of Sounds, Music, Damage Numbers and Flashing VFX
    /// (VS: "a copy of some commonly changed options from other sections"; they change the same settings). Display: Window
    /// Mode, Resolution, Screen Refresh Rate (PC, Exclusive Fullscreen only), V-Sync, Flashing VFX, Disable Blood, Screen
    /// Shake, Show Defanged Enemies. Sound: Sounds, Music. Gameplay: Damage Numbers, Show Stage Progression, Visually
    /// Invert Stages, Visible Joystick, Joystick Type, Pause When Unfocused. Controls (Soul Hunter's own): keyboard and controller remapping.
    /// Fully usable with keyboard or controller; Esc / B closes it.
    /// Changes apply at once and are saved when the screen closes. Built at runtime on its own overlay canvas.
    ///
    /// Responsive (owner, 2026-10-10: landscape and portrait from the same screen). One layout, laid out again on every
    /// screen change (<see cref="UiScreen.Changed"/>) without being rebuilt:
    /// - the content sits in the safe area; the veil stays full screen;
    /// - the panel is at most <see cref="MaxPanelWidth"/> units wide; the tabs are a <see cref="ThemedRow"/> that wraps;
    /// - each section is a scrolling list of rows (label + control, at touch size on a touch screen); keyboard and
    ///   controller focus scroll the chosen row into view;
    /// - when a row is too narrow for label and control side by side (a portrait phone) the control goes under its label;
    /// - Back stays pinned to the bottom of the panel, always on screen.
    ///
    /// Save Data (main menu only; VS Account section, wiki Options, raw, fetched 2026-10-12): "DATA RECOVERY ... Restore" and
    /// "Delete Save ... This option only appears after clicking on the Account section button 6 times after initially
    /// selecting it"; 1.4.200: "click Delete Save and confirm through all the pop-ups". Soul Hunter has no accounts, Twitch
    /// or languages, so the section is named for what is left in it. See the "Save Data" region below.
    /// </summary>
    public sealed class OptionsMenuUI : MonoBehaviour
    {
        public static OptionsMenuUI Current { get; private set; }

        /// <summary>The widest the panel gets (units): wider screens keep it centred.</summary>
        public const float MaxPanelWidth = 1240f;
        /// <summary>Rows narrower than this (units) put the control under its label.</summary>
        public const float StackBelowWidth = 720f;

        private readonly List<Action> _refreshers = new List<Action>();
        private readonly List<Selectable> _selectables = new List<Selectable>();
        private Action _onClosed;
        private InputService _input;
        private InputActionRebindingExtensions.RebindingOperation _rebinding;
        private bool _pauseActionWasEnabled;

        public IReadOnlyList<Selectable> Selectables => _selectables;
        public bool IsRebinding => _rebinding != null;
        /// <summary>The button that closes the screen (pinned to the bottom of the panel).</summary>
        public Button BackButton => _back;
        /// <summary>True while the rows put each control under its label (a narrow screen).</summary>
        public bool IsStacked { get; private set; }

        /// <summary>Opens the Options screen on top of everything; onClosed runs after it closes.</summary>
        /// <param name="withSaveData">True from the main menu: adds the Save Data section (Data Recovery, Delete Save). A run in
        /// progress never shows it: its save is being played on.</param>
        public static OptionsMenuUI Open(Action onClosed = null, bool withSaveData = false)
        {
            if (Current != null) return Current;
            var root = new GameObject("OptionsMenu", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            SoulHunter.Gameplay.UI.ResponsiveCanvas.Apply(canvas); // the game's one UI scale (UiScreen), landscape and portrait
            root.AddComponent<GraphicRaycaster>();
            var options = root.AddComponent<OptionsMenuUI>();
            options._onClosed = onClosed;
            options._withSaveData = withSaveData;
            LastClosedReplacedSave = false;
            options.Build();
            return options;
        }

        private void Awake() => Current = this;

        private void OnEnable()
        {
            UiScreen.Changed += LayoutNow;
            UiThemeAsset.Changed += MarkDirty;
        }

        private void OnDisable()
        {
            UiScreen.Changed -= LayoutNow;
            UiThemeAsset.Changed -= MarkDirty;
        }

        private void OnDestroy()
        {
            UiScreen.Changed -= LayoutNow;
            UiThemeAsset.Changed -= MarkDirty;
            if (Current == this) Current = null;
            CancelRebinding();
            RestorePauseAction();
        }

        private void Update()
        {
            if (_rebinding != null) return;
            if (DialogOpen) { UpdateDialog(); return; } // a question is open: Esc answers it, and focus stays on its buttons
            bool back = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                        (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
            if (back) Close();
            // Keep keyboard/controller focus inside the screen (on the open section).
            if (EventSystem.current != null && _tabButtons.Count > 0 &&
                (EventSystem.current.currentSelectedGameObject == null ||
                 !EventSystem.current.currentSelectedGameObject.transform.IsChildOf(transform) ||
                 !EventSystem.current.currentSelectedGameObject.activeInHierarchy))
            {
                var items = _tabItems[_currentTab];
                EventSystem.current.SetSelectedGameObject(items.Count > 0 ? items[0].gameObject : _tabButtons[_currentTab].gameObject);
            }
            FollowFocus();
        }

        private void LateUpdate()
        {
            // A new canvas gets its real size a frame late, and the safe area can change: lay out when the room changes.
            if (_content != null && _content.rect.size != _laidOutFor) _dirty = true;
            if (_dirty) LayoutNow();
            if (DialogOpen && _dialogPanel != null && _content.rect.size != _dialogFor) LayoutDialog();
            ApplyTextFloor(); // a theme restyle puts button labels back to the theme's floor; the phone's floor is higher
        }

        /// <summary>Saves the options (including remapped controls) and closes the screen.</summary>
        public void Close()
        {
            var data = GameSettings.Data;
            if (data != null && _input != null) data.InputBindingOverrides = _input.SaveBindingOverrides();
            GameSettings.Save();
            LastClosedReplacedSave = SaveReplaced;
            var onClosed = _onClosed;
            Destroy(gameObject);
            onClosed?.Invoke();
        }

        // ---------- build ----------

        private RectTransform _content;
        private RectTransform _panel;
        private TextMeshProUGUI _title;
        private RectTransform _tabRow;
        private Button _back;
        private bool _built, _dirty;
        private Vector2 _laidOutFor = new Vector2(-1f, -1f);

        private void Build()
        {
            if (GameServices.Instance != null) GameServices.Instance.TryGet(out _input);
            // Esc toggles pause in a run; while Options is open it closes Options instead.
            var pause = _input?.Actions?.FindAction(InputService.PauseAction);
            if (pause != null) { _pauseActionWasEnabled = pause.enabled; pause.Disable(); }

            UITheme.AddVeil(transform); // the menu or game behind stays dark, full screen
            // The content lives in the safe area (notch, gesture bar); the panel is placed in it by LayoutNow.
            _content = Rect("OptionsContent", transform, Vector2.zero, Vector2.one);
            SafeAreaFitter.Fit(_content);
            _panel = Rect("Panel", _content, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            UITheme.ApplyPanel(_panel.gameObject.AddComponent<Image>());
            _title = Text("Title", _panel, Vector2.zero, Vector2.one, "Options", 44f, TextAlignmentOptions.Center);
            UITheme.ApplyText(_title, ThemedElement.TextRole.Title);

            var data = GameSettings.Data;
            // Data Recovery and Delete Save replace the save in memory: every option's setter below then writes to the new one.
            _rebindData = () => data = GameSettings.Data;
            // VS groups its options in sections; one tab per section, in the wiki's heading order (Favorites, Display, Sound,
            // Gameplay), then Soul Hunter's Controls. VS Co-op and Account have nothing behind them in Soul Hunter.
            // The tabs share the theme's one tab size and sit centred in a row with the theme's gap (ThemedRow), which
            // wraps onto more lines on a narrow screen.
            var sectionNames = new List<string> { "Favorites", "Display", "Sound", "Gameplay", "Controls" };
            if (_withSaveData) { _saveTab = sectionNames.Count; sectionNames.Add(SaveDataTabName); }
            string[] sections = sectionNames.ToArray();
            _tabRow = Rect("Tabs", _panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            _tabRow.pivot = new Vector2(0.5f, 1f);
            for (int i = 0; i < sections.Length; i++)
            {
                int index = i;
                var tab = AddButton(_tabRow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), sections[i], () => PressTab(index), ThemedElement.SizeRole.Tab);
                tab.name = "Tab_" + sections[i];
                _tabButtons.Add(tab);
            }
            _tabRow.gameObject.AddComponent<ThemedRow>();

            // Favorites (VS): copies of options from other sections, bound to the same settings, so a change in one place
            // shows in the other. Language and Classic Sound Effects have nothing behind them in Soul Hunter and are left out.
            var favorites = BeginTab("Favorites", _panel);
            AddSlider(favorites, "Sounds", () => GameSettings.SfxVolume, v => { if (data != null) data.SfxVolume = v; });
            AddSlider(favorites, "Music", () => GameSettings.MusicVolume, v => { if (data != null) data.MusicVolume = v; });
            AddToggle(favorites, "Damage Numbers", () => GameSettings.DamageNumbers, v => { if (data != null) data.DamageNumbers = v; });
            AddToggle(favorites, "Flashing VFX", () => GameSettings.FlashingVfx, v => { if (data != null) data.FlashingVfx = v; });

            // Display
            var display = BeginTab("Display", _panel);
            AddCycle(display, "Window Mode", () => GameSettings.WindowMode, () => SoulHunter.Gameplay.Core.DisplaySettingsApplier.WindowModeNames.Length,
                i => SoulHunter.Gameplay.Core.DisplaySettingsApplier.WindowModeNames[i], i => { if (data != null) data.WindowMode = i; });
            // Resolution cycles Native, then every resolution the display supports.
            AddCycle(display, "Resolution", () => GameSettings.ResolutionIndex + 1, () => SoulHunter.Gameplay.Core.DisplaySettingsApplier.Resolutions().Count + 1,
                i => SoulHunter.Gameplay.Core.DisplaySettingsApplier.ResolutionLabel(i - 1), i => { if (data != null) data.ResolutionIndex = i - 1; });
            // VS Screen Refresh Rate (PC-exclusive): "Only visible when using Exclusive Fullscreen Window Mode". The wiki puts
            // it between Resolution and Window Mode; Soul Hunter keeps Window Mode on top, so the row appears under the
            // control that reveals it and nothing above the focused Window Mode button moves (ui-ux-pro-max: no layout shift
            // under focus). Choices: Default, then the rates the display offers for the chosen resolution.
            var refresh = AddCycle(display, "Screen Refresh Rate", RefreshRateChoice, () => RefreshRateChoices().Count + 1,
                i => SoulHunter.Gameplay.Core.DisplaySettingsApplier.RefreshRateLabel(RefreshRateAt(i)),
                i => { if (data != null) data.RefreshRateHz = RefreshRateAt(i); });
            ShowOnlyWhen(refresh, () => SoulHunter.Gameplay.Core.DisplaySettingsApplier.ShowsRefreshRate(GameSettings.WindowMode, Application.isMobilePlatform));
            AddToggle(display, "V-Sync", () => GameSettings.VSync, v => { if (data != null) data.VSync = v; });
            AddToggle(display, "Flashing VFX", () => GameSettings.FlashingVfx, v => { if (data != null) data.FlashingVfx = v; });
            AddToggle(display, "Disable Blood", () => GameSettings.DisableBlood, v => { if (data != null) data.DisableBlood = v; });
            AddToggle(display, "Screen Shake", () => GameSettings.ScreenShake, v => { if (data != null) data.ScreenShake = v; });
            // VS Options, Display: the green look of Defanged enemies (Defang PowerUp).
            AddToggle(display, "Show Defanged Enemies", () => GameSettings.ShowDefangedEnemies, v => { if (data != null) data.ShowDefangedEnemies = v; });

            // Sound
            var sound = BeginTab("Sound", _panel);
            AddSlider(sound, "Sounds", () => GameSettings.SfxVolume, v => { if (data != null) data.SfxVolume = v; });
            AddSlider(sound, "Music", () => GameSettings.MusicVolume, v => { if (data != null) data.MusicVolume = v; });

            // Gameplay
            var gameplay = BeginTab("Gameplay", _panel);
            AddToggle(gameplay, "Damage Numbers", () => GameSettings.DamageNumbers, v => { if (data != null) data.DamageNumbers = v; });
            AddToggle(gameplay, "Show Stage Progression", () => GameSettings.ShowStageProgression, v => { if (data != null) data.ShowStageProgression = v; });
            // VS Gameplay (wiki Options): "Visually Invert Stages", listed right after "Show stage progression". It turns the
            // Inverse look (InverseMap: the turned layout and the darker hue) on or off; the Inverse numbers stay either way.
            AddToggle(gameplay, "Visually Invert Stages", () => GameSettings.VisuallyInvertStages, v => { if (data != null) data.VisuallyInvertStages = v; });
            AddToggle(gameplay, "Visible Joystick", () => GameSettings.VisibleJoystick, v => { if (data != null) data.VisibleJoystick = v; });
            // VS Gameplay: "Joystick Type" (Legacy, Default), right after the touch screen control stick (TouchControlsUI).
            AddCycle(gameplay, "Joystick Type", () => GameSettings.JoystickType, () => GameSettings.JoystickTypeNames.Length,
                i => GameSettings.JoystickTypeNames[i], i => { if (data != null) data.JoystickType = i; });
            AddToggle(gameplay, "Pause When Unfocused", () => GameSettings.PauseOnFocusLoss, v => { if (data != null) data.PauseOnFocusLoss = v; });

            // Controls: a header over the two binding columns (keyboard, controller), then one row per action.
            var controls = BeginTab("Controls", _panel);
            AddHeader(controls);
            if (_input != null && _input.Actions != null)
            {
                var move = _input.Actions.FindAction(InputService.MoveAction);
                foreach (var part in new[] { "up", "down", "left", "right" })
                    AddBindingRow(controls, "Move " + char.ToUpper(part[0]) + part.Substring(1), move, CompositePart(move, part), -1);
                foreach (var name in new[] { InputService.DashAction, InputService.UltimateAction, InputService.PauseAction })
                {
                    var action = _input.Actions.FindAction(name);
                    AddBindingRow(controls, name, action, BindingIndex(action, "<Keyboard>"), BindingIndex(action, "<Gamepad>"));
                }
            }
            var resetLine = Rect("ResetRow", controls, new Vector2(0f, 1f), new Vector2(1f, 1f));
            var reset = AddButton(resetLine, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "Reset Controls", () =>
            {
                _input?.ResetBindings();
                RefreshAll();
            });
            reset.name = "Reset Controls";
            AddRow(controls, new OptionRow { Line = resetLine, Kind = RowKind.Action, First = (RectTransform)reset.transform });
            _building.Add(reset);
            _selectables.Add(reset);

            if (_withSaveData) BuildSaveData(BeginTab(SaveDataTabName, _panel));

            // Back: the button that ends the screen, pinned to the bottom of the panel (always on screen).
            _back = AddButton(_panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), "Back", Close);
            _back.name = "Back";
            _selectables.Add(_back);

            _built = true;
            RefreshAll();
            LayoutNow();
            ShowTab(0);
        }

        // ---------- sections ----------

        private readonly List<ScrollRect> _tabs = new List<ScrollRect>();
        private readonly List<List<Selectable>> _tabItems = new List<List<Selectable>>();
        private readonly List<Button> _tabButtons = new List<Button>();
        private List<Selectable> _building = new List<Selectable>();
        private int _currentTab;

        public int CurrentTab => _currentTab;
        public IReadOnlyList<Button> TabButtons => _tabButtons;
        /// <summary>Each section's scrolling list (its viewport is named after the section).</summary>
        public IReadOnlyList<ScrollRect> Sections => _tabs;

        /// <summary>
        /// A section: a viewport named after it (masked, scrolls vertically) with the rows' content inside. Returns the
        /// content, which the rows are added to.
        /// </summary>
        private RectTransform BeginTab(string name, RectTransform panel)
        {
            var viewport = Rect(name, panel, Vector2.zero, Vector2.one);
            // The viewport is the section itself; a drag on the gaps between rows scrolls the list.
            var scroll = ResponsiveScroll.Create(viewport, null, "Content", out var content);
            _tabs.Add(scroll);
            _building = new List<Selectable>();
            _tabItems.Add(_building);
            return content;
        }

        /// <summary>Shows one section and focuses its first control (keyboard/controller friendly).</summary>
        public void ShowTab(int index)
        {
            RefreshAll(); // the Favorites copies and their home rows show the same values
            _currentTab = Mathf.Clamp(index, 0, _tabs.Count - 1);
            for (int i = 0; i < _tabs.Count; i++) _tabs[i].gameObject.SetActive(i == _currentTab);
            for (int i = 0; i < _tabButtons.Count; i++)
                UITheme.ApplyButton(_tabButtons[i], _tabButtons[i].targetGraphic as Image, i == _currentTab ? UITheme.ButtonKind.Primary : UITheme.ButtonKind.Normal, UITheme.IconSlot.None, true, ThemedElement.SizeRole.Tab); // the open section is the main-action colour
            _tabs[_currentTab].StopMovement();
            _tabs[_currentTab].content.anchoredPosition = Vector2.zero; // a section opens at its top
            ApplyTextFloor();
            var first = _tabItems[_currentTab].Count > 0 ? _tabItems[_currentTab][0] : null;
            if (first != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
            _lastFocus = first != null ? first.gameObject : null;
            if (_currentTab == _saveTab) RefreshSaveData(); // both saves as they are on disk now
        }

        private void RefreshAll()
        {
            foreach (var refresh in _refreshers) refresh();
            RestackConditionalRows();
            ApplyTextFloor();
        }

        // ---------- rows shown only in some settings (VS Screen Refresh Rate) ----------

        private readonly List<(RectTransform line, Func<bool> visible)> _conditional = new List<(RectTransform, Func<bool>)>();

        /// <summary>
        /// Shows the row of <paramref name="control"/> only while <paramref name="visible"/> holds. A hidden row is inactive,
        /// so keyboard and controller focus skip it, and the rows under it close up.
        /// </summary>
        private void ShowOnlyWhen(Selectable control, Func<bool> visible)
        {
            _conditional.Add(((RectTransform)control.transform.parent, visible));
            RestackConditionalRows();
        }

        private void RestackConditionalRows()
        {
            if (_conditional.Count == 0) return;
            bool changed = false;
            foreach (var (line, visible) in _conditional)
            {
                bool show = visible();
                if (line.gameObject.activeSelf != show) { line.gameObject.SetActive(show); changed = true; }
            }
            if (changed && _built) LayoutNow(); // the rows under a hidden one close up
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected != null && selected.transform.IsChildOf(transform) && !selected.activeInHierarchy)
                EventSystem.current.SetSelectedGameObject(null); // Update() puts focus back on the open section
        }

        /// <summary>The refresh rates (whole hertz) offered for the chosen resolution, lowest first.</summary>
        private static List<int> RefreshRateChoices() =>
            SoulHunter.Gameplay.Core.DisplaySettingsApplier.RefreshRates(SoulHunter.Gameplay.Core.DisplaySettingsApplier.ChosenSize());

        /// <summary>The Screen Refresh Rate choice now set: 0 is Default (also when the stored rate is not offered here).</summary>
        private static int RefreshRateChoice()
        {
            int index = RefreshRateChoices().IndexOf(GameSettings.RefreshRateHz);
            return GameSettings.RefreshRateHz > 0 && index >= 0 ? index + 1 : 0;
        }

        /// <summary>The refresh rate (whole hertz) at a Screen Refresh Rate choice: 0 is Default.</summary>
        private static int RefreshRateAt(int choice)
        {
            var rates = RefreshRateChoices();
            return choice >= 1 && choice <= rates.Count ? rates[choice - 1] : 0;
        }

        private void AddSlider(RectTransform content, string label, Func<float> get, Action<float> set)
        {
            var row = Line(content, label, RowKind.Slider);
            // The slider's own rect is the touch target (a row-high strip); the groove and fill are a thin bar in its middle.
            var sliderRect = Rect("Slider", row.Line, Vector2.zero, Vector2.zero);
            var hit = sliderRect.gameObject.AddComponent<Image>();
            hit.color = Color.clear; // the whole strip takes the press, not only the thin groove
            var groove = Rect("Groove", sliderRect, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f));
            UITheme.ApplySurface(groove.gameObject.AddComponent<Image>(), ThemedElement.SurfaceRole.Groove);
            var fillArea = Rect("Fill Area", sliderRect, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f));
            var fill = Rect("Fill", fillArea, Vector2.zero, Vector2.one);
            UITheme.ApplySurface(fill.gameObject.AddComponent<Image>(), ThemedElement.SurfaceRole.Accent);
            var handleArea = Rect("Handle Area", sliderRect, Vector2.zero, Vector2.one);
            // A round knob (the theme's knob picture; a plate in themes without one), sized by LayoutNow.
            var handle = Rect("Handle", handleArea, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
            handle.sizeDelta = new Vector2(KnobUnits, KnobUnits);
            var handleImage = handle.gameObject.AddComponent<Image>();
            var slider = sliderRect.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            UITheme.ApplyButton(slider, handleImage, UITheme.ButtonKind.Normal, UITheme.IconSlot.None, false, ThemedElement.SizeRole.Free);
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(get());
            var value = Text("Value", row.Line, Vector2.zero, Vector2.zero, "", 22f, TextAlignmentOptions.Right); // inside the row, clear of the handle
            slider.onValueChanged.AddListener(v =>
            {
                set(v);
                value.text = Mathf.RoundToInt(v * 100f) + "%";
                GameSettings.NotifyChanged();
            });
            _refreshers.Add(() => { slider.SetValueWithoutNotify(get()); value.text = Mathf.RoundToInt(get() * 100f) + "%"; });
            row.First = sliderRect;
            row.Groove = groove;
            row.FillArea = fillArea;
            row.HandleArea = handleArea;
            row.Handle = handle;
            row.Value = value;
            _selectables.Add(slider);
            _building.Add(slider);
        }

        private void AddToggle(RectTransform content, string label, Func<bool> get, Action<bool> set)
        {
            var row = Line(content, label, RowKind.Switch);
            // The switch is the theme's size; on a touch screen its holder scales it up to the touch target (LayoutNow).
            // The holder takes the row's name, so the switch still belongs to its row by name.
            var holder = Rect(label, row.Line, new Vector2(0f, 1f), new Vector2(0f, 1f));
            holder.pivot = new Vector2(0f, 1f);
            TextMeshProUGUI state = null;
            var button = AddButton(holder, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "", () =>
            {
                set(!get());
                GameSettings.NotifyChanged();
                RefreshAll();
            }, ThemedElement.SizeRole.Free);
            ((RectTransform)button.transform).pivot = new Vector2(0.5f, 0.5f);
            var switchLook = ThemedElement.For(button.gameObject);
            switchLook.IsSwitch = true;
            switchLook.SwitchOn = get();
            switchLook.Apply();
            state = button.GetComponentInChildren<TextMeshProUGUI>();
            // Text as well as colour, so the state never depends on colour alone.
            // The words carry the state; the switch's text takes light or dark by contrast with its colour (the theme decides).
            _refreshers.Add(() => { switchLook.SwitchOn = get(); state.text = get() ? "ON" : "OFF"; switchLook.Apply(); });
            button.name = label;
            row.First = holder;
            _selectables.Add(button);
            _building.Add(button);
        }

        /// <summary>A button that steps through a list of choices (VS Window Mode, Resolution).</summary>
        private Button AddCycle(RectTransform content, string label, Func<int> get, Func<int> count,
            Func<int, string> labelFor, Action<int> set)
        {
            var row = Line(content, label, RowKind.Cycle);
            var button = AddButton(row.Line, Vector2.zero, Vector2.zero, "", () =>
            {
                int n = Mathf.Max(1, count());
                set((Mathf.Clamp(get(), 0, n - 1) + 1) % n);
                GameSettings.NotifyChanged();
                RefreshAll();
            }, ThemedElement.SizeRole.Free);
            var text = button.GetComponentInChildren<TextMeshProUGUI>();
            _refreshers.Add(() => text.text = labelFor(Mathf.Clamp(get(), 0, Mathf.Max(1, count()) - 1)));
            button.name = label;
            row.First = (RectTransform)button.transform;
            _selectables.Add(button);
            _building.Add(button);
            return button;
        }

        /// <summary>The Controls header: the section's name over the labels, Keyboard and Controller over the two columns.</summary>
        private void AddHeader(RectTransform content)
        {
            var line = Rect("ControlsHeader", content, new Vector2(0f, 1f), new Vector2(1f, 1f));
            var row = new OptionRow { Line = line, Kind = RowKind.Header };
            row.Label = Text("ControlsTitle", line, Vector2.zero, Vector2.zero, "Controls", 26f, TextAlignmentOptions.Left);
            UITheme.ApplyText(row.Label, ThemedElement.TextRole.Accent);
            var keyboard = Text("KeyboardTitle", line, Vector2.zero, Vector2.zero, "Keyboard", 26f, TextAlignmentOptions.Center);
            UITheme.ApplyText(keyboard, ThemedElement.TextRole.Accent);
            var controller = Text("ControllerTitle", line, Vector2.zero, Vector2.zero, "Controller", 26f, TextAlignmentOptions.Center);
            UITheme.ApplyText(controller, ThemedElement.TextRole.Accent);
            row.First = keyboard.rectTransform;
            row.Second = controller.rectTransform;
            AddRow(content, row);
        }

        private void AddBindingRow(RectTransform content, string label, InputAction action, int keyboardIndex, int gamepadIndex)
        {
            if (action == null) return;
            var row = Line(content, label, RowKind.Binding);
            row.First = AddBindingButton(row.Line, action, keyboardIndex, "<Keyboard>");
            if (gamepadIndex >= 0) row.Second = AddBindingButton(row.Line, action, gamepadIndex, "<Gamepad>");
            else
            {
                var fixedText = Text("Fixed", row.Line, Vector2.zero, Vector2.zero, "Left stick / D-pad", 20f, TextAlignmentOptions.Center);
                fixedText.textWrappingMode = TextWrappingModes.Normal;
                row.Second = fixedText.rectTransform;
            }
        }

        private RectTransform AddBindingButton(RectTransform line, InputAction action, int index, string device)
        {
            if (index < 0) return null;
            Button button = null;
            button = AddButton(line, Vector2.zero, Vector2.zero, "", () => StartRebinding(action, index, device, button), ThemedElement.SizeRole.Free);
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            _refreshers.Add(() => label.text = action.GetBindingDisplayString(index));
            button.name = $"{action.name}_{device}";
            _selectables.Add(button);
            _building.Add(button);
            return (RectTransform)button.transform;
        }

        /// <summary>VS "remap controls": waits for the next key or button of the same device type.</summary>
        private void StartRebinding(InputAction action, int index, string device, Button button)
        {
            if (_rebinding != null) return;
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            label.text = "Press...";
            bool wasEnabled = action.enabled;
            action.Disable();
            _rebinding = action.PerformInteractiveRebinding(index)
                .WithControlsHavingToMatchPath(device)
                .WithCancelingThrough(device == "<Gamepad>" ? "<Gamepad>/select" : "<Keyboard>/backquote")
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(_ => FinishRebinding(action, wasEnabled))
                .OnCancel(_ => FinishRebinding(action, wasEnabled))
                .Start();
        }

        private void FinishRebinding(InputAction action, bool reenable)
        {
            _rebinding?.Dispose();
            _rebinding = null;
            if (reenable && action.name != InputService.PauseAction) action.Enable();
            RefreshAll();
        }

        private void CancelRebinding()
        {
            if (_rebinding == null) return;
            _rebinding.Cancel();
            _rebinding?.Dispose();
            _rebinding = null;
        }

        private void RestorePauseAction()
        {
            var pause = _input?.Actions?.FindAction(InputService.PauseAction);
            if (pause != null && _pauseActionWasEnabled) pause.Enable();
        }

        // ---------- binding lookup ----------

        public static int BindingIndex(InputAction action, string devicePrefix)
        {
            if (action == null) return -1;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var b = action.bindings[i];
                if (!b.isComposite && !b.isPartOfComposite && b.path.StartsWith(devicePrefix)) return i;
            }
            return -1;
        }

        /// <summary>Index of a part ("up", "left"...) in the action's first keyboard composite (WASD).</summary>
        public static int CompositePart(InputAction action, string part)
        {
            if (action == null) return -1;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (!action.bindings[i].isComposite) continue;
                for (int j = i + 1; j < action.bindings.Count && action.bindings[j].isPartOfComposite; j++)
                    if (string.Equals(action.bindings[j].name, part, StringComparison.OrdinalIgnoreCase)) return j;
                return -1;
            }
            return -1;
        }

        // ---------- responsive layout ----------

        private enum RowKind { Slider, Switch, Cycle, Binding, Header, Action, Note }

        /// <summary>One row of a section: its line, its label and its control(s), placed by <see cref="LayoutNow"/>.</summary>
        private sealed class OptionRow
        {
            public RectTransform Line;
            public TextMeshProUGUI Label;
            public RowKind Kind;
            public RectTransform First, Second; // the control (switch holder, slider, cycle, key) and a binding's second column
            public RectTransform Groove, FillArea, HandleArea, Handle; // a slider's parts
            public TextMeshProUGUI Value; // a slider's percentage
            public TextMeshProUGUI Note; // a Note row's wrapped text (the row is as tall as the text)
        }

        private readonly Dictionary<RectTransform, List<OptionRow>> _rows = new Dictionary<RectTransform, List<OptionRow>>();

        // Fixed sizes in canvas units (the 1920x1080 layout's); on a touch screen controls grow to the touch target.
        private const float KnobUnits = 40f;
        private const float GrooveUnits = 22f;
        private const float RowPad = 8f, RowGap = 8f, Inset = 16f, ValueWidth = 96f, MaxChoiceWidth = 600f;

        private void AddRow(RectTransform content, OptionRow row)
        {
            if (!_rows.TryGetValue(content, out var list)) _rows[content] = list = new List<OptionRow>();
            list.Add(row);
        }

        private void MarkDirty() => _dirty = true;

        /// <summary>
        /// Places the panel, title, tabs, sections and Back for the room the safe area gives, then every row. Called on
        /// every screen change (rotation, window resize) and when a row shows or hides; nothing is rebuilt, so values,
        /// the open tab and focus stay.
        /// </summary>
        private void LayoutNow()
        {
            if (!_built || this == null || _content == null) return;
            _dirty = false;
            var theme = UiThemeAsset.Active;
            float gap = theme.Layouts.ButtonGap;
            Vector2 room = _content.rect.size;
            _laidOutFor = room;
            if (room.x <= 1f || room.y <= 1f) { _dirty = true; return; }

            // Panel: centred, at most MaxPanelWidth wide, a margin of air around it.
            float marginX = Mathf.Clamp(room.x * 0.03f, gap, 48f);
            float marginY = room.y < 700f ? gap : Mathf.Clamp(room.y * 0.05f, gap, 54f); // a short screen keeps its height for the list
            float panelW = Mathf.Min(room.x - 2f * marginX, MaxPanelWidth);
            float panelH = room.y - 2f * marginY;
            _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.pivot = new Vector2(0.5f, 0.5f);
            _panel.anchoredPosition = Vector2.zero;
            _panel.sizeDelta = new Vector2(panelW, panelH);

            bool shortPanel = panelH < 640f; // a phone held sideways: a smaller title and tighter padding
            float pad = shortPanel || panelW < StackBelowWidth ? gap : 24f;
            float titleH = shortPanel ? 44f : 76f;
            var title = _title.rectTransform;
            title.anchorMin = new Vector2(0f, 1f);
            title.anchorMax = new Vector2(1f, 1f);
            title.pivot = new Vector2(0.5f, 1f);
            title.sizeDelta = new Vector2(-2f * pad, titleH);
            title.anchoredPosition = new Vector2(0f, -pad);
            _title.fontSizeMax = shortPanel ? 36f : 44f;

            // Tabs: the wrapping row under the title.
            _tabRow.anchorMin = _tabRow.anchorMax = new Vector2(0.5f, 1f);
            _tabRow.pivot = new Vector2(0.5f, 1f);
            _tabRow.anchoredPosition = new Vector2(0f, -(pad + titleH));
            var row = _tabRow.GetComponent<ThemedRow>();
            if (row != null) row.Layout();
            float tabsH = _tabRow.sizeDelta.y * _tabRow.localScale.y;

            // Back: the theme's standard size, pinned to the bottom edge.
            var backLook = _back.GetComponent<ThemedElement>();
            Vector2 backSize = backLook != null && backLook.StandardSize(theme) is Vector2 b ? b : new Vector2(theme.Layouts.ButtonWidth, theme.Layouts.ButtonHeight);
            var back = (RectTransform)_back.transform;
            back.anchorMin = back.anchorMax = new Vector2(0.5f, 0f);
            back.pivot = new Vector2(0.5f, 0.5f);
            back.sizeDelta = backSize;
            back.anchoredPosition = new Vector2(0f, pad + backSize.y * 0.5f);

            // Sections: the room between the tabs (and the open tab's bar under them) and Back.
            float top = pad + titleH + tabsH + gap * 1.5f;
            float bottom = pad + backSize.y + gap;
            foreach (var section in _tabs)
            {
                var viewport = (RectTransform)section.transform;
                viewport.anchorMin = Vector2.zero;
                viewport.anchorMax = Vector2.one;
                viewport.offsetMin = new Vector2(pad, bottom);
                viewport.offsetMax = new Vector2(-pad, -top);
            }

            float width = panelW - 2f * pad;
            IsStacked = width < StackBelowWidth;
            foreach (var pair in _rows) LayoutRows(pair.Key, pair.Value, width, IsStacked, theme, backSize);
            if (DialogOpen) LayoutDialog();
            ApplyTextFloor();
        }

        /// <summary>A Note row's text at one readable size, wrapped: its height for <paramref name="width"/>.</summary>
        private static float NoteHeight(TextMeshProUGUI note, float width, float floor)
        {
            if (note == null) return 0f;
            note.enableAutoSizing = false;
            note.fontSize = Mathf.Max(NoteTextUnits, floor);
            return Mathf.Ceil(note.GetPreferredValues(note.text, Mathf.Max(1f, width), 0f).y) + 2f;
        }

        private void LayoutRows(RectTransform content, List<OptionRow> rows, float width, bool stacked, UiThemeAsset theme, Vector2 buttonSize)
        {
            float gap = theme.Layouts.ButtonGap;
            float touch = UiScreen.MinTouchUnits;
            float floor = TextFloor;
            float controlH = Mathf.Max(56f, touch);
            float sliderH = Mathf.Max(44f, touch);
            float switchScale = Mathf.Max(1f, theme.Layouts.SwitchHeight > 0f ? touch / theme.Layouts.SwitchHeight : 1f);
            Vector2 switchBase = new Vector2(theme.Layouts.SwitchWidth, theme.Layouts.SwitchHeight);
            Vector2 switchSize = switchBase * switchScale;
            float labelH = Mathf.Max(40f, floor * 1.6f);
            float knob = Mathf.Max(KnobUnits, touch * 0.72f);
            // A PC keeps its approved row density (Controls: seven bindings and Reset Controls fit without scrolling);
            // a touch screen gets the roomier rows.
            float rowPad = touch > 0f ? RowPad : RowPad * 0.5f;

            // Wide: label column on the left, controls in a column on the right. Stacked: label on top, control under it.
            float labelW = stacked ? width - 2f * Inset : Mathf.Clamp(width * 0.42f, 220f, 440f);
            float colX = stacked ? Inset : Inset + labelW + gap;
            float colW = Mathf.Max(1f, width - Inset - colX);
            float half = Mathf.Max(1f, (colW - gap) * 0.5f);

            float y = 0f;
            foreach (var r in rows)
            {
                if (!r.Line.gameObject.activeSelf) continue;
                float ctrl = r.Kind switch
                {
                    RowKind.Slider => sliderH,
                    RowKind.Switch => switchSize.y,
                    RowKind.Header => labelH,
                    RowKind.Action => buttonSize.y,
                    RowKind.Note => NoteHeight(r.Note, width - 2f * Inset, floor),
                    _ => controlH
                };
                float rowH, labelTop, ctrlTop;
                if (r.Kind == RowKind.Action || r.Kind == RowKind.Note) { rowH = ctrl + 2f * rowPad; labelTop = ctrlTop = rowPad; }
                else if (stacked)
                {
                    labelTop = rowPad;
                    ctrlTop = rowPad + labelH + 4f;
                    rowH = ctrlTop + ctrl + rowPad;
                }
                else
                {
                    rowH = Mathf.Max(64f, Mathf.Max(ctrl, labelH) + 2f * rowPad);
                    labelTop = (rowH - labelH) * 0.5f;
                    ctrlTop = (rowH - ctrl) * 0.5f;
                }

                var line = r.Line;
                line.anchorMin = new Vector2(0f, 1f);
                line.anchorMax = new Vector2(1f, 1f);
                line.pivot = new Vector2(0.5f, 1f);
                line.sizeDelta = new Vector2(0f, rowH);
                line.anchoredPosition = new Vector2(0f, -y);

                if (r.Label != null) Place(r.Label.rectTransform, Inset, labelTop, labelW, labelH);
                switch (r.Kind)
                {
                    case RowKind.Switch:
                        r.First.sizeDelta = switchBase;
                        r.First.localScale = new Vector3(switchScale, switchScale, 1f);
                        r.First.anchorMin = r.First.anchorMax = new Vector2(0f, 1f);
                        r.First.pivot = new Vector2(0f, 1f);
                        r.First.anchoredPosition = new Vector2(colX, -ctrlTop);
                        break;
                    case RowKind.Slider:
                        float sliderW = Mathf.Max(1f, colW - ValueWidth - gap);
                        Place(r.First, colX, ctrlTop, sliderW, sliderH);
                        r.Groove.sizeDelta = r.FillArea.sizeDelta = new Vector2(0f, GrooveUnits);
                        r.Groove.anchoredPosition = r.FillArea.anchoredPosition = Vector2.zero;
                        // The knob stays inside the strip at both ends.
                        r.HandleArea.offsetMin = new Vector2(knob * 0.5f, 0f);
                        r.HandleArea.offsetMax = new Vector2(-knob * 0.5f, 0f);
                        r.Handle.sizeDelta = new Vector2(knob, knob);
                        Place(r.Value.rectTransform, colX + colW - ValueWidth, ctrlTop, ValueWidth, sliderH);
                        break;
                    case RowKind.Cycle:
                        Place(r.First, colX, ctrlTop, Mathf.Min(colW, MaxChoiceWidth), controlH);
                        break;
                    case RowKind.Binding:
                    case RowKind.Header:
                        float h = r.Kind == RowKind.Header ? labelH : controlH;
                        if (r.First != null) Place(r.First, colX, ctrlTop, half, h);
                        if (r.Second != null) Place(r.Second, colX + half + gap, ctrlTop, half, h);
                        break;
                    case RowKind.Note:
                        Place(r.First, Inset, ctrlTop, Mathf.Max(1f, width - 2f * Inset), ctrl);
                        break;
                    case RowKind.Action:
                        r.First.anchorMin = r.First.anchorMax = new Vector2(0.5f, 1f);
                        r.First.pivot = new Vector2(0.5f, 0.5f);
                        r.First.sizeDelta = buttonSize;
                        r.First.anchoredPosition = new Vector2(0f, -rowH * 0.5f);
                        break;
                }
                y += rowH + RowGap;
            }
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, Mathf.Max(0f, y - RowGap));
            // Keep the scroll position inside the new content height.
            var viewport = content.parent as RectTransform;
            float maxScroll = viewport != null ? Mathf.Max(0f, content.sizeDelta.y - viewport.rect.height) : 0f;
            content.anchoredPosition = new Vector2(0f, Mathf.Clamp(content.anchoredPosition.y, 0f, maxScroll));
        }

        /// <summary>A rect at (x, top) from its parent's top-left corner, w x h units.</summary>
        private static void Place(RectTransform rect, float x, float top, float w, float h)
        {
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(w, h);
            rect.anchoredPosition = new Vector2(x, -top);
        }

        /// <summary>The smallest text on this screen: the theme's 12, or 12 sp on a touch screen (UiScreen).</summary>
        private static float TextFloor => Mathf.Max(12f, UiScreen.MinTextUnits);

        private readonly List<TMP_Text> _texts = new List<TMP_Text>();

        /// <summary>No label on this screen is drawn under the text floor (labels shrink to fit down to it, not past it).</summary>
        private void ApplyTextFloor()
        {
            if (!_built) return;
            if (_texts.Count == 0) GetComponentsInChildren(true, _texts);
            float floor = TextFloor;
            foreach (var text in _texts)
            {
                if (text == null || !text.enableAutoSizing) continue;
                if (text.fontSizeMax < floor) text.fontSizeMax = floor;
                if (text.fontSizeMin < floor) text.fontSizeMin = floor;
            }
        }

        // ---------- focus follows into view ----------

        private GameObject _lastFocus;

        /// <summary>
        /// Keyboard and controller focus scroll the open list so the chosen row is fully in view. A row chosen by the pointer
        /// (a tap, the mouse) is already where the player is looking, so the list stays put.
        /// </summary>
        private void FollowFocus()
        {
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected == _lastFocus) return;
            _lastFocus = selected;
            if (selected == null || _tabs.Count == 0) return;
            var section = _tabs[_currentTab];
            if (!selected.transform.IsChildOf(section.content)) return;
            if (PointerIsOver((RectTransform)selected.transform)) return;
            ResponsiveScroll.KeepInView(section, (RectTransform)selected.transform, wholeRow: true);
        }

        private static bool PointerIsOver(RectTransform rect)
        {
            Vector2? point = null;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) point = Touchscreen.current.primaryTouch.position.ReadValue();
            else if (Mouse.current != null) point = Mouse.current.position.ReadValue();
            if (!point.HasValue) return false;
            var canvas = rect.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.rootCanvas.worldCamera : null;
            return RectTransformUtility.RectangleContainsScreenPoint(rect, point.Value, camera);
        }

        // ---------- Save Data (main menu only) ----------
        // vampire.survivors.wiki/w/Options (raw, fetched 2026-10-12), Account section:
        // - "DATA RECOVERY ... Restore" (update history: "0.5.209 Savedata Recovery Menu");
        // - "Delete Save ... This option only appears after clicking on the Account section button 6 times after initially
        //   selecting it"; 1.4.200: "tap the Account icon seven times, then click Delete Save and confirm through all the
        //   pop-ups" (the first of the seven selects the section; six more reveal the option).
        // Balance method where the wiki is silent (owner rule, 2026-10-09):
        // - The section's name. VS calls it Account (Twitch, language, recovery). Soul Hunter has only the save in it, so it
        //   is "Save Data", the last tab, and only on the main menu: a run in progress is playing on the save.
        // - What Recovery restores and what Delete keeps: SaveService.RestorePrevious and DeleteSave say (the previous good
        //   save; progress goes, this device's options stay, a backup file is kept either way).
        // - Before a restore both saves are put side by side in words, so it is not a blind swap; afterwards the section says
        //   what was restored (ui-ux-pro-max: confirm before an irreversible action, then a brief success message).
        // - "All the pop-ups" are two questions for Delete Save; Recovery, which loses nothing, asks once. Each question
        //   opens with the safe answer chosen, and the buttons say what they do (Restore / Cancel, Delete Save / Keep Save).
        // - The count of six starts again each time the section is selected from another one, and after a delete.

        public const string SaveDataTabName = "Save Data";
        public const int DeletePressesNeeded = 6;
        public const float NoteTextUnits = 22f;
        public const string RestoreLabel = "Restore", DeleteLabel = "Delete Save", KeepLabel = "Keep Save", CancelLabel = "Cancel";
        public const string SaveIntroLine = "The game keeps the save from before its last change. Data Recovery brings that one back.";
        public const string NoPreviousLine = "Previous save: none yet. It appears after the game has saved twice.";

        /// <summary>True when the Options screen closed last had restored or deleted the save: the menu behind it shows old numbers.</summary>
        public static bool LastClosedReplacedSave { get; private set; }
        /// <summary>True once this screen has restored or deleted the save.</summary>
        public bool SaveReplaced { get; private set; }
        /// <summary>The Save Data section's index, or -1 when this screen has none (opened in a run).</summary>
        public int SaveDataTab => _saveTab;
        public Button RestoreButton { get; private set; }
        /// <summary>Hidden until the Save Data tab has been pressed <see cref="DeletePressesNeeded"/> more times after selecting it.</summary>
        public Button DeleteSaveButton { get; private set; }
        public bool DeleteSaveShown => DeleteSaveButton != null && DeleteSaveButton.gameObject.activeInHierarchy;
        /// <summary>The current and the previous save, side by side in words.</summary>
        public string SaveComparisonText => _saveCompare != null ? _saveCompare.text : null;
        /// <summary>What the last restore or delete did ("" before any).</summary>
        public string SaveStatusText => _saveStatus != null ? _saveStatus.text : null;
        public bool DialogOpen => _dialog != null && _dialog.gameObject.activeSelf;
        public string DialogText => _dialogText != null ? _dialogText.text : null;
        public Button DialogConfirmButton => _dialogYes;
        public Button DialogCancelButton => _dialogNo;

        private bool _withSaveData;
        private int _saveTab = -1;
        private int _saveTabPresses;
        private Action _rebindData;
        private TextMeshProUGUI _saveCompare, _saveStatus;

        /// <summary>A tab was pressed: shows its section. Pressing the open Save Data tab again counts toward Delete Save.</summary>
        public void PressTab(int index)
        {
            if (index == _saveTab) _saveTabPresses = _currentTab == index ? _saveTabPresses + 1 : 0; // selecting it starts the count
            ShowTab(index);
        }

        private static SaveService Saves() =>
            GameServices.Instance != null && GameServices.Instance.TryGet(out SaveService save) ? save : null;

        private void BuildSaveData(RectTransform content)
        {
            UITheme.ApplyText(AddNote(content, "SaveIntro", SaveIntroLine), ThemedElement.TextRole.Soft);

            var restoreRow = Line(content, "Data Recovery", RowKind.Cycle);
            RestoreButton = AddButton(restoreRow.Line, Vector2.zero, Vector2.zero, RestoreLabel, AskRestore, ThemedElement.SizeRole.Free);
            RestoreButton.name = "Data Recovery";
            restoreRow.First = (RectTransform)RestoreButton.transform;
            _selectables.Add(RestoreButton);
            _building.Add(RestoreButton);

            _saveCompare = AddNote(content, "SaveCompare", "");

            var deleteRow = Line(content, DeleteLabel, RowKind.Cycle);
            DeleteSaveButton = AddButton(deleteRow.Line, Vector2.zero, Vector2.zero, DeleteLabel, AskDelete, ThemedElement.SizeRole.Free);
            DeleteSaveButton.name = DeleteLabel;
            // The leaving colour: this is the one button here that takes something away.
            UITheme.ApplyButton(DeleteSaveButton, DeleteSaveButton.targetGraphic as Image, UITheme.ButtonKind.Danger, UITheme.IconSlot.None, true, ThemedElement.SizeRole.Free);
            deleteRow.First = (RectTransform)DeleteSaveButton.transform;
            _selectables.Add(DeleteSaveButton);
            _building.Add(DeleteSaveButton);
            Func<bool> deleteShown = () => _saveTabPresses >= DeletePressesNeeded;
            _conditional.Add((deleteRow.Line, deleteShown));

            _saveStatus = AddNote(content, "SaveStatus", "");
            UITheme.ApplyText(_saveStatus, ThemedElement.TextRole.Accent);
            Func<bool> statusShown = () => !string.IsNullOrEmpty(_saveStatus.text);
            _conditional.Add(((RectTransform)_saveStatus.transform.parent, statusShown));
            RestackConditionalRows();
        }

        /// <summary>A row of wrapped text across the section (no control); the row is as tall as its text.</summary>
        private TextMeshProUGUI AddNote(RectTransform content, string name, string text)
        {
            var line = Rect(name, content, new Vector2(0f, 1f), new Vector2(1f, 1f));
            var note = Text("Note", line, Vector2.zero, Vector2.zero, text, NoteTextUnits, TextAlignmentOptions.TopLeft);
            note.enableAutoSizing = false; // one readable size, wrapped (LayoutRows gives the row the text's height)
            note.textWrappingMode = TextWrappingModes.Normal;
            AddRow(content, new OptionRow { Line = line, Kind = RowKind.Note, First = note.rectTransform, Note = note });
            return note;
        }

        /// <summary>A save in the words a player knows it by.</summary>
        public static string Describe(SaveService.SaveSummary save)
        {
            if (save == null) return "nothing";
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            string text = save.Gold.ToString("N0", culture) + " gold, "
                          + Counted(save.CharactersBought, "character", "characters") + " bought, "
                          + Counted(save.PowerUpRanks, "Power Up rank", "Power Up ranks") + " bought, "
                          + Counted(save.ChestsOpened, "chest", "chests") + " opened";
            if (save.Written > DateTime.MinValue) text += " (saved " + save.Written.ToString("d MMM yyyy, HH:mm", culture) + ")";
            return text;
        }

        private static string Counted(int count, string one, string many) => count + " " + (count == 1 ? one : many);

        /// <summary>Reads both saves again: the comparison, and whether there is anything to restore.</summary>
        private void RefreshSaveData()
        {
            if (!_withSaveData || _saveCompare == null) return;
            var save = Saves();
            SaveService.SaveSummary previous = null;
            bool can = save != null && save.TryDescribePrevious(out previous);
            _saveCompare.text = "Current save: " + Describe(save != null ? save.DescribeCurrent() : null) + "\n"
                                + (can ? "Previous save: " + Describe(previous) : NoPreviousLine);
            RestoreButton.interactable = can;
            var look = RestoreButton.GetComponent<ThemedElement>();
            if (look != null) look.ShowUsable(can); // nothing to restore: the unusable look, and the line above says why
            RestackConditionalRows();
            LayoutNow();
        }

        /// <summary>Data Recovery: one question, with both saves side by side.</summary>
        public void AskRestore()
        {
            var save = Saves();
            if (save == null || !save.TryDescribePrevious(out var previous)) { RefreshSaveData(); return; }
            _dialogReturn = RestoreButton;
            string now = Describe(save.DescribeCurrent()), then = Describe(previous);
            Ask("Restore the previous save?\n\nCurrent save: " + now + "\nPrevious save: " + then
                + "\n\nThe current save is kept on this device as a backup file.",
                RestoreLabel, UITheme.ButtonKind.Normal, CancelLabel, () =>
                {
                    bool done = save.RestorePrevious();
                    AfterSaveReplaced(done, done ? "Restored the previous save: " + then + "."
                        : "The previous save could not be restored. Nothing was changed.");
                });
        }

        /// <summary>Delete Save: two questions (wiki: "confirm through all the pop-ups").</summary>
        public void AskDelete()
        {
            var save = Saves();
            if (save == null) return;
            _dialogReturn = DeleteSaveButton;
            string now = Describe(save.DescribeCurrent());
            Ask("Delete your save?\n\nYou lose: " + now + ", and everything you unlocked.\nYour options and controls stay as they are.",
                DeleteLabel, UITheme.ButtonKind.Danger, KeepLabel, () =>
                    Ask("Delete the save now?\n\nThis cannot be undone in the game.", DeleteLabel, UITheme.ButtonKind.Danger, KeepLabel, () =>
                    {
                        bool done = save.DeleteSave();
                        string file = done && !string.IsNullOrEmpty(save.LastDeletedBackupPath) ? System.IO.Path.GetFileName(save.LastDeletedBackupPath) : null;
                        AfterSaveReplaced(done, done
                            ? "Save deleted. Your options were kept." + (file != null ? " A copy of the old save stays on this device: " + file + "." : "")
                            : "The save could not be deleted. Nothing was changed.");
                    }));
        }

        private void AfterSaveReplaced(bool done, string status)
        {
            if (done)
            {
                SaveReplaced = true;
                _saveTabPresses = 0; // Delete Save hides again
                _rebindData?.Invoke(); // the options now read and write the save that is in memory
                GameSettings.NotifyChanged(); // sound and display follow the save's options
            }
            _saveStatus.text = status;
            RefreshAll();
            RefreshSaveData();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(RestoreButton.gameObject);
        }

        // ---------- the question box (Save Data) ----------

        private RectTransform _dialog, _dialogPanel, _dialogButtons;
        private TextMeshProUGUI _dialogText;
        private Button _dialogYes, _dialogNo;
        private Action _dialogYesAction;
        private Selectable _dialogReturn;
        private Vector2 _dialogFor = new Vector2(-1f, -1f);
        private const float DialogMaxWidth = 820f, DialogPad = 24f, DialogTextUnits = 26f;

        /// <summary>Asks a yes/no question over the screen. The "no" answer is chosen when it opens; Esc / B answers no.</summary>
        private void Ask(string message, string yesLabel, UITheme.ButtonKind yesKind, string noLabel, Action onYes)
        {
            EnsureDialog();
            _dialogText.text = message;
            SetLabel(_dialogYes, yesLabel);
            SetLabel(_dialogNo, noLabel);
            UITheme.ApplyButton(_dialogYes, _dialogYes.targetGraphic as Image, yesKind, UITheme.IconSlot.None, true);
            _dialogYesAction = onYes;
            _dialog.gameObject.SetActive(true);
            _dialog.SetAsLastSibling();
            LayoutDialog();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(_dialogNo.gameObject);
        }

        private static void SetLabel(Button button, string label)
        {
            var text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null) text.text = label;
        }

        private void EnsureDialog()
        {
            if (_dialog != null) return;
            _dialog = Rect("Dialog", transform, Vector2.zero, Vector2.one);
            // Its own veil, over the Options panel: nothing behind the question takes a click.
            var veil = Rect("DialogVeil", _dialog, new Vector2(-1f, -1f), new Vector2(2f, 2f)).gameObject.AddComponent<Image>();
            veil.raycastTarget = true;
            UITheme.ApplySurface(veil, ThemedElement.SurfaceRole.Veil);
            var safe = Rect("DialogContent", _dialog, Vector2.zero, Vector2.one);
            SafeAreaFitter.Fit(safe);
            var centre = new Vector2(0.5f, 0.5f);
            _dialogPanel = Rect("DialogPanel", safe, centre, centre);
            UITheme.ApplyPanel(_dialogPanel.gameObject.AddComponent<Image>(), 1f);
            _dialogText = Text("Message", _dialogPanel, Vector2.zero, Vector2.zero, "", DialogTextUnits, TextAlignmentOptions.TopLeft);
            _dialogText.enableAutoSizing = false;
            _dialogText.textWrappingMode = TextWrappingModes.Normal;
            _dialogButtons = Rect("DialogButtons", _dialogPanel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            _dialogButtons.pivot = new Vector2(0.5f, 0f);
            // The safe answer first (and chosen); the answer that changes the save second.
            _dialogNo = AddButton(_dialogButtons, centre, centre, CancelLabel, CloseDialog);
            _dialogNo.name = "Dialog_No";
            UITheme.ApplyButton(_dialogNo, _dialogNo.targetGraphic as Image, UITheme.ButtonKind.Primary, UITheme.IconSlot.None, true);
            _dialogYes = AddButton(_dialogButtons, centre, centre, RestoreLabel, ConfirmDialog);
            _dialogYes.name = "Dialog_Yes";
            _dialogButtons.gameObject.AddComponent<ThemedRow>();
            // Keyboard and controller stay on the two answers (the options behind the question are out of reach).
            _dialogNo.navigation = Between(_dialogYes);
            _dialogYes.navigation = Between(_dialogNo);
        }

        private static Navigation Between(Selectable other) => new Navigation
        {
            mode = Navigation.Mode.Explicit, selectOnLeft = other, selectOnRight = other, selectOnUp = other, selectOnDown = other
        };

        private void LayoutDialog()
        {
            if (_dialogPanel == null || _content == null) return;
            Vector2 room = _content.rect.size;
            _dialogFor = room;
            float gap = UiThemeAsset.Active.Layouts.ButtonGap;
            float width = Mathf.Max(1f, Mathf.Min(room.x - 2f * gap, DialogMaxWidth));
            _dialogPanel.anchorMin = _dialogPanel.anchorMax = _dialogPanel.pivot = new Vector2(0.5f, 0.5f);
            _dialogPanel.anchoredPosition = Vector2.zero;
            _dialogPanel.sizeDelta = new Vector2(width, _dialogPanel.sizeDelta.y); // the width first: the answers wrap to it
            var row = _dialogButtons.GetComponent<ThemedRow>();
            if (row != null) row.Layout();
            float rowHeight = _dialogButtons.sizeDelta.y * _dialogButtons.localScale.y;
            _dialogText.fontSize = Mathf.Max(DialogTextUnits, TextFloor);
            float textWidth = Mathf.Max(1f, width - 2f * DialogPad);
            float textHeight = Mathf.Ceil(_dialogText.GetPreferredValues(_dialogText.text, textWidth, 0f).y) + 2f;
            float height = Mathf.Min(Mathf.Max(1f, room.y - 2f * gap), DialogPad + textHeight + gap * 1.5f + rowHeight + DialogPad);
            _dialogPanel.sizeDelta = new Vector2(width, height);
            Place(_dialogText.rectTransform, DialogPad, DialogPad, textWidth, Mathf.Max(1f, height - 2f * DialogPad - rowHeight - gap * 1.5f));
            _dialogButtons.anchoredPosition = new Vector2(0f, DialogPad);
        }

        private void UpdateDialog()
        {
            bool back = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                        (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
            if (back) { CloseDialog(); return; }
            var system = EventSystem.current;
            if (system == null) return;
            var selected = system.currentSelectedGameObject;
            if (selected == null || !selected.transform.IsChildOf(_dialog)) system.SetSelectedGameObject(_dialogNo.gameObject);
        }

        private void ConfirmDialog()
        {
            var action = _dialogYesAction;
            CloseDialog();
            action?.Invoke();
        }

        /// <summary>Closes the question without doing anything and gives focus back to the button that asked.</summary>
        public void CloseDialog()
        {
            if (_dialog == null || !_dialog.gameObject.activeSelf) return;
            _dialog.gameObject.SetActive(false);
            _dialogYesAction = null;
            if (EventSystem.current != null && _dialogReturn != null && _dialogReturn.gameObject.activeInHierarchy)
                EventSystem.current.SetSelectedGameObject(_dialogReturn.gameObject);
        }

        // ---------- building helpers ----------

        /// <summary>A row with a surface and a label, added to its section; its controls are added by the caller.</summary>
        private OptionRow Line(RectTransform content, string label, RowKind kind)
        {
            var line = Rect(label, content, new Vector2(0f, 1f), new Vector2(1f, 1f));
            UITheme.ApplySurface(line.gameObject.AddComponent<Image>(), ThemedElement.SurfaceRole.Row);
            var row = new OptionRow { Line = line, Kind = kind };
            row.Label = Text("Label", line, Vector2.zero, Vector2.zero, label, 24f, TextAlignmentOptions.Left);
            AddRow(content, row); // so rows can close up around a hidden one
            return row;
        }

        private static Button AddButton(RectTransform parent, Vector2 min, Vector2 max, string label, Action onClick,
            ThemedElement.SizeRole size = ThemedElement.SizeRole.Standard)
        {
            var rect = Rect("Button", parent, min, max);
            var image = rect.gameObject.AddComponent<Image>();
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Text("Text", rect, Vector2.zero, Vector2.one, label, 24f, TextAlignmentOptions.Center); // the same size as the row labels
            // After the label exists, so the theme gives it its light-or-dark text and its place.
            UITheme.ApplyButton(button, image, label == "Back" ? UITheme.ButtonKind.Primary : UITheme.ButtonKind.Normal,
                label == "Back" ? UITheme.IconSlot.Back : UITheme.IconSlot.None, true, size);
            button.onClick.AddListener(() =>
            {
                var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
                if (audio != null) audio.PlaySFX(audio.UIClickSound);
                onClick();
            });
            return button;
        }


        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static TextMeshProUGUI Text(string name, Transform parent, Vector2 min, Vector2 max, string text, float size, TextAlignmentOptions align)
        {
            var label = Rect(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            UITheme.ApplyText(label, ThemedElement.TextRole.Body);
            label.enableAutoSizing = true;
            label.fontSizeMin = TextFloor;
            label.fontSizeMax = Mathf.Max(size, TextFloor);
            label.alignment = align;
            label.raycastTarget = false;
            return label;
        }
    }
}
