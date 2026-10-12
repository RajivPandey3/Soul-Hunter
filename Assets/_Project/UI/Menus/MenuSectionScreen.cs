using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.UI
{
    /// <summary>
    /// A main menu section screen (Collection, Bestiary, Unlocks, Credits; release item P3-02, 2026-10-09), built at runtime
    /// next to the scene's panels so the MainMenu scene is not edited. Every screen has the same frame: one backing panel
    /// inside the safe area, the title on top with a count beside it (or under it on a narrow screen), Back pinned to the
    /// bottom in a ThemedRow, and the screen's own content in the room between (Body), where long lists scroll. Everything
    /// is styled through the theme (UITheme / ThemedElement), never with colours of its own. The screen reads the save each
    /// time it opens and when the theme changes, and lays itself out again on a rotation or window change (owner,
    /// 2026-10-10: landscape and portrait from the same screens). ui-ux-pro-max: one size per control (Back is the standard
    /// button), visible focus, 4.5:1 text, 12 sp text on touch, 48 dp targets, state in words; frontend-design: one bold
    /// element per screen (the Collection's "?" grid), the rest quiet.
    /// </summary>
    public abstract class MenuSectionScreen : MonoBehaviour
    {
        /// <summary>Space between the safe area's edge and the screen's backing, and inside the backing (canvas units).</summary>
        public const float Margin = 32f, Pad = 24f;
        /// <summary>Header heights: the title line, and the count line when it sits under the title (narrow screens).</summary>
        public const float TitleHeight = 72f, CountHeight = 44f;
        /// <summary>Under this inner width the count goes under the title instead of beside it.</summary>
        public const float WideHeaderWidth = 900f;

        protected RectTransform Panel { get; private set; }
        /// <summary>The screen's backing window, inside the safe area.</summary>
        public RectTransform Frame { get; private set; }
        /// <summary>The room between the header and the Back row: each screen builds its content here.</summary>
        public RectTransform Body { get; private set; }
        /// <summary>The row the Back button sits in, pinned to the bottom of the window.</summary>
        public RectTransform Footer { get; private set; }
        public Button BackButton { get; private set; }
        public TextMeshProUGUI TitleText { get; private set; }
        public TextMeshProUGUI CountText { get; private set; }
        protected TMP_FontAsset Font { get; private set; }

        /// <summary>Every text the screen shows, with the colour of the surface it sits on (tests check 4.5:1).</summary>
        public IEnumerable<(TMP_Text Text, Color Surface)> ReadableTexts()
        {
            foreach (var pair in _readable)
                if (pair.Text != null && pair.Text.isActiveAndEnabled && !string.IsNullOrWhiteSpace(pair.Text.text))
                    yield return (pair.Text, pair.Surface());
        }

        /// <summary>Every text the screen made itself (button labels excluded: the theme sizes those).</summary>
        public IEnumerable<TMP_Text> OwnTexts()
        {
            foreach (var f in _floors) if (f.Text != null) yield return f.Text;
        }

        /// <summary>The scroll areas the screen holds (tests).</summary>
        public IReadOnlyList<ScrollRect> Scrolls => _scrolls;

        private readonly List<(TMP_Text Text, Func<Color> Surface)> _readable = new List<(TMP_Text, Func<Color>)>();
        // Each text's own smallest size: on a touch screen the floor (UiScreen.MinTextUnits, 12 sp) may lift it.
        private readonly List<(TMP_Text Text, float Min, float Size)> _floors = new List<(TMP_Text, float, float)>();
        private readonly List<ScrollRect> _scrolls = new List<ScrollRect>();
        private Vector2 _lastFrame = new Vector2(-1f, -1f);
        private float _lastFooter = -1f;
        private bool _built;

        /// <summary>Builds a screen of type <typeparamref name="T"/> beside <paramref name="sibling"/>, hidden until opened.</summary>
        public static T Create<T>(GameObject sibling, string name, string title, Button backTemplate, UnityEngine.Events.UnityAction back)
            where T : MenuSectionScreen
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.SetActive(false); // built hidden: OnEnable (which reads the save) runs only once the screen is opened
            var rect = (RectTransform)go.transform;
            rect.SetParent(sibling.transform.parent, false);
            rect.SetSiblingIndex(sibling.transform.GetSiblingIndex() + 1);
            Anchor(rect, 0f, 0f, 1f, 1f);
            var screen = go.AddComponent<T>();
            screen.Setup(title, backTemplate, back);
            return screen;
        }

        private void Setup(string title, Button backTemplate, UnityEngine.Events.UnityAction back)
        {
            Panel = (RectTransform)transform;
            var font = backTemplate != null ? backTemplate.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            Font = font != null ? font.font : null;

            // The window sits in the safe area (notch, gesture bar) with a fixed margin.
            var safe = NewRect("SectionContent", Panel);
            SafeAreaFitter.Fit(safe);
            var backing = new GameObject("SectionBacking", typeof(RectTransform), typeof(Image));
            Frame = (RectTransform)backing.transform;
            Frame.SetParent(safe, false);
            Anchor(Frame, 0f, 0f, 1f, 1f);
            Frame.offsetMin = new Vector2(Margin, Margin);
            Frame.offsetMax = new Vector2(-Margin, -Margin);
            var backingImage = backing.GetComponent<Image>();
            UITheme.ApplyPanel(backingImage, 0.93f);
            backingImage.raycastTarget = true; // clicks never reach the menu behind

            TitleText = MakeText("Title", Frame, 0f, 1f, 1f, 1f, title, 56f, TextAlignmentOptions.Center, ThemedElement.TextRole.Title,
                () => UITheme.Panel);
            CountText = MakeText("Count", Frame, 0f, 1f, 1f, 1f, "", 34f, TextAlignmentOptions.MidlineRight, ThemedElement.TextRole.Accent,
                () => UITheme.Panel);

            // Back, the button that ends the screen, pinned to the bottom edge in a ThemedRow (it wraps, never shrinks).
            Footer = NewRect("BackRow", Frame);
            Footer.anchorMin = Footer.anchorMax = new Vector2(0.5f, 0f);
            Footer.pivot = new Vector2(0.5f, 0f);
            Footer.anchoredPosition = new Vector2(0f, Pad);
            Footer.sizeDelta = new Vector2(UITheme.Layout.ButtonWidth, UITheme.Layout.ButtonHeight);
            if (backTemplate != null)
            {
                BackButton = Instantiate(backTemplate, Footer);
                BackButton.name = "Back_Button";
                BackButton.gameObject.SetActive(true);
                BackButton.onClick = new Button.ButtonClickedEvent(); // drop the template's listeners
                BackButton.onClick.AddListener(back);
                foreach (var label in BackButton.GetComponentsInChildren<TextMeshProUGUI>(true)) label.text = "Back";
                var backRect = (RectTransform)BackButton.transform;
                backRect.anchorMin = backRect.anchorMax = backRect.pivot = new Vector2(0.5f, 0.5f);
                backRect.anchoredPosition = Vector2.zero;
                UITheme.ApplyButton(BackButton, BackButton.targetGraphic as Image, UITheme.ButtonKind.Normal, UITheme.IconSlot.Back);
            }
            Footer.gameObject.AddComponent<ThemedRow>();

            Body = NewRect("Body", Frame);
            Build();
            _built = true;
            Relayout();
        }

        /// <summary>Builds the screen's own content (once), inside <see cref="Body"/>.</summary>
        protected abstract void Build();

        /// <summary>Shows the save's current state.</summary>
        public abstract void Refresh();

        protected virtual void OnEnable()
        {
            if (!_built) return;
            UiThemeAsset.Changed += Refresh;
            UiScreen.Changed += Relayout;
            Refresh();
            Relayout(); // after Refresh: the count line is part of the header once it has text
        }

        protected virtual void OnDisable()
        {
            UiThemeAsset.Changed -= Refresh;
            UiScreen.Changed -= Relayout;
        }

        private void LateUpdate()
        {
            if (!_built || Frame == null) return;
            if (Frame.rect.size != _lastFrame || !Mathf.Approximately(FooterHeight(), _lastFooter)) Relayout();
        }

        private float FooterHeight() => Footer != null ? Footer.rect.height * Footer.localScale.y : 0f;

        /// <summary>
        /// Places the header, the Back row and the body for the window's size, and lifts small text to the touch floor. Runs
        /// when the screen opens, on a rotation or window change (UiScreen.Changed) and when the window's size changes. It
        /// only moves things, so nothing is rebuilt and the screen keeps its state (the chosen entry, the scroll).
        /// </summary>
        public void Relayout()
        {
            if (Frame == null || TitleText == null) return;
            _lastFrame = Frame.rect.size;
            _lastFooter = FooterHeight();
            float inner = Frame.rect.width - 2f * Pad;
            bool wide = inner >= WideHeaderWidth;
            float top = Pad;
            // Wide: the title in the middle of the top line, the count on its right. Narrow: the count on its own line.
            PlaceTop(TitleText.rectTransform, top, TitleHeight, wide ? 0.27f : 0f, wide ? 0.27f : 0f);
            if (wide)
            {
                PlaceTop(CountText.rectTransform, top, TitleHeight, 0.74f, 0f);
                CountText.alignment = TextAlignmentOptions.MidlineRight;
                top += TitleHeight;
            }
            else
            {
                top += TitleHeight;
                PlaceTop(CountText.rectTransform, top, CountHeight, 0f, 0f);
                CountText.alignment = TextAlignmentOptions.Center;
                if (!string.IsNullOrEmpty(CountText.text)) top += CountHeight;
            }
            float gap = UITheme.Layout.ButtonGap;
            Body.anchorMin = Vector2.zero;
            Body.anchorMax = Vector2.one;
            Body.pivot = new Vector2(0.5f, 0.5f);
            Body.offsetMin = new Vector2(Pad, Pad + _lastFooter + gap);
            Body.offsetMax = new Vector2(-Pad, -(top + gap));
            ApplyTextFloor();
            foreach (var scroll in _scrolls) ResponsiveScroll.SizeTouchBar(scroll); // the bar's hit area follows the touch target
            OnRelayout();
        }

        /// <summary>A screen's own layout step after the frame is placed (entry heights for touch, and so on).</summary>
        protected virtual void OnRelayout() { }

        /// <summary>A strip <paramref name="height"/> units tall, <paramref name="fromTop"/> under the window's top, inset by shares of the width.</summary>
        private static void PlaceTop(RectTransform rect, float fromTop, float height, float leftShare, float rightShare)
        {
            rect.anchorMin = new Vector2(leftShare, 1f);
            rect.anchorMax = new Vector2(1f - rightShare, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(Pad, -(fromTop + height));
            rect.offsetMax = new Vector2(-Pad, -fromTop);
        }

        /// <summary>On a touch screen no text is under 12 sp (UiScreen.MinTextUnits); on a PC each keeps its own sizes.</summary>
        private void ApplyTextFloor()
        {
            float floor = UiScreen.MinTextUnits;
            foreach (var (text, min, size) in _floors)
            {
                if (text == null) continue;
                if (text.enableAutoSizing) text.fontSizeMin = Mathf.Max(min, floor);
                else text.fontSize = Mathf.Max(size, floor);
                if (text.fontSizeMax < text.fontSizeMin) text.fontSizeMax = text.fontSizeMin;
            }
        }

        /// <summary>The save this menu shows (the MainMenu's SaveService).</summary>
        protected static GameData Save
        {
            get
            {
                SaveService save = null;
                GameServices.Instance?.TryGet(out save);
                return save != null ? save.CurrentData : null;
            }
        }

        // ---------------------------------------------------------------- building helpers

        protected TextMeshProUGUI MakeText(string name, RectTransform parent, float xMin, float yMin, float xMax, float yMax, string text,
            float maxSize, TextAlignmentOptions alignment, ThemedElement.TextRole role, Func<Color> surface)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Anchor(rect, xMin, yMin, xMax, yMax);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (Font != null) label.font = Font;
            label.fontSize = maxSize; // a size before auto-size (TMP's defaults would replace the limits)
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Min(16f, maxSize);
            label.fontSizeMax = maxSize;
            label.alignment = alignment;
            label.text = text;
            label.raycastTarget = false;
            UITheme.ApplyText(label, role);
            if (surface != null) _readable.Add((label, surface));
            _floors.Add((label, label.fontSizeMin, maxSize));
            return label;
        }

        /// <summary>Records a text's own sizes again (after a screen changed its auto-size limits).</summary>
        protected void RememberSizes(TMP_Text text)
        {
            for (int i = 0; i < _floors.Count; i++)
                if (_floors[i].Text == text) _floors[i] = (text, text.fontSizeMin, text.fontSize);
        }

        /// <summary>
        /// A text that takes the height it needs (one fixed size, wrapped), for content in a vertical layout: it never
        /// shrinks, so on a narrow screen it grows taller and its list scrolls.
        /// </summary>
        protected TextMeshProUGUI FlowText(RectTransform parent, string name, float size, TextAlignmentOptions alignment,
            ThemedElement.TextRole role, Func<Color> surface)
        {
            var text = MakeText(name, parent, 0f, 0f, 1f, 1f, "", size, alignment, role, surface);
            text.enableAutoSizing = false;
            text.fontSize = size;
            text.textWrappingMode = TextWrappingModes.Normal;
            RememberSizes(text);
            return text;
        }

        protected static Image MakeImage(string name, RectTransform parent, Sprite sprite)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.enabled = sprite != null;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A themed row surface (info boxes, list rows).</summary>
        protected static RectTransform MakeBox(string name, RectTransform parent, float xMin, float yMin, float xMax, float yMax)
        {
            var box = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            box.SetParent(parent, false);
            Anchor(box, xMin, yMin, xMax, yMax);
            var image = box.GetComponent<Image>();
            UITheme.ApplySurface(image, ThemedElement.SurfaceRole.Row);
            image.raycastTarget = false;
            return box;
        }

        /// <summary>An empty rect filling <paramref name="parent"/>.</summary>
        protected static RectTransform NewRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Anchor(rect, 0f, 0f, 1f, 1f);
            return rect;
        }

        /// <summary>Stacks a scroll content's children top down, each as tall as it needs, across the full width.</summary>
        protected static VerticalLayoutGroup Column(RectTransform content, float spacing, RectOffset padding = null)
        {
            var list = content.gameObject.AddComponent<VerticalLayoutGroup>();
            list.spacing = spacing;
            list.childControlWidth = list.childControlHeight = true;
            list.childForceExpandWidth = true;
            list.childForceExpandHeight = false;
            if (padding != null) list.padding = padding;
            return list;
        }

        /// <summary>
        /// A vertical scroll area with a themed scrollbar (groove and knob). The scrollbar is selectable, so keyboard and
        /// controller players can scroll a list that holds no buttons; the selected entry is kept in view. On a touch screen
        /// the bar's hit area is the 48 dp touch target while its groove stays drawn thin, and the list also scrolls by
        /// dragging anywhere on it.
        /// </summary>
        protected RectTransform MakeScroll(string name, RectTransform parent, float xMin, float yMin, float xMax, float yMax, out ScrollRect scroll)
        {
            var area = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            area.SetParent(parent, false);
            Anchor(area, xMin, yMin, xMax, yMax);
            scroll = ResponsiveScroll.Create(area, "Viewport", "Content", out var content, ResponsiveScroll.Bar.Touch,
                fitContentHeight: true, keepSelectedInView: true);
            _scrolls.Add(scroll);
            return content;
        }

        protected static void Anchor(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        /// <summary>A text inside a list entry, registered for the readability check against <paramref name="surface"/>.</summary>
        protected TextMeshProUGUI EntryText(RectTransform parent, string name, float maxSize, TextAlignmentOptions alignment,
            ThemedElement.TextRole role, Func<Color> surface)
        {
            var text = MakeText(name, parent, 0f, 0f, 1f, 1f, "", maxSize, alignment, role, surface);
            text.fontSizeMin = Mathf.Min(14f, maxSize);
            RememberSizes(text);
            return text;
        }

        /// <summary>A tappable entry's height: its design height, never under the 48 dp touch target on a touch screen.</summary>
        protected static float TouchHeight(float design) => Mathf.Max(design, UiScreen.MinTouchUnits);
    }

    /// <summary>Calls back when its selectable gets keyboard/controller focus (a list entry shows its details then).</summary>
    public sealed class FocusCallback : MonoBehaviour, ISelectHandler
    {
        public Action Selected;
        public void OnSelect(BaseEventData eventData) => Selected?.Invoke();
    }
}
