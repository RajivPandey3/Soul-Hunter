using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// The game's one scroll view (owner, 2026-10-10: "0 duplicacy or parallel implementation"). Every screen built in code
    /// that has more content than room scrolls through this: a masked viewport, an optional see-through drag surface (so a
    /// drag between two entries still scrolls), a content rect anchored to the top and stretched across, and optionally the
    /// theme's groove-and-knob scrollbar. <see cref="KeepInView"/> scrolls a control into the window, <see cref="IsInView"/>
    /// tells whether it is there, and <see cref="KeepSelectedInView"/> follows keyboard and controller focus.
    /// No screen adds a ScrollRect itself (ResponsiveUiTests guards it).
    /// </summary>
    public static class ResponsiveScroll
    {
        /// <summary>The scrollbar a scroll view shows.</summary>
        public enum Bar
        {
            /// <summary>No bar: the list scrolls by dragging, the wheel and focus.</summary>
            None,
            /// <summary>A thin groove and knob as a quiet "there is more" mark; never a focus stop (the list's own controls are).</summary>
            Mark,
            /// <summary>
            /// A selectable bar (keyboard and controller players can scroll a list that holds no buttons) that shows the
            /// theme's focus colour. On a touch screen its hit area is the touch target while the groove stays drawn thin;
            /// call <see cref="SizeTouchBar"/> again when the screen changes.
            /// </summary>
            Touch,
        }

        /// <summary>The Mark bar's drawn width and the room the viewport leaves for it (canvas units).</summary>
        public const float MarkWidth = 14f, MarkRoom = 22f;
        /// <summary>The Touch bar's drawn groove width and the gap between the bar's hit area and the list (canvas units).</summary>
        public const float TouchGroove = 18f, TouchGap = 10f;
        /// <summary>The mouse wheel's step (canvas units per notch) when a screen does not choose its own.</summary>
        public const float DefaultSensitivity = 40f;

        /// <summary>
        /// Makes <paramref name="area"/> scroll. With a <paramref name="viewportName"/> the masked viewport is a new child that
        /// fills the area (less the bar's room) and the ScrollRect sits on the area; with none, the area itself is the masked
        /// viewport and holds the ScrollRect. The content is a new child of the viewport, anchored to the top and stretched
        /// across (top-left when <paramref name="horizontal"/>); <paramref name="fitContentHeight"/> sizes it by its layout.
        /// A scrollbar needs a viewport child (it sits beside the viewport in the area).
        /// </summary>
        public static ScrollRect Create(RectTransform area, string viewportName, string contentName, out RectTransform content,
            Bar bar = Bar.None, float sensitivity = DefaultSensitivity, bool dragSurface = true, bool fitContentHeight = false,
            bool keepSelectedInView = false, bool horizontal = false)
        {
            if (area == null) throw new ArgumentNullException(nameof(area));
            if (bar != Bar.None && string.IsNullOrEmpty(viewportName))
                throw new ArgumentException("A scrollbar sits beside its viewport: give the scroll view a viewport child.", nameof(viewportName));

            RectTransform viewport;
            if (string.IsNullOrEmpty(viewportName)) viewport = area;
            else
            {
                viewport = NewRect(viewportName, area);
                viewport.anchorMin = Vector2.zero;
                viewport.anchorMax = Vector2.one;
                viewport.pivot = new Vector2(0.5f, 0.5f);
                viewport.offsetMin = viewport.offsetMax = Vector2.zero;
            }
            if (viewport.GetComponent<RectMask2D>() == null) viewport.gameObject.AddComponent<RectMask2D>();
            if (dragSurface && viewport.GetComponent<Graphic>() == null)
            {
                // A see-through raycast target, so a drag anywhere in the list (also between entries) scrolls it.
                var catcher = viewport.gameObject.AddComponent<Image>();
                catcher.color = Color.clear;
                catcher.raycastTarget = true;
            }

            content = NewRect(contentName, viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = horizontal ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
            content.pivot = horizontal ? new Vector2(0f, 1f) : new Vector2(0.5f, 1f);
            content.offsetMin = content.offsetMax = Vector2.zero;
            if (fitContentHeight)
            {
                var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                if (horizontal) fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            Scrollbar scrollbar = bar == Bar.None ? null : MakeBar(area, BarName(viewportName), bar);

            var scroll = area.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = horizontal;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.scrollSensitivity = sensitivity;
            if (scrollbar != null)
            {
                scroll.verticalScrollbar = scrollbar;
                scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
                if (bar == Bar.Touch) SizeTouchBar(scroll);
                else
                {
                    viewport.offsetMin = Vector2.zero;
                    viewport.offsetMax = new Vector2(-MarkRoom, 0f);
                }
            }
            if (keepSelectedInView) area.gameObject.AddComponent<KeepSelectedInView>().Scroll = scroll;
            return scroll;
        }

        /// <summary>
        /// A Touch bar's hit area: the drawn groove on a PC, the touch target on a touch screen, and the viewport's room for
        /// it. Call it again when the screen changes (<see cref="UiScreen.Changed"/>).
        /// </summary>
        public static void SizeTouchBar(ScrollRect scroll)
        {
            if (scroll == null || scroll.verticalScrollbar == null || scroll.viewport == null) return;
            var bar = (RectTransform)scroll.verticalScrollbar.transform;
            float width = Mathf.Max(TouchGroove, UiScreen.MinTouchUnits);
            bar.sizeDelta = new Vector2(width, 0f);
            scroll.viewport.offsetMin = Vector2.zero;
            scroll.viewport.offsetMax = new Vector2(-(width + TouchGap), 0f); // room for the scrollbar
        }

        /// <summary>
        /// Scrolls <paramref name="scroll"/> just enough to show <paramref name="target"/> (when it is in the list), plus
        /// <paramref name="margin"/> of air; with <paramref name="wholeRow"/> the whole row holding it (the content's child)
        /// is brought into view. The content never moves past its ends.
        /// </summary>
        public static void KeepInView(ScrollRect scroll, RectTransform target, float margin = 0f, bool wholeRow = false)
        {
            if (scroll == null || target == null || scroll.content == null) return;
            var content = scroll.content;
            if (target == content || !target.IsChildOf(content)) return;
            var viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
            var item = target;
            if (wholeRow) while (item.parent != null && item.parent != content) item = (RectTransform)item.parent;
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, item);
            Rect view = viewport.rect;
            float shift = 0f; // how far the content moves up (+) or down (-)
            if (bounds.max.y > view.yMax) shift = -(bounds.max.y - view.yMax) - margin; // above the window: bring it down
            else if (bounds.min.y < view.yMin) shift = view.yMin - bounds.min.y + margin; // below the window: bring it up
            if (Mathf.Abs(shift) < 0.5f) return;
            float maxScroll = Mathf.Max(0f, content.rect.height - view.height);
            scroll.StopMovement();
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, Mathf.Clamp(content.anchoredPosition.y + shift, 0f, maxScroll));
        }

        /// <summary>True when <paramref name="target"/> lies wholly inside the viewport (on screen, not scrolled away).</summary>
        public static bool IsInView(ScrollRect scroll, RectTransform target)
        {
            if (scroll == null || target == null) return false;
            var viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, target);
            Rect view = viewport.rect;
            return bounds.min.y >= view.yMin - 0.5f && bounds.max.y <= view.yMax + 0.5f
                && bounds.min.x >= view.xMin - 0.5f && bounds.max.x <= view.xMax + 0.5f;
        }

        /// <summary>A new rect under <paramref name="parent"/>, on its layer (a screen-space camera canvas draws by layer).</summary>
        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static string BarName(string viewportName) =>
            viewportName.Contains("Viewport") ? viewportName.Replace("Viewport", "Scrollbar") : viewportName + "Scrollbar";

        private static Scrollbar MakeBar(RectTransform area, string name, Bar style)
        {
            var bar = NewRect(name, area);
            var barGo = bar.gameObject;
            barGo.AddComponent<Image>();
            barGo.AddComponent<Scrollbar>();
            bar.anchorMin = new Vector2(1f, 0f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(1f, 0.5f);
            bar.anchoredPosition = Vector2.zero;
            var scrollbar = barGo.GetComponent<Scrollbar>();
            RectTransform slide;
            if (style == Bar.Mark)
            {
                bar.sizeDelta = new Vector2(MarkWidth, 0f);
                UITheme.ApplySurface(barGo.GetComponent<Image>(), ThemedElement.SurfaceRole.Groove);
                slide = Strip("SlidingArea", bar, Vector2.zero, Vector2.one, 0f);
            }
            else
            {
                // The bar's own image takes the presses and draws nothing; the groove and the knob are drawn thin inside it.
                barGo.GetComponent<Image>().color = Color.clear;
                var groove = Strip("Groove", bar, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), TouchGroove);
                var grooveImage = groove.gameObject.AddComponent<Image>();
                UITheme.ApplySurface(grooveImage, ThemedElement.SurfaceRole.Groove);
                grooveImage.raycastTarget = false;
                slide = Strip("SlidingArea", bar, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), TouchGroove);
            }
            var handle = NewRect("Handle", slide);
            handle.gameObject.AddComponent<Image>();
            handle.anchorMin = Vector2.zero;
            handle.anchorMax = Vector2.one;
            handle.pivot = new Vector2(0.5f, 0.5f);
            handle.offsetMin = handle.offsetMax = Vector2.zero;
            var handleImage = handle.GetComponent<Image>();
            UITheme.ApplySurface(handleImage, ThemedElement.SurfaceRole.Knob);
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            if (style == Bar.Mark)
            {
                // A mark, not a control: it is never a focus stop (the list's own controls are) and keeps the theme's knob.
                scrollbar.transition = Selectable.Transition.None;
                scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
            }
            else
            {
                var colors = scrollbar.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = UITheme.Focus;
                colors.selectedColor = UITheme.Focus; // the focused scrollbar shows the theme's focus colour
                colors.pressedColor = UITheme.Pressed;
                scrollbar.colors = colors;
            }
            return scrollbar;
        }

        /// <summary>A strip inside the bar: the whole bar (width 0, stretched), or a centred column of <paramref name="width"/>.</summary>
        private static RectTransform Strip(string name, RectTransform bar, Vector2 min, Vector2 max, float width)
        {
            var strip = NewRect(name, bar);
            strip.anchorMin = min;
            strip.anchorMax = max;
            strip.pivot = new Vector2(0.5f, 0.5f);
            if (width > 0f)
            {
                strip.sizeDelta = new Vector2(width, 0f);
                strip.anchoredPosition = Vector2.zero;
            }
            else strip.offsetMin = strip.offsetMax = Vector2.zero;
            return strip;
        }
    }

    /// <summary>
    /// Keeps the EventSystem's selected control in its scroll view's window (keyboard and controller focus; ui-ux-pro-max:
    /// focus not obscured). It acts only when the selection changes, so a list the player scrolls by hand stays put.
    /// </summary>
    public sealed class KeepSelectedInView : MonoBehaviour
    {
        public ScrollRect Scroll;
        /// <summary>Air kept between the control and the window's edge when it is scrolled in (canvas units).</summary>
        public float Margin;
        /// <summary>Bring the whole row holding the control (the content's child) into view.</summary>
        public bool WholeRow;
        private GameObject _last;

        private void LateUpdate()
        {
            var system = EventSystem.current;
            var selected = system != null ? system.currentSelectedGameObject : null;
            if (selected == _last) return;
            _last = selected;
            if (selected == null || Scroll == null || Scroll.content == null || !selected.transform.IsChildOf(Scroll.content)) return;
            Canvas.ForceUpdateCanvases(); // a list laid out this frame (a new page, a rotation) is measured as drawn
            ResponsiveScroll.KeepInView(Scroll, selected.transform as RectTransform, Margin, WholeRow);
        }
    }
}
