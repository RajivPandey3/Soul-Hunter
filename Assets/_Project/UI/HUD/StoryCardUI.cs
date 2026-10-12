using System.Collections.Generic;
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
    ///
    /// Responsive (owner, 2026-10-10: landscape and portrait from the same screen). The card is placed in canvas units
    /// inside the safe area, at most <see cref="MaxWidth"/> wide and never wider than the screen minus its margins. On a
    /// wide screen it sits in the free space between the HUD's top-left labels and item slots and its top-right items, under
    /// the stage label (as on 1920x1080). Where that space is too narrow (a phone held upright) it spans the width under all
    /// of them. Its height follows its text, and its text never goes under the touch floor (12 sp).
    /// </summary>
    public sealed class StoryCardUI : MonoBehaviour
    {
        public const float FadeSeconds = 0.6f;
        public const float SecondsPerCharacter = 0.045f;
        public const float MinSeconds = 5f;
        // Canvas units. On 1920x1080 the card is 1000 x 259, 151 under the top edge (the approved place).
        public const float MaxWidth = 1000f, MinWidth = 420f, DesignHeight = 259f, TopOffset = 151f;
        public const float Margin = 24f, Pad = 16f, TitleHeight = 48f, TitleSize = 30f, BodySize = 24f;

        private CanvasGroup _group;
        private RectTransform _rect;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _body;
        private LevelProgressionManager _progression;
        private float _shownFor;
        private float _duration;
        private float _nextLayout;

        public bool IsShowing => _group != null && _group.gameObject.activeSelf;
        public float Alpha => _group != null ? _group.alpha : 0f;
        public string Title => _title != null ? _title.text : "";
        public string Body => _body != null ? _body.text : "";
        /// <summary>The card itself (tests).</summary>
        public RectTransform Card => _rect;
        public TextMeshProUGUI TitleText => _title;
        public TextMeshProUGUI BodyText => _body;

        public static float DurationFor(string text) =>
            Mathf.Max(MinSeconds, (text ?? "").Length * SecondsPerCharacter) + 2f * FadeSeconds;

        public void Setup()
        {
            if (_group != null) return;
            Build();
            UiScreen.Changed += Layout;
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
            UiScreen.Changed -= Layout;
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
            Layout();
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
            // The HUD's labels grow (kills, gold) and its slots fill: look again now and then.
            if (Time.unscaledTime >= _nextLayout) Layout();
            _shownFor += Time.unscaledDeltaTime;
            float fadeIn = Mathf.Clamp01(_shownFor / FadeSeconds);
            float fadeOut = Mathf.Clamp01((_duration - _shownFor) / FadeSeconds);
            _group.alpha = Mathf.Min(fadeIn, fadeOut);
            if (_shownFor >= _duration) _group.gameObject.SetActive(false);
        }

        private void Build()
        {
            var card = new GameObject("StoryCard", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _rect = (RectTransform)card.transform;
            _rect.SetParent(HudReadability.CanvasRoot(transform), false);
            _rect.SetAsFirstSibling(); // under the menus
            _rect.anchorMin = _rect.anchorMax = _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.sizeDelta = new Vector2(MaxWidth, DesignHeight);
            var background = card.GetComponent<Image>();
            UITheme.ApplySurface(background, ThemedElement.SurfaceRole.StoryCard);
            background.raycastTarget = false;
            _group = card.GetComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;

            _title = Label(_rect, "Title", TitleSize, FontStyles.Bold);
            UITheme.ApplyText(_title, ThemedElement.TextRole.Title); // the title look, like LEVEL UP!
            var title = _title.rectTransform;
            title.anchorMin = new Vector2(0f, 1f);
            title.anchorMax = Vector2.one;
            title.pivot = new Vector2(0.5f, 1f);
            title.offsetMin = new Vector2(Pad, -(Pad + TitleHeight));
            title.offsetMax = new Vector2(-Pad, -Pad);
            _body = Label(_rect, "Story", BodySize, FontStyles.Italic);
            var body = _body.rectTransform;
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = new Vector2(Pad * 1.5f, Pad);
            body.offsetMax = new Vector2(-Pad * 1.5f, -(Pad + TitleHeight));
            card.SetActive(false);
        }

        /// <summary>
        /// Places the card for the canvas it is on: in the free span under the stage label between the HUD's top-left and
        /// top-right items when that span is at least <see cref="MinWidth"/> wide, otherwise across the width under all of
        /// them; as tall as its text needs (at least the approved height when there is room), never past the safe area's
        /// bottom or onto the touch pads there.
        /// </summary>
        public void Layout()
        {
            _nextLayout = Time.unscaledTime + 0.25f;
            if (_rect == null || !(_rect.parent is RectTransform space)) return;
            float floor = Mathf.Max(14f, UiScreen.MinTextUnits);
            foreach (var label in new[] { _title, _body })
            {
                label.fontSizeMin = floor;
                if (label.fontSizeMax < floor) label.fontSizeMax = floor;
            }
            Rect safe = HudClearance.SafeRect(space, Margin);
            if (safe.width <= 0f || safe.height <= 0f) return;
            float gap = HudClearance.Gap;
            var obstacles = Obstacles(space);
            float width = Mathf.Min(MaxWidth, safe.width);
            float cx = safe.center.x;
            // Where its top sits by default: the approved distance on a 1080-high canvas, closer on a short one.
            float top = safe.yMax + Margin - TopOffset * Mathf.Clamp(space.rect.height / UiScreen.ReferenceShortSide, 0.45f, 1f);

            // 1) Between the HUD's left and right columns, under whatever spans the middle (the stage label).
            float left = cx - width * 0.5f, right = cx + width * 0.5f;
            for (int pass = 0; pass <= obstacles.Count; pass++)
            {
                var band = new Rect(left, top - HeightFor(right - left), right - left, HeightFor(right - left));
                bool moved = false;
                foreach (var o in obstacles)
                {
                    if (!HudClearance.Near(band, o, gap)) continue;
                    if (o.xMin < cx && o.xMax > cx) top = o.yMin - gap;   // spans the middle: go under it
                    else if (o.xMax <= cx) left = Mathf.Max(left, o.xMax + gap);   // a left-column item
                    else right = Mathf.Min(right, o.xMin - gap);   // a right-column item
                    moved = true;
                    break;
                }
                if (!moved) break;
            }
            Rect box = new Rect(left, top - HeightFor(right - left), right - left, HeightFor(right - left));
            bool fits = box.width >= MinWidth && box.yMin >= safe.yMin && Free(box, obstacles, gap);
            if (!fits)
            {
                // 2) Too narrow (a phone held upright): across the width, under everything it meets at the top.
                left = cx - width * 0.5f;
                right = cx + width * 0.5f;
                top = safe.yMax + Margin - TopOffset * Mathf.Clamp(space.rect.height / UiScreen.ReferenceShortSide, 0.45f, 1f);
                for (int pass = 0; pass <= obstacles.Count; pass++)
                {
                    var band = new Rect(left, top - HeightFor(width), width, HeightFor(width));
                    Rect? hit = null;
                    foreach (var o in obstacles)
                        if (o.center.y > safe.center.y && HudClearance.Near(band, o, gap)) { hit = o; break; }
                    if (hit == null) break;
                    top = hit.Value.yMin - gap;
                }
                // Never onto the pads at the bottom (the stick, Dash): the card's bottom stays above them.
                float bottom = safe.yMin;
                foreach (var o in obstacles)
                    if (o.center.y <= safe.center.y && o.xMax > left && o.xMin < right) bottom = Mathf.Max(bottom, o.yMax + gap);
                float height = Mathf.Max(0f, Mathf.Min(HeightFor(width), top - bottom));
                box = new Rect(left, top - height, width, height);
            }
            top = Mathf.Min(box.yMax, safe.yMax);
            box = Rect.MinMaxRect(box.xMin, Mathf.Max(box.yMin, safe.yMin), box.xMax, top);
            _rect.anchorMin = _rect.anchorMax = _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.sizeDelta = box.size;
            _rect.anchoredPosition = box.center - space.rect.center;
        }

        /// <summary>The card's height for a width: its title and its story at full size, at least the approved height.</summary>
        private float HeightFor(float width)
        {
            float textWidth = Mathf.Max(1f, width - 3f * Pad);
            float size = _body.fontSize;
            _body.fontSize = _body.fontSizeMax; // measure at full size; auto-size only shrinks it when the room is short
            float body = _body.GetPreferredValues(_body.text, textWidth, 0f).y;
            _body.fontSize = size;
            return Mathf.Max(DesignHeight, Mathf.Ceil(TitleHeight + body + 2f * Pad + 8f));
        }

        private static bool Free(Rect box, List<Rect> obstacles, float gap)
        {
            foreach (var o in obstacles) if (HudClearance.Near(box, o, gap)) return false;
            return true;
        }

        /// <summary>What the card keeps clear of: the HUD's labels (their backings), the item slots and the touch pads.</summary>
        private List<Rect> Obstacles(RectTransform space)
        {
            var list = new List<Rect>();
            float tooBig = Mathf.Min(space.rect.width, space.rect.height) * 0.5f;
            foreach (var backing in space.GetComponentsInChildren<HudBacking>())
            {
                var image = backing.GetComponent<Image>();
                if (image == null || !image.enabled) continue;
                list.Add(HudClearance.RectIn((RectTransform)backing.transform, space));
            }
            foreach (var slots in space.GetComponentsInChildren<InventoryHud>())
                foreach (RectTransform slot in slots.transform)
                    if (slot.gameObject.activeSelf) list.Add(HudClearance.RectIn(slot, space));
            list.AddRange(HudClearance.TouchPads(space));
            // An unlock notice shown at the same moment (the Bestiary opens as Level 3 starts): the card goes under it.
            var unlocks = FindFirstObjectByType<SoulHunter.Gameplay.Core.CharacterUnlockTracker>();
            if (unlocks != null && unlocks.NoticeBox != null && unlocks.NoticeBox.gameObject.activeInHierarchy)
                list.Add(HudClearance.RectIn(unlocks.NoticeBox, space));
            list.RemoveAll(r => r.width > tooBig * 2f || r.height > tooBig); // full-screen containers are not obstacles
            return list;
        }

        private static TextMeshProUGUI Label(RectTransform parent, string name, float size, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = size; // a size first, so TextMeshPro's later Awake keeps these limits
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
