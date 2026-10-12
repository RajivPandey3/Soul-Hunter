using UnityEngine;
using UnityEngine.UI;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Gives a canvas the game's one scale (<see cref="UiScreen"/>) and keeps it right through rotations and window
    /// changes. Every screen-space canvas uses it: <see cref="Apply(Canvas)"/> on an existing canvas (the HUD prefab, the
    /// menu scene) or <see cref="Create"/> for a screen's own overlay canvas.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public sealed class ResponsiveCanvas : MonoBehaviour
    {
        private CanvasScaler _scaler;
        private Vector2 _lastPixels;
        private float _lastScale;

        /// <summary>Makes <paramref name="go"/> a root overlay canvas at <paramref name="sortingOrder"/> with the game's scale.</summary>
        public static Canvas Create(GameObject go, int sortingOrder)
        {
            var canvas = go.GetComponent<Canvas>() ?? go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
            if (go.GetComponent<GraphicRaycaster>() == null) go.AddComponent<GraphicRaycaster>();
            Apply(canvas);
            return canvas;
        }

        /// <summary>Puts an existing canvas on the game's scale (its root canvas, if it is nested).</summary>
        public static ResponsiveCanvas Apply(Canvas canvas)
        {
            if (canvas == null) return null;
            if (!canvas.isRootCanvas && canvas.rootCanvas != null) canvas = canvas.rootCanvas;
            var responsive = canvas.GetComponent<ResponsiveCanvas>() ?? canvas.gameObject.AddComponent<ResponsiveCanvas>();
            responsive.Refresh(force: true);
            return responsive;
        }

        private void OnEnable() => Refresh(force: true);

        private void LateUpdate() => Refresh(force: false);

        private void Refresh(bool force)
        {
            if (_scaler == null) _scaler = GetComponent<CanvasScaler>() ?? gameObject.AddComponent<CanvasScaler>();
            Vector2 px = UiScreen.Pixels;
            float scale = UiScreen.ScaleFactor;
            if (!force && px == _lastPixels && Mathf.Approximately(scale, _lastScale)) return;
            bool changed = _lastScale > 0f && (px != _lastPixels || !Mathf.Approximately(scale, _lastScale));
            _lastPixels = px;
            _lastScale = scale;
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            _scaler.scaleFactor = scale;
            _scaler.referencePixelsPerUnit = 100f;
            if (changed) UiScreen.RaiseChanged();
        }
    }
}
