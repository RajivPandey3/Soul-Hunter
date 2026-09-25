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
    /// VS-style Options screen, opened from the main menu and the pause menu: Sounds and Music volume,
    /// Flashing VFX, Damage Numbers, Screen Shake, Fullscreen, Pause When Unfocused, and control remapping
    /// for keyboard and controller. Fully usable with keyboard or controller; Esc / B closes it. Changes
    /// apply at once and are saved when the screen closes. Built at runtime on its own overlay canvas.
    /// </summary>
    public sealed class OptionsMenuUI : MonoBehaviour
    {
        public static OptionsMenuUI Current { get; private set; }

        private static readonly Color PanelColor = new Color(0.08f, 0.08f, 0.13f, 1f);
        private static readonly Color RowColor = new Color(0.22f, 0.24f, 0.34f, 1f);
        private static readonly Color AccentColor = new Color(1f, 0.85f, 0.25f, 1f);

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
            // Keep keyboard/controller focus inside the screen.
            if (EventSystem.current != null && _selectables.Count > 0 &&
                (EventSystem.current.currentSelectedGameObject == null ||
                 !EventSystem.current.currentSelectedGameObject.transform.IsChildOf(transform)))
                EventSystem.current.SetSelectedGameObject(_selectables[0].gameObject);
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

            var panel = Rect("Panel", transform, new Vector2(0.18f, 0.05f), new Vector2(0.82f, 0.95f));
            panel.gameObject.AddComponent<Image>().color = PanelColor;
            Text("Title", panel, new Vector2(0f, 0.92f), new Vector2(1f, 0.99f), "Options", 44f, TextAlignmentOptions.Center).color = AccentColor;

            var data = GameSettings.Data;
            float y = 0.9f;
            const float row = 0.044f; // 7 options + controls title + 7 control rows + buttons fit the panel
            AddSlider(panel, ref y, row, "Sounds", () => GameSettings.SfxVolume, v => { if (data != null) data.SfxVolume = v; });
            AddSlider(panel, ref y, row, "Music", () => GameSettings.MusicVolume, v => { if (data != null) data.MusicVolume = v; });
            AddToggle(panel, ref y, row, "Flashing VFX", () => GameSettings.FlashingVfx, v => { if (data != null) data.FlashingVfx = v; });
            AddToggle(panel, ref y, row, "Damage Numbers", () => GameSettings.DamageNumbers, v => { if (data != null) data.DamageNumbers = v; });
            AddToggle(panel, ref y, row, "Screen Shake", () => GameSettings.ScreenShake, v => { if (data != null) data.ScreenShake = v; });
            AddToggle(panel, ref y, row, "Fullscreen", () => GameSettings.Fullscreen, v => { if (data != null) data.Fullscreen = v; });
            AddToggle(panel, ref y, row, "Pause When Unfocused", () => GameSettings.PauseOnFocusLoss, v => { if (data != null) data.PauseOnFocusLoss = v; });

            y -= row * 0.4f;
            // Column headers sit over the binding columns (row columns 0.3-0.6 and 0.63-0.93 of a 0.05-0.95 line).
            Text("ControlsTitle", panel, new Vector2(0.05f, y - row), new Vector2(0.32f, y), "Controls", 26f, TextAlignmentOptions.Left).color = AccentColor;
            Text("KeyboardTitle", panel, new Vector2(0.32f, y - row), new Vector2(0.59f, y), "Keyboard", 26f, TextAlignmentOptions.Center).color = AccentColor;
            Text("ControllerTitle", panel, new Vector2(0.617f, y - row), new Vector2(0.887f, y), "Controller", 26f, TextAlignmentOptions.Center).color = AccentColor;
            y -= row;
            if (_input != null && _input.Actions != null)
            {
                var move = _input.Actions.FindAction(InputService.MoveAction);
                foreach (var part in new[] { "up", "down", "left", "right" })
                    AddBindingRow(panel, ref y, row, "Move " + char.ToUpper(part[0]) + part.Substring(1), move, CompositePart(move, part), -1);
                foreach (var name in new[] { InputService.DashAction, InputService.UltimateAction, InputService.PauseAction })
                {
                    var action = _input.Actions.FindAction(name);
                    AddBindingRow(panel, ref y, row, name, action, BindingIndex(action, "<Keyboard>"), BindingIndex(action, "<Gamepad>"));
                }
            }

            y -= row * 0.3f;
            var reset = AddButton(panel, new Vector2(0.1f, y - row), new Vector2(0.45f, y), "Reset Controls", () =>
            {
                _input?.ResetBindings();
                RefreshAll();
            });
            var back = AddButton(panel, new Vector2(0.55f, y - row), new Vector2(0.9f, y), "Back", Close);
            _selectables.Add(reset);
            _selectables.Add(back);

            RefreshAll();
            if (EventSystem.current != null && _selectables.Count > 0) EventSystem.current.SetSelectedGameObject(_selectables[0].gameObject);
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
            background.color = new Color(0.12f, 0.12f, 0.18f, 1f);
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
            _refreshers.Add(() => { state.text = get() ? "ON" : "OFF"; state.color = get() ? AccentColor : Color.white; });
            button.name = label;
            _selectables.Add(button);
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
            var line = Rect(label, panel, new Vector2(0.05f, y - row), new Vector2(0.95f, y));
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
        public static void ApplyFocusColors(Selectable selectable, Image graphic)
        {
            graphic.color = Color.white;
            var colors = selectable.colors;
            colors.normalColor = new Color(0.14f, 0.16f, 0.26f, 1f);
            colors.highlightedColor = new Color(0.3f, 0.34f, 0.52f, 1f);
            colors.selectedColor = new Color(0.85f, 0.45f, 0.08f, 1f);
            colors.pressedColor = new Color(1f, 0.7f, 0.2f, 1f);
            colors.colorMultiplier = 1f;
            selectable.colors = colors;
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
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = size;
            label.alignment = align;
            label.raycastTarget = false;
            return label;
        }
    }
}
