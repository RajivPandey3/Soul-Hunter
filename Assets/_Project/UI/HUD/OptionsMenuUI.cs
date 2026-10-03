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
    /// VS-style Options screen (sections as on the VS wiki options page), opened from the main menu and the
    /// pause menu. Sound: Sounds, Music. Display: Window Mode, Resolution, V-Sync, Flashing VFX, Disable Blood,
    /// Screen Shake. Gameplay: Damage Numbers, Show Stage Progression, Visible Joystick, Pause When Unfocused.
    /// Controls: keyboard and controller remapping. Fully usable with keyboard or controller; Esc / B closes it.
    /// Changes apply at once and are saved when the screen closes. Built at runtime on its own overlay canvas.
    /// </summary>
    public sealed class OptionsMenuUI : MonoBehaviour
    {
        public static OptionsMenuUI Current { get; private set; }

        private static readonly Color PanelColor = UITheme.Panel;
        private static readonly Color RowColor = UITheme.Row;
        private static readonly Color AccentColor = UITheme.Accent;

        private readonly List<Action> _refreshers = new List<Action>();
        private readonly List<Selectable> _selectables = new List<Selectable>();
        private Action _onClosed;
        private InputService _input;
        private InputActionRebindingExtensions.RebindingOperation _rebinding;
        private bool _pauseActionWasEnabled;

        public IReadOnlyList<Selectable> Selectables => _selectables;
        public bool IsRebinding => _rebinding != null;

        /// <summary>Opens the Options screen on top of everything; onClosed runs after it closes.</summary>
        public static OptionsMenuUI Open(Action onClosed = null)
        {
            if (Current != null) return Current;
            var root = new GameObject("OptionsMenu", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            var options = root.AddComponent<OptionsMenuUI>();
            options._onClosed = onClosed;
            options.Build();
            return options;
        }

        private void Awake() => Current = this;

        private void OnDestroy()
        {
            if (Current == this) Current = null;
            CancelRebinding();
            RestorePauseAction();
        }

        private void Update()
        {
            if (_rebinding != null) return;
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
        }

        /// <summary>Saves the options (including remapped controls) and closes the screen.</summary>
        public void Close()
        {
            var data = GameSettings.Data;
            if (data != null && _input != null) data.InputBindingOverrides = _input.SaveBindingOverrides();
            GameSettings.Save();
            var onClosed = _onClosed;
            Destroy(gameObject);
            onClosed?.Invoke();
        }

        // ---------- build ----------

        private void Build()
        {
            if (GameServices.Instance != null) GameServices.Instance.TryGet(out _input);
            // Esc toggles pause in a run; while Options is open it closes Options instead.
            var pause = _input?.Actions?.FindAction(InputService.PauseAction);
            if (pause != null) { _pauseActionWasEnabled = pause.enabled; pause.Disable(); }

            UITheme.AddVeil(transform); // the menu or game behind stays dark
            var panel = Rect("Panel", transform, new Vector2(0.18f, 0.05f), new Vector2(0.82f, 0.95f));
            UITheme.ApplyPanel(panel.gameObject.AddComponent<Image>(), PanelColor.a);
            Text("Title", panel, new Vector2(0f, 0.92f), new Vector2(1f, 0.99f), "Options", 44f, TextAlignmentOptions.Center).color = AccentColor;

            var data = GameSettings.Data;
            // VS groups its options in sections; one tab per section.
            string[] sections = { "Sound", "Display", "Gameplay", "Controls" };
            for (int i = 0; i < sections.Length; i++)
            {
                int index = i;
                var tab = AddButton(panel, new Vector2(0.05f + i * 0.2275f, 0.83f), new Vector2(0.255f + i * 0.2275f, 0.9f), sections[i], () => ShowTab(index));
                tab.name = "Tab_" + sections[i];
                _tabButtons.Add(tab);
            }
            const float row = 0.1f;

            // Sound
            var sound = BeginTab("Sound", panel);
            float y = 1f;
            AddSlider(sound, ref y, row, "Sounds", () => GameSettings.SfxVolume, v => { if (data != null) data.SfxVolume = v; });
            AddSlider(sound, ref y, row, "Music", () => GameSettings.MusicVolume, v => { if (data != null) data.MusicVolume = v; });

            // Display
            var display = BeginTab("Display", panel);
            y = 1f;
            AddCycle(display, ref y, row, "Window Mode", () => GameSettings.WindowMode, () => SoulHunter.Gameplay.Core.DisplaySettingsApplier.WindowModeNames.Length,
                i => SoulHunter.Gameplay.Core.DisplaySettingsApplier.WindowModeNames[i], i => { if (data != null) data.WindowMode = i; });
            // Resolution cycles Native, then every resolution the display supports.
            AddCycle(display, ref y, row, "Resolution", () => GameSettings.ResolutionIndex + 1, () => SoulHunter.Gameplay.Core.DisplaySettingsApplier.Resolutions().Count + 1,
                i => SoulHunter.Gameplay.Core.DisplaySettingsApplier.ResolutionLabel(i - 1), i => { if (data != null) data.ResolutionIndex = i - 1; });
            AddToggle(display, ref y, row, "V-Sync", () => GameSettings.VSync, v => { if (data != null) data.VSync = v; });
            AddToggle(display, ref y, row, "Flashing VFX", () => GameSettings.FlashingVfx, v => { if (data != null) data.FlashingVfx = v; });
            AddToggle(display, ref y, row, "Disable Blood", () => GameSettings.DisableBlood, v => { if (data != null) data.DisableBlood = v; });
            AddToggle(display, ref y, row, "Screen Shake", () => GameSettings.ScreenShake, v => { if (data != null) data.ScreenShake = v; });
            // VS Options, Display: the green look of Defanged enemies (Defang PowerUp).
            AddToggle(display, ref y, row, "Show Defanged Enemies", () => GameSettings.ShowDefangedEnemies, v => { if (data != null) data.ShowDefangedEnemies = v; });

            // Gameplay
            var gameplay = BeginTab("Gameplay", panel);
            y = 1f;
            AddToggle(gameplay, ref y, row, "Damage Numbers", () => GameSettings.DamageNumbers, v => { if (data != null) data.DamageNumbers = v; });
            AddToggle(gameplay, ref y, row, "Show Stage Progression", () => GameSettings.ShowStageProgression, v => { if (data != null) data.ShowStageProgression = v; });
            AddToggle(gameplay, ref y, row, "Visible Joystick", () => GameSettings.VisibleJoystick, v => { if (data != null) data.VisibleJoystick = v; });
            AddToggle(gameplay, ref y, row, "Pause When Unfocused", () => GameSettings.PauseOnFocusLoss, v => { if (data != null) data.PauseOnFocusLoss = v; });

            // Controls: headers over the binding columns (0.3-0.6 and 0.63-0.93 of a 0.02-0.98 line).
            var controls = BeginTab("Controls", panel);
            const float controlRow = 0.095f;
            y = 1f;
            Text("ControlsTitle", controls, new Vector2(0.02f, y - controlRow), new Vector2(0.3f, y), "Controls", 26f, TextAlignmentOptions.Left).color = AccentColor;
            Text("KeyboardTitle", controls, new Vector2(0.308f, y - controlRow), new Vector2(0.596f, y), "Keyboard", 26f, TextAlignmentOptions.Center).color = AccentColor;
            Text("ControllerTitle", controls, new Vector2(0.625f, y - controlRow), new Vector2(0.913f, y), "Controller", 26f, TextAlignmentOptions.Center).color = AccentColor;
            y -= controlRow;
            if (_input != null && _input.Actions != null)
            {
                var move = _input.Actions.FindAction(InputService.MoveAction);
                foreach (var part in new[] { "up", "down", "left", "right" })
                    AddBindingRow(controls, ref y, controlRow, "Move " + char.ToUpper(part[0]) + part.Substring(1), move, CompositePart(move, part), -1);
                foreach (var name in new[] { InputService.DashAction, InputService.UltimateAction, InputService.PauseAction })
                {
                    var action = _input.Actions.FindAction(name);
                    AddBindingRow(controls, ref y, controlRow, name, action, BindingIndex(action, "<Keyboard>"), BindingIndex(action, "<Gamepad>"));
                }
            }
            var reset = AddButton(controls, new Vector2(0.3f, y - controlRow), new Vector2(0.7f, y - 0.01f), "Reset Controls", () =>
            {
                _input?.ResetBindings();
                RefreshAll();
            });
            reset.name = "Reset Controls";
            _building.Add(reset);
            _selectables.Add(reset);

            var back = AddButton(panel, new Vector2(0.35f, 0.03f), new Vector2(0.65f, 0.1f), "Back", Close);
            back.name = "Back";
            _selectables.Add(back);

            RefreshAll();
            ShowTab(0);
        }

        // ---------- sections ----------

        private readonly List<RectTransform> _tabs = new List<RectTransform>();
        private readonly List<List<Selectable>> _tabItems = new List<List<Selectable>>();
        private readonly List<Button> _tabButtons = new List<Button>();
        private List<Selectable> _building = new List<Selectable>();
        private int _currentTab;

        public int CurrentTab => _currentTab;
        public IReadOnlyList<Button> TabButtons => _tabButtons;

        private RectTransform BeginTab(string name, RectTransform panel)
        {
            var tab = Rect(name, panel, new Vector2(0.03f, 0.13f), new Vector2(0.97f, 0.81f));
            _tabs.Add(tab);
            _building = new List<Selectable>();
            _tabItems.Add(_building);
            return tab;
        }

        /// <summary>Shows one section and focuses its first control (keyboard/controller friendly).</summary>
        public void ShowTab(int index)
        {
            _currentTab = Mathf.Clamp(index, 0, _tabs.Count - 1);
            for (int i = 0; i < _tabs.Count; i++) _tabs[i].gameObject.SetActive(i == _currentTab);
            for (int i = 0; i < _tabButtons.Count; i++)
                _tabButtons[i].GetComponentInChildren<TextMeshProUGUI>().color = i == _currentTab ? AccentColor : UITheme.Text;
            var first = _tabItems[_currentTab].Count > 0 ? _tabItems[_currentTab][0] : null;
            if (first != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
        }

        private void RefreshAll()
        {
            foreach (var refresh in _refreshers) refresh();
        }

        private void AddSlider(RectTransform panel, ref float y, float row, string label, Func<float> get, Action<float> set)
        {
            var line = Line(panel, ref y, row, label);
            var sliderRect = Rect("Slider", line, new Vector2(0.45f, 0.3f), new Vector2(0.95f, 0.7f));
            var background = sliderRect.gameObject.AddComponent<Image>();
            background.color = UITheme.Row;
            var fillArea = Rect("Fill Area", sliderRect, Vector2.zero, Vector2.one);
            var fill = Rect("Fill", fillArea, Vector2.zero, Vector2.one);
            fill.gameObject.AddComponent<Image>().color = AccentColor;
            var handleArea = Rect("Handle Area", sliderRect, Vector2.zero, Vector2.one);
            var handle = Rect("Handle", handleArea, new Vector2(0f, -0.4f), new Vector2(0f, 1.4f));
            handle.sizeDelta = new Vector2(18f, 0f);
            var handleImage = handle.gameObject.AddComponent<Image>();
            var slider = sliderRect.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            ApplyFocusColors(slider, handleImage);
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(get());
            var value = Text("Value", line, new Vector2(0.96f, 0f), new Vector2(1.1f, 1f), "", 22f, TextAlignmentOptions.Left);
            slider.onValueChanged.AddListener(v =>
            {
                set(v);
                value.text = Mathf.RoundToInt(v * 100f) + "%";
                GameSettings.NotifyChanged();
            });
            _refreshers.Add(() => { slider.SetValueWithoutNotify(get()); value.text = Mathf.RoundToInt(get() * 100f) + "%"; });
            _selectables.Add(slider);
            _building.Add(slider);
        }

        private void AddToggle(RectTransform panel, ref float y, float row, string label, Func<bool> get, Action<bool> set)
        {
            var line = Line(panel, ref y, row, label);
            TextMeshProUGUI state = null;
            var button = AddButton(line, new Vector2(0.45f, 0.08f), new Vector2(0.65f, 0.92f), "", () =>
            {
                set(!get());
                GameSettings.NotifyChanged();
                RefreshAll();
            });
            state = button.GetComponentInChildren<TextMeshProUGUI>();
            // Text as well as colour, so the state never depends on colour alone.
            _refreshers.Add(() => { state.text = get() ? "ON" : "OFF"; state.color = get() ? AccentColor : UITheme.Text; });
            button.name = label;
            _selectables.Add(button);
            _building.Add(button);
        }

        /// <summary>A button that steps through a list of choices (VS Window Mode, Resolution).</summary>
        private void AddCycle(RectTransform panel, ref float y, float row, string label, Func<int> get, Func<int> count,
            Func<int, string> labelFor, Action<int> set)
        {
            var line = Line(panel, ref y, row, label);
            var button = AddButton(line, new Vector2(0.45f, 0.08f), new Vector2(0.95f, 0.92f), "", () =>
            {
                int n = Mathf.Max(1, count());
                set((Mathf.Clamp(get(), 0, n - 1) + 1) % n);
                GameSettings.NotifyChanged();
                RefreshAll();
            });
            var text = button.GetComponentInChildren<TextMeshProUGUI>();
            _refreshers.Add(() => text.text = labelFor(Mathf.Clamp(get(), 0, Mathf.Max(1, count()) - 1)));
            button.name = label;
            _selectables.Add(button);
            _building.Add(button);
        }

        private void AddBindingRow(RectTransform panel, ref float y, float row, string label, InputAction action, int keyboardIndex, int gamepadIndex)
        {
            if (action == null) return;
            var line = Line(panel, ref y, row, label);
            AddBindingButton(line, new Vector2(0.3f, 0.08f), new Vector2(0.6f, 0.92f), action, keyboardIndex, "<Keyboard>");
            if (gamepadIndex >= 0) AddBindingButton(line, new Vector2(0.63f, 0.08f), new Vector2(0.93f, 0.92f), action, gamepadIndex, "<Gamepad>");
            else Text("Fixed", line, new Vector2(0.63f, 0f), new Vector2(0.93f, 1f), "Left stick / D-pad", 20f, TextAlignmentOptions.Center);
        }

        private void AddBindingButton(RectTransform line, Vector2 min, Vector2 max, InputAction action, int index, string device)
        {
            if (index < 0) return;
            Button button = null;
            button = AddButton(line, min, max, "", () => StartRebinding(action, index, device, button));
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            _refreshers.Add(() => label.text = action.GetBindingDisplayString(index));
            button.name = $"{action.name}_{device}";
            _selectables.Add(button);
            _building.Add(button);
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

        // ---------- layout helpers ----------

        private RectTransform Line(RectTransform panel, ref float y, float row, string label)
        {
            var line = Rect(label, panel, new Vector2(0.02f, y - row), new Vector2(0.98f, y));
            line.gameObject.AddComponent<Image>().color = RowColor;
            Text("Label", line, new Vector2(0.02f, 0f), new Vector2(0.44f, 1f), label, 24f, TextAlignmentOptions.Left);
            y -= row * 1.08f;
            return line;
        }

        private static Button AddButton(RectTransform parent, Vector2 min, Vector2 max, string label, Action onClick)
        {
            var rect = Rect("Button", parent, min, max);
            var image = rect.gameObject.AddComponent<Image>();
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ApplyFocusColors(button, image);
            button.onClick.AddListener(() =>
            {
                var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
                if (audio != null) audio.PlaySFX(audio.UIClickSound);
                onClick();
            });
            Text("Text", rect, Vector2.zero, Vector2.one, label, 22f, TextAlignmentOptions.Center);
            return button;
        }

        /// <summary>
        /// ColorTint multiplies the graphic colour, so a dark graphic hides the focus tint. The graphic stays white
        /// and the state colours carry the look, making keyboard/controller focus clearly visible (orange).
        /// </summary>
        public static void ApplyFocusColors(Selectable selectable, Image graphic) => UITheme.ApplyButton(selectable, graphic);

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
            label.color = UITheme.Text;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = size;
            label.alignment = align;
            label.raycastTarget = false;
            return label;
        }
    }
}
