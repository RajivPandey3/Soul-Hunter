using UnityEngine;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Keeps a screen's root rect inside the device's safe area (notch, rounded corners, gesture bar; ui-ux-pro-max
    /// "Safe-area compliance"), in landscape and portrait. Backdrops and veils stay full screen; only the content root takes
    /// this. On a desktop the safe area is the whole screen, so nothing moves.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect _last;

        /// <summary>Adds the fitter to <paramref name="rect"/> and fits it now.</summary>
        public static SafeAreaFitter Fit(RectTransform rect)
        {
            if (rect == null) return null;
            var fitter = rect.GetComponent<SafeAreaFitter>() ?? rect.gameObject.AddComponent<SafeAreaFitter>();
            fitter.Apply(force: true);
            return fitter;
        }

        private void OnEnable() => Apply(force: true);

        private void LateUpdate() => Apply(force: false);

        private void Apply(bool force)
        {
            Rect safe = UiScreen.SafeAreaNormalized;
            if (!force && safe == _last) return;
            _last = safe;
            var rect = (RectTransform)transform;
            rect.anchorMin = safe.min;
            rect.anchorMax = safe.max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
