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
            UiScreen.Changed += PlaceZone;
            GameSettings.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            GameSettings.Changed -= Refresh;
            UiScreen.Changed -= PlaceZone;
            UiThemeAsset.Changed -= TintPauseSymbol;
            UiThemeAsset.Changed -= TintPlate;
        }

        private RectTransform _zoneRect;
        private readonly System.Collections.Generic.List<Image> _pauseBars = new System.Collections.Generic.List<Image>();

        /// <summary>The floating stick's touch area (tests).</summary>
        public RectTransform ZoneRect => _zoneRect;
        /// <summary>The two bars of the Pause symbol (tests).</summary>
        public System.Collections.Generic.IReadOnlyList<Image> PauseBars => _pauseBars;

        /// <summary>
        /// Where a thumb may land for the floating stick. A phone held sideways: the left part of the screen, as before.
        /// A phone held upright (owner, 2026-10-12: "portrait mode mein joystick ko jahan par ungli rakhe wahin par aa
        /// jaye"): the whole width under the HUD strip, because one hand holds and plays there. Dash and Pause are drawn
        /// after the area, so their own presses still reach them. The wiki does not describe the VS mobile stick's area.
        /// </summary>
        private void PlaceZone()
        {
            if (_zoneRect == null) return;
            _zoneRect.anchorMin = Vector2.zero;
            _zoneRect.anchorMax = UiScreen.IsPortrait ? new Vector2(1f, 0.82f) : new Vector2(0.45f, 0.75f);
            _zoneRect.offsetMin = _zoneRect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Owner (2026-10-12: "pause ko symbolic banao"): the Pause pad shows the two-bar pause symbol, not the word. The
        /// bars take the HUD label colour from the theme.
        /// </summary>
        private void MakePauseASymbol(RectTransform pause)
        {
            var word = pause.GetComponentInChildren<TextMeshProUGUI>();
            if (word != null) word.gameObject.SetActive(false);
            foreach (float x in new[] { 0.31f, 0.56f })
            {
                var bar = new GameObject("PauseBar", typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)bar.transform;
                rect.SetParent(pause, false);
                rect.anchorMin = new Vector2(x, 0.26f);
                rect.anchorMax = new Vector2(x + 0.13f, 0.74f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                var image = bar.GetComponent<Image>();
                image.raycastTarget = false;
                _pauseBars.Add(image);
            }
            TintPauseSymbol();
            UiThemeAsset.Changed += TintPauseSymbol;
        }

        private void TintPauseSymbol()
        {
            foreach (var bar in _pauseBars)
                if (bar != null) bar.color = UITheme.Hud.LabelText;
        }

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
            bool paused = SoulHunter.Core.Services.GameTime.IsPaused;
            if (_zoneImage != null) _zoneImage.raycastTarget = !paused;
            // While a menu has the game paused the pads are hidden: on a phone they were drawn over the pause screen
            // (2026-10-10). They come back when play resumes.
            if (_root != null)
            {
                bool show = ShouldShow && !paused;
                if (_root.activeSelf != show) _root.SetActive(show);
            }
        }

        private void Build()
        {
            _root = new GameObject("TouchControls", typeof(RectTransform));
            var root = (RectTransform)_root.transform;
            root.SetParent(HudReadability.CanvasRoot(transform), false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            SafeAreaFitter.Fit(root); // clear of the notch and the gesture bar, in landscape and portrait
            _root.SetActive(false); // on-screen controls register their virtual device on enable

            // Floating stick area (Joystick Type Default): the left part of the screen under the HUD's top labels, behind
            // the stick, clear of Dash and Pause on the right. Invisible; only the stick is drawn.
            var zone = new GameObject("StickZone", typeof(RectTransform));
            var zoneRect = (RectTransform)zone.transform;
            zoneRect.SetParent(root, false);
            _zoneRect = zoneRect;
            PlaceZone();
            _zoneImage = zone.AddComponent<Image>();
            _zoneImage.color = Color.clear; // catches presses, draws nothing

            // Responsive (owner, 2026-10-10: landscape and portrait): the pads keep one size in canvas units, placed from the
            // safe area's corners, so they stay round and the same size whichever way the phone is held. Before, they were
            // screen fractions and stretched into tall bars in portrait. Sizes are well over the 48 dp touch target.
            // Movement stick (its resting place; Legacy keeps it here).
            // Owner (2026-10-12: "square ko round kar do ... joystick ka aakar ekdam chhota kar do"): the stick's square pad
            // is not drawn. In its place is a round see-through plate the size the knob used to be, and the knob on it is
            // a small dot. The square still takes the Legacy stick's press and keeps other HUD boxes clear of it.
            var stickBase = Pad("StickBase", root, Vector2.zero, Vector2.zero, ThemedElement.SurfaceRole.TouchPad);
            Destroy(stickBase.GetComponent<ThemedElement>());
            stickBase.GetComponent<Image>().color = Color.clear;
            // A little smaller on a short screen (a phone held sideways is 510 units high), clear of the item slots.
            Corner(stickBase, new Vector2(0f, 0f), Mathf.Min(StickSize, Mathf.Max(UiScreen.MinTouchUnits * 2f, UiScreen.CanvasUnits.y * 0.36f)), new Vector2(EdgeMargin, EdgeMargin));
            float plateFrom = 0.5f - PlateShare * 0.5f, dotFrom = 0.5f - DotShare * 0.5f;
            // The theme's round picture in the touch pad's see-through colour.
            _plate = Pad("StickPlate", stickBase, new Vector2(plateFrom, plateFrom), new Vector2(1f - plateFrom, 1f - plateFrom), ThemedElement.SurfaceRole.Knob).GetComponent<Image>();
            Destroy(_plate.GetComponent<ThemedElement>());
            _plate.raycastTarget = false;
            // The knob keeps its touch size (Legacy presses it); only the small dot on it is drawn.
            var knob = Pad("StickKnob", stickBase, new Vector2(0.3f, 0.3f), new Vector2(0.7f, 0.7f), ThemedElement.SurfaceRole.Knob);
            Destroy(knob.GetComponent<ThemedElement>());
            knob.GetComponent<Image>().color = Color.clear;
            float inKnob = (dotFrom - 0.3f) / 0.4f;
            _dot = Pad("StickDot", knob, new Vector2(inKnob, inKnob), new Vector2(1f - inKnob, 1f - inKnob), ThemedElement.SurfaceRole.Knob).GetComponent<Image>();
            _dot.raycastTarget = false;
            TintPlate();
            UiThemeAsset.Changed += TintPlate;
            var stick = knob.gameObject.AddComponent<OnScreenStick>();
            stick.controlPath = "<Gamepad>/leftStick";
            // The dot travels to the plate's rim.
            stick.movementRange = stickBase.sizeDelta.x * (PlateShare - DotShare) * 0.5f;
            _stickBase = stickBase;
            _baseImage = stickBase.GetComponent<Image>();
            _knobImage = knob.GetComponent<Image>();
            _zone = zone.AddComponent<FloatingStickZone>();
            _zone.Init(stickBase, stick);

            // Dash and Pause.
            Corner(TouchButton(root, "Dash", "<Gamepad>/buttonSouth"), new Vector2(1f, 0f), DashSize, new Vector2(-EdgeMargin, EdgeMargin * 1.4f));
            // Below the timer at the top right.
            _pause = TouchButton(root, "Pause", "<Gamepad>/start");
            MakePauseASymbol(_pause);
            Corner(_pause, new Vector2(1f, 1f), PauseSize, new Vector2(-EdgeMargin, -PauseFromTop));
            if (_pausePlace.HasValue) PlacePause(_pausePlace.Value.x, _pausePlace.Value.y, _pausePlace.Value.z);
        }

        private RectTransform _pause;
        private Vector3? _pausePlace;
        private Image _plate, _dot;
        private readonly System.Collections.Generic.List<Image> _roundPads = new System.Collections.Generic.List<Image>();
        /// <summary>The round Dash and Pause pads (tests).</summary>
        public System.Collections.Generic.IReadOnlyList<Image> RoundPads => _roundPads;

        /// <summary>The round see-through plate under the stick (tests).</summary>
        public Image Plate => _plate;
        /// <summary>The small knob that is drawn (tests).</summary>
        public Image Dot => _dot;

        /// <summary>The plate's and the knob dot's width, as shares of the stick's size.</summary>
        public const float PlateShare = 0.4f, DotShare = 0.14f;

        private void TintPlate()
        {
            // Owner (2026-10-12: "transparent ka colour white kar dena"): white, as see-through as the theme's touch pads.
            if (_plate != null) _plate.color = new Color(1f, 1f, 1f, UITheme.Hud.TouchPad.a);
            foreach (var pad in _roundPads)
                if (pad != null) pad.color = UITheme.Hud.TouchPad;
        }

        /// <summary>The Pause pad (tests and the HUD strip).</summary>
        public RectTransform PauseButton => _pause;

        /// <summary>
        /// The phone's HUD strip (owner, 2026-10-12): Pause sits at the strip's right end, <paramref name="size"/> units
        /// square, its top right corner <paramref name="fromRight"/> and <paramref name="fromTop"/> units in from the safe
        /// area's. A size of 0 puts it back in its PC place.
        /// </summary>
        public void PlacePause(float size, float fromRight, float fromTop)
        {
            _pausePlace = size > 0f ? new Vector3(size, fromRight, fromTop) : (Vector3?)null;
            if (_pause == null) return;
            if (size > 0f) Corner(_pause, new Vector2(1f, 1f), size, new Vector2(-fromRight, -fromTop));
            else Corner(_pause, new Vector2(1f, 1f), PauseSize, new Vector2(-EdgeMargin, -PauseFromTop));
        }

        // Sizes in canvas units (UiScreen: on the owner's phone 1 unit = 0.71 dp, so the stick is 156 dp, Dash 106 dp,
        // Pause 64 dp). Soul Hunter's own numbers; the wiki does not size VS's mobile stick.
        public const float StickSize = 220f, DashSize = 150f, PauseSize = 90f, EdgeMargin = 36f, PauseFromTop = 150f;

        /// <summary>Places a square pad of <paramref name="size"/> units at a corner of the safe area, offset inwards.</summary>
        private static void Corner(RectTransform rect, Vector2 corner, float size, Vector2 offset)
        {
            float s = Mathf.Max(size, UiScreen.MinTouchUnits);
            rect.anchorMin = rect.anchorMax = corner;
            rect.pivot = new Vector2(0.5f, 0.5f); // the floating stick puts its centre under the thumb
            rect.sizeDelta = new Vector2(s, s);
            rect.anchoredPosition = offset + (new Vector2(0.5f, 0.5f) - corner) * s;
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

        private RectTransform TouchButton(RectTransform parent, string label, string controlPath)
        {
            // Owner (2026-10-12: "dash aur pause ko bhi gol kar do"): the theme's round picture in the touch pad's dark
            // see-through colour, so the label reads on any floor.
            var rect = Pad(label + "Button", parent, Vector2.zero, Vector2.zero, ThemedElement.SurfaceRole.Knob);
            Destroy(rect.GetComponent<ThemedElement>());
            rect.GetComponent<Image>().color = UITheme.Hud.TouchPad;
            _roundPads.Add(rect.GetComponent<Image>());
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
            return rect;
        }
    }
}
