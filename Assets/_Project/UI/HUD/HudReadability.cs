using TMPro;
using UnityEngine;
using SoulHunter.Core.Persistence;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Readability for the run HUD: the kill, gold and time labels were dark blue on the dark ground; they
    /// become white with a black outline. Also shows the current stage (VS "Show stage progression") at the
    /// top of the screen, hidden when the option is off. The HUD adds this to itself.
    /// </summary>
    public sealed class HudReadability : MonoBehaviour
    {
        private TextMeshProUGUI _stageLabel;
        private LevelProgressionManager _progression;

        public TextMeshProUGUI StageLabel => _stageLabel;

        public void Setup(params TMP_Text[] labels)
        {
            foreach (var label in labels)
            {
                MakeReadable(label);
                if (label == null) continue;
                // Bug MIS B14: "Total Kills:" wrapped onto two lines at four digits and the HUD shifted; one line.
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Overflow;
                HudBacking.Add(label);
                HudMenuHide.Attach(label.gameObject);
            }

            var go = new GameObject("StageProgress_Text", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(CanvasRoot(transform), false);
            rect.anchorMin = new Vector2(0.3f, 0.91f);
            rect.anchorMax = new Vector2(0.7f, 0.97f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            _stageLabel = go.AddComponent<TextMeshProUGUI>();
            _stageLabel.alignment = TextAlignmentOptions.Center;
            _stageLabel.enableAutoSizing = true;
            _stageLabel.fontSizeMin = 16f;
            _stageLabel.fontSizeMax = 34f;
            _stageLabel.raycastTarget = false;
            MakeReadable(_stageLabel);
            HudBacking.Add(_stageLabel);
            HudMenuHide.Attach(go);

            _progression = LevelProgressionManager.Instance;
            if (_progression != null) _progression.OnStageStarted += HandleStageStarted;
            GameSettings.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_progression != null) _progression.OnStageStarted -= HandleStageStarted;
            GameSettings.Changed -= Refresh;
        }

        private void HandleStageStarted(int _)
        {
            _stageShownFor = 0f;
            Refresh();
        }

        /// <summary>Seconds of play the stage name stays on a phone before it hides (owner, 2026-10-12).</summary>
        public const float PhoneStageSeconds = 5f;
        private float _stageShownFor;
        private float _phoneTop = -1f;

        /// <summary>
        /// The phone's HUD strip: the stage name is one small line under the strip, <paramref name="fromTop"/> units below
        /// the top of <paramref name="root"/>, shown for the first seconds of each stage. A negative value puts it back
        /// in its PC place, always shown.
        /// </summary>
        public void PlaceForPhone(RectTransform root, float fromTop, float height, float textSize)
        {
            if (_stageLabel == null) return;
            var rect = _stageLabel.rectTransform;
            bool phone = root != null && fromTop >= 0f;
            if (phone)
            {
                if (rect.parent != root) MoveWithBacking(rect, root);
                rect.anchorMin = new Vector2(0.04f, 1f);
                rect.anchorMax = new Vector2(0.96f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(0f, height);
                rect.anchoredPosition = new Vector2(0f, -fromTop);
                _stageLabel.fontSizeMin = Mathf.Min(textSize, UiScreen.MinTextUnits);
                _stageLabel.fontSizeMax = textSize;
                _stageLabel.textWrappingMode = TextWrappingModes.NoWrap;
            }
            else if (_phoneTop >= 0f)
            {
                var canvasRoot = CanvasRoot(transform);
                if (rect.parent != canvasRoot) MoveWithBacking(rect, canvasRoot);
                rect.anchorMin = new Vector2(0.3f, 0.91f);
                rect.anchorMax = new Vector2(0.7f, 0.97f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                _stageLabel.fontSizeMin = 16f;
                _stageLabel.fontSizeMax = 34f;
                _stageLabel.textWrappingMode = TextWrappingModes.Normal;
            }
            _phoneTop = phone ? fromTop : -1f;
            Refresh();
        }

        /// <summary>The label's backing bar is its sibling, drawn just before it; both move together.</summary>
        private static void MoveWithBacking(RectTransform label, Transform to)
        {
            var backing = label.parent != null ? label.parent.Find(label.name + "_Backing") : null;
            if (backing != null) backing.SetParent(to, false);
            label.SetParent(to, false);
        }

        private void Update()
        {
            if (_phoneTop < 0f || _stageLabel == null || _stageShownFor >= PhoneStageSeconds) return;
            _stageShownFor += Time.deltaTime; // game time: a pause does not use the seconds up
            if (_stageShownFor >= PhoneStageSeconds) Refresh();
        }

        private void Refresh()
        {
            if (_stageLabel == null) return;
            _stageLabel.gameObject.SetActive(GameSettings.ShowStageProgression && (_phoneTop < 0f || _stageShownFor < PhoneStageSeconds));
            int stage = _progression != null ? _progression.CurrentStage : 1;
            _stageLabel.text = $"Stage {stage}/{LevelProgressionManager.MaxStages}: {CampaignLevelCatalog.Get(stage).Name}";
        }

        /// <summary>The full-screen root of the canvas holding t (the HUD controller itself is not full-screen).</summary>
        public static Transform CanvasRoot(Transform t)
        {
            var canvas = t.GetComponentInParent<Canvas>();
            return canvas != null ? canvas.rootCanvas.transform : t;
        }

        /// <summary>The theme's HUD label colour with its outline, readable on every stage floor.</summary>
        public static void MakeReadable(TMP_Text label)
        {
            if (label == null) return;
            // TMP builds its material on Awake; a label under an inactive parent cannot take an outline yet.
            if (!label.gameObject.activeInHierarchy) { label.color = UITheme.Hud.LabelText; return; }
            UITheme.ApplyText(label, ThemedElement.TextRole.HudLabel);
        }
    }

    /// <summary>
    /// Bug MIS B06 (2026-09-30): white damage numbers flying behind the white HUD text made it unreadable ("Tota50 Kills").
    /// A dark translucent bar sits behind each HUD label, sized to its text every frame, so numbers passing behind are
    /// dimmed and the label stays clean. The HUD canvas draws over the world, so the bar covers them.
    /// </summary>
    public sealed class HudBacking : MonoBehaviour
    {
        // Bug MIS v2 C11 (2026-10-02): at 55% the world showed through, so damage numbers, fire and blood read as drawn over
        // the labels ("3Gold: 6628"). 85% keeps the labels clean.
        public static Color Color => UITheme.Hud.LabelBacking; // Theme Studio > HUD
        private const float Padding = 10f;
        private TMP_Text _label;
        private RectTransform _rect;

        public static HudBacking Add(TMP_Text label)
        {
            if (label == null || label.transform.parent == null) return null;
            var go = new GameObject(label.name + "_Backing", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(label.transform.parent, false);
            rect.SetSiblingIndex(label.transform.GetSiblingIndex()); // drawn just before (behind) the label
            var image = go.GetComponent<UnityEngine.UI.Image>();
            UITheme.ApplySurface(image, ThemedElement.SurfaceRole.HudBacking);
            image.raycastTarget = false;
            go.AddComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
            var backing = go.AddComponent<HudBacking>();
            backing._label = label;
            backing._rect = rect;
            return backing;
        }

        private void LateUpdate()
        {
            if (_label == null) { Destroy(gameObject); return; }
            bool show = _label.isActiveAndEnabled && !string.IsNullOrEmpty(_label.text) && !HudMenuHide.Hidden;
            var image = GetComponent<UnityEngine.UI.Image>();
            image.enabled = show;
            if (!show) return;
            // The label's drawn text, in the parent's space.
            Bounds b = _label.textBounds;
            var labelRect = _label.rectTransform;
            Vector3 min = labelRect.TransformPoint(b.min), max = labelRect.TransformPoint(b.max);
            var parent = (RectTransform)_rect.parent;
            Vector3 pMin = parent.InverseTransformPoint(min), pMax = parent.InverseTransformPoint(max);
            _rect.anchorMin = _rect.anchorMax = parent.pivot;
            _rect.pivot = new Vector2(0.5f, 0.5f);
            Vector2 centre = (pMin + pMax) * 0.5f, size = new Vector2(Mathf.Abs(pMax.x - pMin.x), Mathf.Abs(pMax.y - pMin.y)) + Vector2.one * Padding * 2f;
            // anchors sit on the parent's pivot, so the position is relative to it
            _rect.anchoredPosition = centre;
            _rect.sizeDelta = size;
        }
    }
}
