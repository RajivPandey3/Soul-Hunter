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
    /// next to the scene's panels so the MainMenu scene is not edited. Every screen has the Power Up shop's frame: one
    /// backing panel, Back on the header's left, the title in the middle and a count on the right; everything is styled
    /// through the theme (UITheme / ThemedElement), never with colours of its own. The screen reads the save each time it
    /// opens and when the theme changes. ui-ux-pro-max: one size per control (Back is the standard button), visible focus,
    /// 4.5:1 text, state in words; frontend-design: one bold element per screen (the Collection's "?" grid), the rest quiet.
    /// </summary>
    public abstract class MenuSectionScreen : MonoBehaviour
    {
        protected RectTransform Panel { get; private set; }
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

        private readonly List<(TMP_Text Text, Func<Color> Surface)> _readable = new List<(TMP_Text, Func<Color>)>();

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

            var backing = new GameObject("SectionBacking", typeof(RectTransform), typeof(Image));
            var backingRect = (RectTransform)backing.transform;
            backingRect.SetParent(Panel, false);
            Anchor(backingRect, 0.03f, 0.05f, 0.97f, 0.95f);
            var backingImage = backing.GetComponent<Image>();
            UITheme.ApplyPanel(backingImage, 0.93f);
            backingImage.raycastTarget = true; // clicks never reach the menu behind

            if (backTemplate != null)
            {
                BackButton = Instantiate(backTemplate, Panel);
                BackButton.name = "Back_Button";
                BackButton.gameObject.SetActive(true);
                BackButton.onClick = new Button.ButtonClickedEvent(); // drop the template's listeners
                BackButton.onClick.AddListener(back);
                foreach (var label in BackButton.GetComponentsInChildren<TextMeshProUGUI>(true)) label.text = "Back";
                var backRect = (RectTransform)BackButton.transform;
                backRect.anchorMin = backRect.anchorMax = new Vector2(0.03f, 0.885f);
                backRect.pivot = new Vector2(0.5f, 0.5f);
                backRect.anchoredPosition = new Vector2(24f + UITheme.Layout.ButtonWidth * 0.5f, 0f);
                UITheme.ApplyButton(BackButton, BackButton.targetGraphic as Image, UITheme.ButtonKind.Normal, UITheme.IconSlot.Back);
            }
            TitleText = MakeText("Title", Panel, 0.3f, 0.84f, 0.7f, 0.93f, title, 56f, TextAlignmentOptions.Center, ThemedElement.TextRole.Title,
                () => UITheme.Panel);
            CountText = MakeText("Count", Panel, 0.68f, 0.85f, 0.95f, 0.92f, "", 34f, TextAlignmentOptions.MidlineRight, ThemedElement.TextRole.Accent,
                () => UITheme.Panel);
            Build();
            _built = true;
        }

        private bool _built;

        /// <summary>Builds the screen's own content (once).</summary>
        protected abstract void Build();

        /// <summary>Shows the save's current state.</summary>
        public abstract void Refresh();

        protected virtual void OnEnable()
        {
            if (!_built) return;
            UiThemeAsset.Changed += Refresh;
            Refresh();
        }

        protected virtual void OnDisable() => UiThemeAsset.Changed -= Refresh;

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
            return label;
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

        /// <summary>
        /// A vertical scroll area with a themed scrollbar (groove and knob). The scrollbar is selectable, so keyboard and
        /// controller players can scroll a list that holds no buttons; the selected entry is kept in view.
        /// </summary>
        protected RectTransform MakeScroll(string name, RectTransform parent, float xMin, float yMin, float xMax, float yMax, out ScrollRect scroll)
        {
            var area = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            area.SetParent(parent, false);
            Anchor(area, xMin, yMin, xMax, yMax);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            viewport.SetParent(area, false);
            Anchor(viewport, 0f, 0f, 1f, 1f);
            viewport.offsetMax = new Vector2(-28f, 0f); // room for the scrollbar

            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var barGo = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            var bar = (RectTransform)barGo.transform;
            bar.SetParent(area, false);
            bar.anchorMin = new Vector2(1f, 0f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(1f, 0.5f);
            bar.sizeDelta = new Vector2(18f, 0f);
            bar.anchoredPosition = Vector2.zero;
            UITheme.ApplySurface(barGo.GetComponent<Image>(), ThemedElement.SurfaceRole.Groove);
            var handleArea = new GameObject("SlidingArea", typeof(RectTransform)).GetComponent<RectTransform>();
            handleArea.SetParent(bar, false);
            Anchor(handleArea, 0f, 0f, 1f, 1f);
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            handle.SetParent(handleArea, false);
            Anchor(handle, 0f, 0f, 1f, 1f);
            var handleImage = handle.GetComponent<Image>();
            UITheme.ApplySurface(handleImage, ThemedElement.SurfaceRole.Knob);
            var scrollbar = barGo.GetComponent<Scrollbar>();
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            var colors = scrollbar.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = UITheme.Focus;
            colors.selectedColor = UITheme.Focus; // the focused scrollbar shows the theme's focus colour
            colors.pressedColor = UITheme.Pressed;
            scrollbar.colors = colors;

            scroll = area.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            area.gameObject.AddComponent<KeepSelectedInView>().Scroll = scroll;
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
            return text;
        }
    }

    /// <summary>Calls back when its selectable gets keyboard/controller focus (a list entry shows its details then).</summary>
    public sealed class FocusCallback : MonoBehaviour, ISelectHandler
    {
        public Action Selected;
        public void OnSelect(BaseEventData eventData) => Selected?.Invoke();
    }

    /// <summary>Scrolls a list so the focused entry is in view (keyboard and controller).</summary>
    public sealed class KeepSelectedInView : MonoBehaviour
    {
        public ScrollRect Scroll;
        private GameObject _last;

        private void LateUpdate()
        {
            var system = EventSystem.current;
            var selected = system != null ? system.currentSelectedGameObject : null;
            if (selected == null || selected == _last || Scroll == null || Scroll.content == null) return;
            _last = selected;
            if (!selected.transform.IsChildOf(Scroll.content)) return;
            var viewport = Scroll.viewport;
            var item = (RectTransform)selected.transform;
            var corners = new Vector3[4];
            item.GetWorldCorners(corners);
            float bottom = viewport.InverseTransformPoint(corners[0]).y, top = viewport.InverseTransformPoint(corners[1]).y;
            var view = viewport.rect;
            var position = Scroll.content.anchoredPosition;
            if (top > view.yMax) position.y -= top - view.yMax;
            else if (bottom < view.yMin) position.y += view.yMin - bottom;
            else return;
            float maxY = Mathf.Max(0f, Scroll.content.rect.height - view.height);
            position.y = Mathf.Clamp(position.y, 0f, maxY);
            Scroll.content.anchoredPosition = position;
        }
    }
}
