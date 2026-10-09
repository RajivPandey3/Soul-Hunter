using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;
using SoulHunter.Core.Persistence;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// On-screen touch controls (VS "Visible Joysticks" / mobile control stick): a movement stick bottom-left
    /// plus Dash and Pause buttons bottom-right. They drive a virtual gamepad through the Input System's
    /// OnScreenStick/OnScreenButton, so InputService's gamepad bindings (left stick, A, Start) handle them.
    /// Always shown on mobile; on desktop only with the Visible Joystick option. The HUD adds this to itself.
    /// VS Options, Gameplay, "Joystick Type" (Legacy, Default): Legacy is the fixed stick bottom-left; Default is a floating
    /// stick that jumps to where the thumb lands in the left part of the screen (FloatingStickZone, which records why).
    /// </summary>
    public sealed class TouchControlsUI : MonoBehaviour
    {
        private GameObject _root;
        private RectTransform _stickBase;
        private Image _baseImage, _knobImage, _zoneImage;
        private FloatingStickZone _zone;

        public GameObject Root => _root;
        public RectTransform StickBase => _stickBase;
        public FloatingStickZone Zone => _zone;
        /// <summary>True with Joystick Type Default (the floating stick), false with Legacy (the fixed stick).</summary>
        public bool IsFloating => GameSettings.JoystickType == GameSettings.JoystickDefault;
        public static bool ShouldShow => Application.isMobilePlatform || GameSettings.VisibleJoystick;

        private void Start()
        {
            Build();
            GameSettings.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy() => GameSettings.Changed -= Refresh;

        private void Refresh()
        {
            if (_root == null) return;
            _root.SetActive(ShouldShow);
            bool floating = IsFloating;
            if (!floating) _zone.Release();
            _zone.gameObject.SetActive(floating);
            // Legacy: the stick itself takes the press. Default: the area does, and moves the stick under the thumb.
            _baseImage.raycastTarget = _knobImage.raycastTarget = !floating;
        }

        private void Update()
        {
            // A menu drawn on this canvas (pause) must get the clicks the floating stick's area would catch.
            if (_zoneImage != null) _zoneImage.raycastTarget = !SoulHunter.Core.Services.GameTime.IsPaused;
        }

        private void Build()
        {
            _root = new GameObject("TouchControls", typeof(RectTransform));
            var root = (RectTransform)_root.transform;
            root.SetParent(HudReadability.CanvasRoot(transform), false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            _root.SetActive(false); // on-screen controls register their virtual device on enable

            // Floating stick area (Joystick Type Default): the left part of the screen under the HUD's top labels, behind
            // the stick, clear of Dash and Pause on the right. Invisible; only the stick is drawn.
            var zone = new GameObject("StickZone", typeof(RectTransform));
            var zoneRect = (RectTransform)zone.transform;
            zoneRect.SetParent(root, false);
            zoneRect.anchorMin = Vector2.zero;
            zoneRect.anchorMax = new Vector2(0.45f, 0.75f);
            zoneRect.offsetMin = zoneRect.offsetMax = Vector2.zero;
            _zoneImage = zone.AddComponent<Image>();
            _zoneImage.color = Color.clear; // catches presses, draws nothing

            // Movement stick (its resting place; Legacy keeps it here).
            var stickBase = Pad("StickBase", root, new Vector2(0.04f, 0.06f), new Vector2(0.2f, 0.34f), ThemedElement.SurfaceRole.TouchPad);
            var knob = Pad("StickKnob", stickBase, new Vector2(0.3f, 0.3f), new Vector2(0.7f, 0.7f), ThemedElement.SurfaceRole.TouchKnob);
            var stick = knob.gameObject.AddComponent<OnScreenStick>();
            stick.controlPath = "<Gamepad>/leftStick";
            stick.movementRange = 90f;
            _stickBase = stickBase;
            _baseImage = stickBase.GetComponent<Image>();
            _knobImage = knob.GetComponent<Image>();
            _zone = zone.AddComponent<FloatingStickZone>();
            _zone.Init(stickBase, stick);

            // Dash and Pause.
            TouchButton(root, "Dash", new Vector2(0.84f, 0.08f), new Vector2(0.96f, 0.26f), "<Gamepad>/buttonSouth");
            TouchButton(root, "Pause", new Vector2(0.905f, 0.7f), new Vector2(0.975f, 0.82f), "<Gamepad>/start"); // below the timer
        }

        private static RectTransform Pad(string name, Transform parent, Vector2 min, Vector2 max, ThemedElement.SurfaceRole role)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = go.AddComponent<Image>();
            UITheme.ApplySurface(image, role); // Theme Studio > HUD > touch controls
            return rect;
        }

        private static void TouchButton(RectTransform parent, string label, Vector2 min, Vector2 max, string controlPath)
        {
            var rect = Pad(label + "Button", parent, min, max, ThemedElement.SurfaceRole.TouchPad); // dark pad: the label reads on any floor
            rect.gameObject.AddComponent<OnScreenButton>().controlPath = controlPath;
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
            text.fontSizeMax = 30f;
            text.raycastTarget = false;
            UITheme.ApplyText(text, ThemedElement.TextRole.HudLabel);
        }
    }
}
