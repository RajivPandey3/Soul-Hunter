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
    /// </summary>
    public sealed class TouchControlsUI : MonoBehaviour
    {
        private GameObject _root;

        public GameObject Root => _root;
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
            if (_root != null) _root.SetActive(ShouldShow);
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

            // Movement stick.
            var stickBase = Pad("StickBase", root, new Vector2(0.04f, 0.06f), new Vector2(0.2f, 0.34f), new Color(0f, 0f, 0f, 0.3f));
            var knob = Pad("StickKnob", stickBase, new Vector2(0.3f, 0.3f), new Vector2(0.7f, 0.7f), new Color(1f, 1f, 1f, 0.6f));
            var stick = knob.gameObject.AddComponent<OnScreenStick>();
            stick.controlPath = "<Gamepad>/leftStick";
            stick.movementRange = 90f;

            // Dash and Pause.
            TouchButton(root, "Dash", new Vector2(0.84f, 0.08f), new Vector2(0.96f, 0.26f), "<Gamepad>/buttonSouth");
            TouchButton(root, "Pause", new Vector2(0.905f, 0.7f), new Vector2(0.975f, 0.82f), "<Gamepad>/start"); // below the timer
        }

        private static RectTransform Pad(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = go.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        private static void TouchButton(RectTransform parent, string label, Vector2 min, Vector2 max, string controlPath)
        {
            var rect = Pad(label + "Button", parent, min, max, new Color(0f, 0f, 0f, 0.4f)); // dark pad: white label reads on any floor
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
            text.color = Color.white;
        }
    }
}
