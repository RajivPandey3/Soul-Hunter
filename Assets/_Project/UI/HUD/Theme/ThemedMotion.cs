using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Button and card motion, switched and tuned in the theme (Theme Studio > Movement). Following the ui-ux-pro-max
    /// guidelines (2026-10-04: "animate 1-2 key elements per view", "visible focus on every control", "respect reduced
    /// motion") and frontend-design ("spend boldness in one place"):
    /// - only the chosen (focused) button breathes, gets the light sweep and a focus ring, so one thing moves per screen;
    /// - every button answers the player: it lifts under the mouse and squashes when pressed;
    /// - with the player's Flashing VFX option off, nothing moves on its own (the focus ring stays).
    /// Runs on real time, so it also moves while the game is paused behind a menu. Only scale is animated, so layouts are
    /// never disturbed.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ThemedMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public bool IsCard;
        private bool _hover, _pressed;
        private RectTransform _shine;
        private ThemeFocusRing _ring;

        /// <summary>True while this button is the chosen one (keyboard/controller focus).</summary>
        public bool Focused => EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;

        /// <summary>The player's Options switch: with Flashing VFX off, UI motion that is not a reply to the player stops.</summary>
        public static bool AmbientMotionAllowed => SoulHunter.Core.Persistence.GameSettings.FlashingVfx;

        private void OnDisable()
        {
            _hover = _pressed = false;
            transform.localScale = Vector3.one;
            if (_ring != null) _ring.enabled = false;
            if (_shine != null) _shine.gameObject.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData) => _hover = true;
        public void OnPointerExit(PointerEventData eventData) { _hover = false; _pressed = false; }
        public void OnPointerDown(PointerEventData eventData) => _pressed = true;
        public void OnPointerUp(PointerEventData eventData) => _pressed = false;

        private void Update()
        {
            var theme = UiThemeAsset.Active;
            var motion = theme.Motions;
            bool focused = Focused;
            var selectable = GetComponent<Selectable>();
            bool usable = selectable == null || selectable.IsInteractable();
            bool ambient = AmbientMotionAllowed;

            Vector3 scale = Vector3.one;
            if (usable && focused && motion.Breathe && ambient)
                scale *= 1f + (IsCard ? 0.5f : 1f) * motion.BreatheAmount * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * motion.BreatheSpeed));
            if (usable && _hover && motion.HoverLift) scale *= IsCard ? 1.015f : 1.03f;
            if (usable && _pressed && motion.PressSquish) scale = new Vector3(scale.x * 1.03f, scale.y * 0.93f, 1f);
            transform.localScale = Vector3.Lerp(transform.localScale, scale, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));

            FocusRing(theme, usable && focused);
            UpdateShine(theme, usable && focused && !IsCard && ambient);
        }

        // Focus is shown by a ring as well as by colour, so it never depends on colour alone.
        private void FocusRing(UiThemeAsset theme, bool on)
        {
            var graphic = GetComponent<Selectable>() != null ? GetComponent<Selectable>().targetGraphic : null;
            if (graphic == null) return;
            if (_ring == null)
            {
                if (!on) return;
                _ring = graphic.GetComponent<ThemeFocusRing>();
                if (_ring == null) _ring = graphic.gameObject.AddComponent<ThemeFocusRing>();
            }
            _ring.enabled = on && theme.Motions.FocusRingWidth > 0f;
            _ring.effectColor = theme.Buttons.Focus;
            _ring.effectDistance = new Vector2(theme.Motions.FocusRingWidth, -theme.Motions.FocusRingWidth);
            _ring.useGraphicAlpha = false;
        }

        private void UpdateShine(UiThemeAsset theme, bool allowed)
        {
            var sprite = theme.Shape.Shine;
            bool on = allowed && theme.Motions.Shine && sprite != null;
            if (!on) { if (_shine != null) _shine.gameObject.SetActive(false); return; }
            if (_shine == null)
            {
                var mask = new GameObject("ThemeShineMask", typeof(RectTransform), typeof(RectMask2D));
                var maskRect = (RectTransform)mask.transform;
                maskRect.SetParent(transform, false);
                maskRect.anchorMin = Vector2.zero;
                maskRect.anchorMax = Vector2.one;
                maskRect.offsetMin = new Vector2(6f, 6f);
                maskRect.offsetMax = new Vector2(-6f, -6f);
                maskRect.SetSiblingIndex(Mathf.Min(1, transform.childCount - 1));
                var band = new GameObject("ThemeShine", typeof(RectTransform), typeof(Image));
                _shine = (RectTransform)band.transform;
                _shine.SetParent(maskRect, false);
                var image = band.GetComponent<Image>();
                image.raycastTarget = false;
                image.sprite = sprite;
                mask.AddComponent<LayoutElement>().ignoreLayout = true;
            }
            _shine.gameObject.SetActive(true);
            var parent = (RectTransform)_shine.parent;
            float width = parent.rect.width, height = parent.rect.height;
            _shine.anchorMin = _shine.anchorMax = new Vector2(0f, 0.5f);
            _shine.sizeDelta = new Vector2(height * 0.6f, height * 1.6f);
            float cycle = Mathf.Max(0.8f, theme.Motions.ShineEvery);
            float k = Mathf.Repeat(Time.unscaledTime, cycle) / Mathf.Min(0.9f, cycle * 0.5f); // a quick pass, then rest
            _shine.anchoredPosition = new Vector2(Mathf.Lerp(-height, width + height, Mathf.Clamp01(k)), 0f);
        }
    }

    /// <summary>
    /// The icon on a themed button moves a little (a flicker and a tilt) while its button is the chosen one, when the
    /// theme and the player's Flashing VFX option allow it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ThemedIconMotion : MonoBehaviour
    {
        private ThemedMotion _button;
        private void OnDisable() { transform.localRotation = Quaternion.identity; transform.localScale = Vector3.one; }

        private void Update()
        {
            if (_button == null) _button = GetComponentInParent<ThemedMotion>();
            bool on = UiThemeAsset.Active.Motions.IconMotion && ThemedMotion.AmbientMotionAllowed && _button != null && _button.Focused;
            if (!on) { transform.localRotation = Quaternion.identity; transform.localScale = Vector3.one; return; }
            float t = Time.unscaledTime;
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2.2f) * 6f);
            float s = 1f + Mathf.Sin(t * 5.1f) * 0.035f;
            transform.localScale = new Vector3(s, 1f + Mathf.Sin(t * 5.1f + 1f) * 0.05f, 1f);
        }
    }

    /// <summary>Titles bounce gently, when the theme and the player's Flashing VFX option allow it.</summary>
    [DisallowMultipleComponent]
    public sealed class ThemedTitleMotion : MonoBehaviour
    {
        private void OnDisable() { transform.localScale = Vector3.one; transform.localRotation = Quaternion.identity; }

        private void Update()
        {
            if (!UiThemeAsset.Active.Motions.TitleBounce || !ThemedMotion.AmbientMotionAllowed)
            {
                transform.localScale = Vector3.one;
                transform.localRotation = Quaternion.identity;
                return;
            }
            float t = Time.unscaledTime;
            float s = 1f + 0.035f * (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f * 0.55f));
            transform.localScale = new Vector3(s, s, 1f);
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 2f * 0.55f) * -0.8f);
        }
    }
}
