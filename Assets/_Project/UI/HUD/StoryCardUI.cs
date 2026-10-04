using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// Tells the owner's story in play (owner, 2026-09-27: "MERE STORY KO FOLLOW KARO"): when a stage starts, a card
    /// under the stage label shows the level name and its story line (CampaignStory.CardText; the premise on Level 1,
    /// the twist on Level 10). It does not pause the game, and it hides and waits while a menu has the game paused.
    /// The HUD adds this to itself.
    /// </summary>
    public sealed class StoryCardUI : MonoBehaviour
    {
        public const float FadeSeconds = 0.6f;
        public const float SecondsPerCharacter = 0.045f;
        public const float MinSeconds = 5f;

        private CanvasGroup _group;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _body;
        private LevelProgressionManager _progression;
        private float _shownFor;
        private float _duration;

        public bool IsShowing => _group != null && _group.gameObject.activeSelf;
        public float Alpha => _group != null ? _group.alpha : 0f;
        public string Title => _title != null ? _title.text : "";
        public string Body => _body != null ? _body.text : "";

        public static float DurationFor(string text) =>
            Mathf.Max(MinSeconds, (text ?? "").Length * SecondsPerCharacter) + 2f * FadeSeconds;

        public void Setup()
        {
            if (_group != null) return;
            Build();
            _progression = LevelProgressionManager.Instance;
            if (_progression != null)
            {
                _progression.OnStageStarted += Show;
                // The first stage started before the HUD was ready.
                Show(_progression.CurrentStage);
            }
        }

        private void OnDestroy()
        {
            if (_progression != null) _progression.OnStageStarted -= Show;
        }

        public void Show(int stage)
        {
            if (_group == null) return;
            var level = CampaignLevelCatalog.Get(stage);
            _title.text = $"Level {level.Number}: {level.Name}";
            _body.text = CampaignStory.CardText(level.Number);
            _duration = DurationFor(_body.text);
            _shownFor = 0f;
            _group.alpha = 0f;
            _group.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!IsShowing) return;
            // A level-up, chest or pause menu is see-through: hide the card under it and carry on once it closes
            // (editor play showed the Level 10 card's title through "LEVEL UP!").
            if (GameTime.IsPaused)
            {
                _group.alpha = 0f;
                return;
            }
            _shownFor += Time.unscaledDeltaTime;
            float fadeIn = Mathf.Clamp01(_shownFor / FadeSeconds);
            float fadeOut = Mathf.Clamp01((_duration - _shownFor) / FadeSeconds);
            _group.alpha = Mathf.Min(fadeIn, fadeOut);
            if (_shownFor >= _duration) _group.gameObject.SetActive(false);
        }

        private void Build()
        {
            var card = new GameObject("StoryCard", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            var rect = (RectTransform)card.transform;
            rect.SetParent(HudReadability.CanvasRoot(transform), false);
            rect.SetAsFirstSibling(); // under the menus
            // Bug MIS v2 C21 (2026-10-02): from 20% of the width the card covered the HUD's item slots (they end near 21%).
            rect.anchorMin = new Vector2(0.24f, 0.62f);
            rect.anchorMax = new Vector2(0.76f, 0.86f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var background = card.GetComponent<Image>();
            UITheme.ApplySurface(background, ThemedElement.SurfaceRole.StoryCard);
            background.raycastTarget = false;
            _group = card.GetComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;

            _title = Label(rect, "Title", new Vector2(0.04f, 0.68f), new Vector2(0.96f, 0.96f), 30f, FontStyles.Bold);
            UITheme.ApplyText(_title, ThemedElement.TextRole.Accent); // like the level-up heading
            _body = Label(rect, "Story", new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.68f), 24f, FontStyles.Italic);
            card.SetActive(false);
        }

        private static TextMeshProUGUI Label(RectTransform parent, string name, Vector2 min, Vector2 max, float size, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var label = go.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = size;
            label.fontStyle = style;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            HudReadability.MakeReadable(label);
            return label;
        }
    }
}
