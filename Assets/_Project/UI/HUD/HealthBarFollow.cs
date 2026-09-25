using UnityEngine;
using UnityEngine.UI;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Learning Comment: VS jaisa chhota HP bar jo hero ke pairon ke neeche chalta hai (owner request 2026-09-26).
    /// The HUD's health slider used to be a red strip across the whole bottom of the screen. This keeps the same slider
    /// (GameHUDController still sets its value), makes it small, styles it (dark back, red fill) and every frame puts it
    /// just under the hero on screen.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DefaultExecutionOrder(1000)] // after CameraFollow, so the bar never trails the camera by a frame
    public sealed class HealthBarFollow : MonoBehaviour
    {
        /// <summary>Bar size in canvas units (the HUD canvas is 1920x1080 reference).</summary>
        public static readonly Vector2 Size = new Vector2(96f, 11f);
        /// <summary>How far under the hero's feet (canvas units).</summary>
        public const float BelowFeet = 10f;

        public Transform Target;

        private RectTransform _rect;
        private RectTransform _canvasRect;
        private Canvas _canvas;

        /// <summary>Turns the HUD slider into a small bar that follows the target.</summary>
        public static HealthBarFollow Attach(Slider slider, Transform target)
        {
            if (slider == null || target == null) return null;
            var follow = slider.GetComponent<HealthBarFollow>() ?? slider.gameObject.AddComponent<HealthBarFollow>();
            follow.Target = target;
            follow.Setup(slider);
            return follow;
        }

        private void Setup(Slider slider)
        {
            _rect = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>().rootCanvas;
            _canvasRect = (RectTransform)_canvas.transform;
            // Straight under the canvas so its position is in canvas space, drawn first (under the menus).
            _rect.SetParent(_canvasRect, false);
            _rect.SetAsFirstSibling();
            _rect.anchorMin = _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 1f);
            _rect.sizeDelta = Size;
            _rect.localScale = Vector3.one;

            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            if (slider.handleRect != null) slider.handleRect.gameObject.SetActive(false);
            var background = transform.Find("Background");
            if (background != null && background.TryGetComponent<Image>(out var back)) back.color = new Color(0.05f, 0.03f, 0.05f, 0.85f);
            if (slider.fillRect != null)
            {
                if (slider.fillRect.TryGetComponent<Image>(out var fill)) fill.color = new Color(0.9f, 0.12f, 0.12f, 1f);
                // The fill area can carry inset offsets sized for the wide strip; keep the fill inside the small bar.
                var area = slider.fillRect.parent as RectTransform;
                if (area != null)
                {
                    area.anchorMin = Vector2.zero;
                    area.anchorMax = Vector2.one;
                    area.offsetMin = new Vector2(1.5f, 1.5f);
                    area.offsetMax = new Vector2(-1.5f, -1.5f);
                }
                slider.fillRect.offsetMin = slider.fillRect.offsetMax = Vector2.zero;
            }
            LateUpdate();
        }

        private void LateUpdate()
        {
            if (Target == null || _canvasRect == null) return;
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 screen = cam.WorldToScreenPoint(FeetPosition());
            if (screen.z < 0f) return;
            var eventCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, eventCamera, out var local))
                _rect.anchoredPosition = local + new Vector2(0f, -BelowFeet);
        }

        /// <summary>The hero's feet: the player root sits at the body centre, one unit above the ground.</summary>
        public Vector3 FeetPosition() => Target.position + Vector3.down * 1f;
    }
}
