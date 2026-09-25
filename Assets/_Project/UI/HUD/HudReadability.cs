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
            foreach (var label in labels) MakeReadable(label);

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

        private void HandleStageStarted(int _) => Refresh();

        private void Refresh()
        {
            if (_stageLabel == null) return;
            _stageLabel.gameObject.SetActive(GameSettings.ShowStageProgression);
            int stage = _progression != null ? _progression.CurrentStage : 1;
            _stageLabel.text = $"Stage {stage}/{LevelProgressionManager.MaxStages}: {CampaignLevelCatalog.Get(stage).Name}";
        }

        /// <summary>The full-screen root of the canvas holding t (the HUD controller itself is not full-screen).</summary>
        public static Transform CanvasRoot(Transform t)
        {
            var canvas = t.GetComponentInParent<Canvas>();
            return canvas != null ? canvas.rootCanvas.transform : t;
        }

        /// <summary>White text with a black outline reads on every stage floor.</summary>
        public static void MakeReadable(TMP_Text label)
        {
            if (label == null) return;
            label.color = Color.white;
            // TMP builds its material on Awake; a label under an inactive parent cannot take an outline yet.
            if (!label.gameObject.activeInHierarchy) return;
            label.outlineWidth = 0.25f;
            label.outlineColor = new Color32(0, 0, 0, 255);
        }
    }
}
