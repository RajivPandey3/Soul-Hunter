using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;
using SoulHunter.Core.Persistence;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// The prompt shown before the main menu, once per launch (vampire.survivors.wiki/w/Main_menu, raw, fetched 2026-10-12):
    /// "Once the player loads the game, a flashing "PRESS TO START" prompt will appear. When clicked, the player will gain
    /// access to the main menu." Update history, 0.2.13: "fixed keyboard input opening/closing menus too quickly"; v1.6.0
    /// mentions "the Press any button screen".
    ///
    /// Soul Hunter's reading:
    /// - Own wording: "Press any key to begin" (a touch screen: "Tap to begin").
    /// - It shows over the menu's own background and logo, where the menu's buttons will be; the buttons are hidden and
    ///   take no input until the player continues. Any key, any controller button, a click or a tap continues.
    /// - The menu's buttons come back one frame after the press, so the press that continues cannot also press a button
    ///   (the 0.2.13 fix).
    /// - "Flashing" is a slow pulse of the prompt's text, and it respects the Flashing VFX option: with the option off the
    ///   text is steady (owner's accessibility rule; ui-ux-pro-max: respect reduced motion).
    /// - The game's version sits small in a corner (coordinator direction, 2026-10-12; VS shows its version number on the
    ///   main menu: v1.6.0 "Removed "RELEASE" from version number on main menu").
    /// Balance method (wiki silent): once per launch, not once per visit to the menu: coming back from a run goes straight
    /// to the menu. Tools that play the game (-sh-autoplay, -sh-benchmark) never see it.
    /// It lives with the HUD code because that assembly reads keys and controllers; the menu calls <see cref="Show"/>.
    /// The layout waits for the owner's approval.
    /// </summary>
    public sealed class StartPrompt : MonoBehaviour
    {
        public const string CanvasName = "StartPromptCanvas";
        /// <summary>Over the menu, under Options (1000).</summary>
        public const int ScreenSortingOrder = 900;
        public const string KeyLine = "Press any key to begin", TouchLine = "Tap to begin";
        public const string VersionPrefix = "Version ";
        /// <summary>The pulse: one slow breath about every 1.6 seconds, never under this share of full strength.</summary>
        public const float PulseSeconds = 1.6f, PulseFloor = 0.35f;

        /// <summary>True once the prompt has been answered (or skipped) in this launch: it does not show again.</summary>
        public static bool ShownThisLaunch;

        public static StartPrompt Current { get; private set; }

        public TextMeshProUGUI PromptText { get; private set; }
        public TextMeshProUGUI VersionText { get; private set; }
        /// <summary>The full-screen catcher: a click or a tap anywhere continues.</summary>
        public Button Catcher { get; private set; }
        /// <summary>The prompt text's strength now (1 when steady).</summary>
        public float PromptAlpha => _pulse != null ? _pulse.alpha : 1f;
        /// <summary>True while the text pulses (the Flashing VFX option is on).</summary>
        public bool IsFlashing { get; private set; }
        public bool Continued { get; private set; }

        private readonly List<(CanvasGroup Group, bool Added, float Alpha, bool Interactable, bool Blocks)> _hidden =
            new List<(CanvasGroup, bool, float, bool, bool)>();
        private readonly List<Button> _buttons = new List<Button>();
        private CanvasGroup _pulse;
        private System.Action _onContinue;
        private System.IDisposable _anyButton;

        /// <summary>
        /// Shows the prompt over the menu and hides <paramref name="menuButtons"/> until the player continues, then runs
        /// <paramref name="onContinue"/> (the menu focuses its first button there). Returns null, doing nothing, when the
        /// prompt has already been answered in this launch or a tool is playing.
        /// </summary>
        public static StartPrompt Show(IEnumerable<Button> menuButtons, System.Action onContinue)
        {
            if (ShownThisLaunch) return null;
            if (ToolIsPlaying()) { ShownThisLaunch = true; return null; }
            if (Current != null) return Current;
            var go = new GameObject(CanvasName, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = ScreenSortingOrder;
            ResponsiveCanvas.Apply(canvas); // the game's one UI scale (UiScreen), landscape and portrait
            go.AddComponent<GraphicRaycaster>();
            var prompt = go.AddComponent<StartPrompt>();
            prompt._onContinue = onContinue;
            prompt.Build(menuButtons);
            return prompt;
        }

        private static bool ToolIsPlaying()
        {
            foreach (string arg in System.Environment.GetCommandLineArgs())
                if (arg == "-sh-autoplay" || arg == "-sh-benchmark") return true;
            return false;
        }

        private void Awake() => Current = this;

        private void OnDestroy()
        {
            if (Current == this) Current = null;
            _anyButton?.Dispose();
            _anyButton = null;
            Restore(); // the menu is never left without its buttons (a scene change while the prompt is up)
        }

        private void Build(IEnumerable<Button> menuButtons)
        {
            var root = (RectTransform)transform;

            // No veil: the menu's background and logo stay as they are. The catcher only takes the click or tap.
            var catcher = Area("Catcher", root);
            var catcherImage = catcher.gameObject.AddComponent<Image>();
            catcherImage.color = Color.clear;
            catcherImage.raycastTarget = true;
            Catcher = catcher.gameObject.AddComponent<Button>();
            Catcher.transition = Selectable.Transition.None;
            Catcher.navigation = new Navigation { mode = Navigation.Mode.None };
            Catcher.targetGraphic = catcherImage;
            Catcher.onClick.AddListener(Continue);

            var content = Area("Content", root);
            SafeAreaFitter.Fit(content);

            var promptRect = Area("Prompt", content);
            promptRect.anchorMin = promptRect.anchorMax = promptRect.pivot = new Vector2(0.5f, 0.5f);
            _pulse = promptRect.gameObject.AddComponent<CanvasGroup>();
            _pulse.interactable = _pulse.blocksRaycasts = false;
            PromptText = promptRect.gameObject.AddComponent<TextMeshProUGUI>();
            PromptText.fontSize = 46f; // a size before auto-size (TMP's defaults would replace the limits)
            PromptText.enableAutoSizing = true;
            PromptText.fontSizeMin = Mathf.Max(18f, UiScreen.MinTextUnits);
            PromptText.fontSizeMax = 46f;
            PromptText.alignment = TextAlignmentOptions.Center;
            PromptText.textWrappingMode = TextWrappingModes.NoWrap;
            PromptText.raycastTarget = false;
            PromptText.text = UiScreen.IsTouch ? TouchLine : KeyLine;
            UITheme.ApplyText(PromptText, ThemedElement.TextRole.Title);

            var versionRect = Area("Version", content);
            versionRect.anchorMin = versionRect.anchorMax = versionRect.pivot = new Vector2(1f, 0f);
            VersionText = versionRect.gameObject.AddComponent<TextMeshProUGUI>();
            VersionText.enableAutoSizing = false;
            VersionText.alignment = TextAlignmentOptions.BottomRight;
            VersionText.textWrappingMode = TextWrappingModes.NoWrap;
            VersionText.raycastTarget = false;
            VersionText.text = VersionPrefix + Application.version;
            UITheme.ApplyText(VersionText, ThemedElement.TextRole.Soft);

            if (menuButtons != null)
                foreach (var button in menuButtons)
                {
                    if (button == null) continue;
                    _buttons.Add(button);
                    var group = button.GetComponent<CanvasGroup>();
                    bool added = group == null;
                    if (added) group = button.gameObject.AddComponent<CanvasGroup>();
                    _hidden.Add((group, added, group.alpha, group.interactable, group.blocksRaycasts));
                    group.alpha = 0f;
                    group.interactable = false;
                    group.blocksRaycasts = false;
                }
            Place();
            // Any button of any device (a key, a controller button, a mouse button, a touch) continues, as soon as the press
            // is read. Update() below also looks, for a device this does not report.
            _anyButton = InputSystem.onAnyButtonPress.Call(_ => Continue());
        }

        private void Update()
        {
            if (Continued) return;
            Place();
            // VS: a flashing prompt. Here a slow pulse, and steady when the player switched Flashing VFX off.
            IsFlashing = GameSettings.FlashingVfx;
            if (_pulse != null)
                _pulse.alpha = IsFlashing ? PulseAt(Time.unscaledTime) : 1f;

            bool pressed = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
            if (!pressed && Gamepad.current != null)
                foreach (var control in Gamepad.current.allControls)
                    if (control is ButtonControl button && !button.synthetic && button.wasPressedThisFrame) { pressed = true; break; }
            if (pressed) Continue();
        }

        /// <summary>The pulse's strength at a time: a smooth breath between <see cref="PulseFloor"/> and 1.</summary>
        public static float PulseAt(float seconds)
        {
            float wave = 0.5f + 0.5f * Mathf.Cos(seconds / PulseSeconds * Mathf.PI * 2f);
            return Mathf.Lerp(PulseFloor, 1f, wave);
        }

        /// <summary>
        /// The prompt sits where the menu's buttons will be (they keep their places while hidden), so it is under the logo
        /// in the frame on a PC and in the middle of the safe area on a phone, in either orientation.
        /// </summary>
        private void Place()
        {
            if (PromptText == null) return;
            var rect = PromptText.rectTransform;
            var parent = (RectTransform)rect.parent;
            Vector2 centre = parent.rect.center; // without buttons: the middle of the safe area
            float width = Mathf.Min(900f, parent.rect.width * 0.9f);
            bool any = false;
            Vector3 min = Vector3.zero, max = Vector3.zero;
            var corners = new Vector3[4];
            foreach (var button in _buttons)
            {
                if (button == null) continue;
                ((RectTransform)button.transform).GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    Vector3 local = parent.InverseTransformPoint(corner);
                    if (!any) { min = max = local; any = true; }
                    else { min = Vector3.Min(min, local); max = Vector3.Max(max, local); }
                }
            }
            if (any)
            {
                centre = (min + max) * 0.5f;
                // Kept inside the safe area whatever the buttons' block is.
                float half = width * 0.5f;
                Rect room = parent.rect;
                centre.x = Mathf.Clamp(centre.x, room.xMin + half, Mathf.Max(room.xMin + half, room.xMax - half));
            }
            float height = Mathf.Max(72f, UiScreen.MinTextUnits * 3f);
            rect.sizeDelta = new Vector2(Mathf.Max(1f, width), height);
            // The parent's pivot is its centre (a stretched area), so a local point is the anchored position from the centre anchor.
            rect.anchoredPosition = centre - parent.rect.center;

            if (VersionText != null)
            {
                float size = Mathf.Max(20f, UiScreen.MinTextUnits);
                VersionText.fontSize = size;
                VersionText.rectTransform.sizeDelta = new Vector2(Mathf.Min(420f, parent.rect.width * 0.6f), size * 1.6f);
                VersionText.rectTransform.anchoredPosition = new Vector2(-16f, 12f);
            }
        }

        /// <summary>Answers the prompt: the menu's buttons come back on the next frame and the prompt goes.</summary>
        public void Continue()
        {
            if (Continued) return;
            Continued = true;
            ShownThisLaunch = true;
            _anyButton?.Dispose();
            _anyButton = null;
            var audio = SoulHunter.Gameplay.Audio.AudioManager.Instance;
            if (audio != null) audio.PlaySFX(audio.UIConfirmSound);
            if (_pulse != null) _pulse.alpha = 0f;
            if (VersionText != null) VersionText.gameObject.SetActive(false);
            StartCoroutine(Leave());
        }

        private IEnumerator Leave()
        {
            // One frame with the menu still out of reach: the press that continued cannot also press Start (VS 0.2.13:
            // "fixed keyboard input opening/closing menus too quickly").
            yield return null;
            Restore();
            var onContinue = _onContinue;
            _onContinue = null;
            onContinue?.Invoke();
            Destroy(gameObject);
        }

        private void Restore()
        {
            foreach (var (group, added, alpha, interactable, blocks) in _hidden)
            {
                if (group == null) continue;
                // Usable again at once (a group this prompt added is removed at the end of the frame, so it is opened first).
                group.alpha = added ? 1f : alpha;
                group.interactable = added || interactable;
                group.blocksRaycasts = added || blocks;
                if (added) Destroy(group);
            }
            _hidden.Clear();
        }

        private static RectTransform Area(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}
