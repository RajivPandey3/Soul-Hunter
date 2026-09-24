using UnityEngine;
using UnityEngine.UI;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Learning Comment:
    /// Low-HP warning (VS-style feedback): jab player ki health threshold (default 30%) ya usse kam ho,
    /// screen par halka laal (red) tint dhadakta (pulse) hai. Overlay runtime par HUD ke peeche banta hai,
    /// clicks block nahi karta, aur naam se reuse hota hai taake do HUD scripts do overlay na banayein.
    /// </summary>
    public class LowHealthWarning : MonoBehaviour
    {
        public const string OverlayName = "LowHealthWarning_Overlay";

        [SerializeField, Range(0f, 1f)] private float _threshold = 0.3f;
        [SerializeField] private Color _tint = new Color(0.8f, 0f, 0f, 1f);
        [SerializeField, Range(0f, 1f)] private float _maxAlpha = 0.25f;
        [SerializeField, Min(0.1f)] private float _pulsesPerSecond = 1.5f;

        private HealthController _health;
        private Image _overlay;

        public bool IsWarning { get; private set; }

        /// <summary>True while alive and at or below the threshold share of max health.</summary>
        public static bool IsLow(int current, int max, float threshold) =>
            max > 0 && current > 0 && current <= max * threshold;

        public void Bind(HealthController health)
        {
            if (_health != null) _health.OnHealthChanged -= HandleHealthChanged;
            _health = health;
            EnsureOverlay();
            if (_health == null) return;
            _health.OnHealthChanged += HandleHealthChanged;
            HandleHealthChanged(_health.CurrentHealth, _health.MaxHealth);
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnHealthChanged -= HandleHealthChanged;
        }

        private void HandleHealthChanged(int current, int max)
        {
            IsWarning = IsLow(current, max, _threshold);
            if (_overlay != null) _overlay.enabled = IsWarning;
        }

        private void Update()
        {
            if (!IsWarning || _overlay == null) return;
            // Unscaled so the warning keeps pulsing on pause and level-up screens.
            float wave = (Mathf.Sin(Time.unscaledTime * _pulsesPerSecond * 2f * Mathf.PI) + 1f) * 0.5f;
            var color = _tint;
            color.a = _maxAlpha * (0.4f + 0.6f * wave);
            _overlay.color = color;
        }

        private void EnsureOverlay()
        {
            if (_overlay != null) return;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            Transform root = canvas.rootCanvas.transform;

            Transform existing = root.Find(OverlayName);
            if (existing != null)
            {
                _overlay = existing.GetComponent<Image>();
                return;
            }

            var overlayObject = new GameObject(OverlayName, typeof(RectTransform));
            overlayObject.layer = gameObject.layer;
            overlayObject.transform.SetParent(root, false);
            overlayObject.transform.SetAsFirstSibling(); // behind the rest of the HUD

            var rect = (RectTransform)overlayObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _overlay = overlayObject.AddComponent<Image>();
            _overlay.raycastTarget = false;
            _overlay.color = new Color(_tint.r, _tint.g, _tint.b, 0f);
            _overlay.enabled = false;
        }
    }
}
