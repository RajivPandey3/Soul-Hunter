using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SoulHunter.UI
{
    /// <summary>
    /// Places the main menu's title and buttons inside the open area of the background frame art (owner: "main menu panel
    /// thik karo", 2026-10-03). The frame is drawn into the background picture and the picture keeps its aspect, so the
    /// layout follows where the picture is actually drawn, and refits when the window changes size. Before, the title and
    /// buttons were centred on the screen while the frame's centre ornament sits right of centre, and Quit fell onto the
    /// frame's bottom border (off the screen in a short window). The look (dark crimson buttons, red edge, orange focus) is
    /// Soul Hunter's own; the wiki's Main_menu page lists what the menu holds, not how it looks.
    /// </summary>
    public sealed class MainMenuLayout : MonoBehaviour
    {
        // The frame's open area in the background picture (fractions, measured on the 1672x941 art, bottom-up):
        // inner edges about x 0.27-0.91, y 0.10-0.60; the centre ornaments sit at x 0.562.
        public const float FrameCentreX = 0.562f;
        public const float TitleTop = 0.595f, TitleBottom = 0.465f, TitleHalfWidth = 0.24f;
        public const float ButtonsTop = 0.43f, ButtonsBottom = 0.14f; // the open area under the logo; buttons centre in it


        private Image _background;
        private RectTransform _area;
        private TMP_Text _title;
        private Button[] _buttons;
        private Vector2 _lastScreen;
        private Rect _lastPicture;

        /// <summary>The frame's open area (a full-screen-anchored rect fitted to the drawn picture), for tests.</summary>
        public RectTransform Area => _area;
        public TMP_Text Title => _title;
        public Button[] Buttons => _buttons;

        public static MainMenuLayout Attach(GameObject homePanel, Button[] buttons)
        {
            if (homePanel == null) return null;
            var layout = homePanel.GetComponent<MainMenuLayout>();
            if (layout == null) layout = homePanel.AddComponent<MainMenuLayout>();
            layout.Setup(buttons);
            return layout;
        }

        private void Setup(Button[] buttons)
        {
            _buttons = buttons;
            _background = FindBackground();
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
                if (text.name == "GameLogo_Text") { _title = text; break; }

            var go = new GameObject("FrameArea", typeof(RectTransform));
            _area = (RectTransform)go.transform;
            _area.SetParent(transform, false);
            _area.SetAsFirstSibling();

            if (_title != null)
            {
                // The two logo sprites on one centred line (they were on two lines pushed apart with spaces).
                _title.text = "<sprite name=\"sprite_5_0\"> <sprite name=\"sprite_4_0\">";
                _title.alignment = TextAlignmentOptions.Center;
                _title.enableAutoSizing = true;
                _title.fontSizeMin = 10f;
                _title.fontSizeMax = 200f;
                _title.rectTransform.SetParent(_area, false);
                Place(_title.rectTransform, FrameCentreX - TitleHalfWidth, TitleBottom, FrameCentreX + TitleHalfWidth, TitleTop);
            }

            int index = 0;
            foreach (var button in _buttons)
            {
                if (button == null) continue;
                button.transform.SetParent(_area, false);
                Style(button, index++);
            }
            PlaceButtons();
            SoulHunter.Gameplay.UI.UiThemeAsset.Changed += PlaceButtons;
            Fit();
        }

        private void OnDestroy() => SoulHunter.Gameplay.UI.UiThemeAsset.Changed -= PlaceButtons;

        /// <summary>
        /// A column of the theme's standard buttons with the theme's gap, centred on the frame's ornament in the open area
        /// under the logo (one button size in the whole game, owner 2026-10-07; the frame no longer stretches them).
        /// </summary>
        private void PlaceButtons()
        {
            if (this == null || _buttons == null) return;
            var layout = SoulHunter.Gameplay.UI.UITheme.Layout;
            int count = 0;
            foreach (var button in _buttons) if (button != null) count++;
            float step = layout.ButtonHeight + layout.ButtonGap;
            int index = 0;
            foreach (var button in _buttons)
            {
                if (button == null) continue;
                var rect = (RectTransform)button.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(FrameCentreX, (ButtonsTop + ButtonsBottom) * 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(layout.ButtonWidth, layout.ButtonHeight);
                rect.anchoredPosition = new Vector2(0f, ((count - 1) * 0.5f - index) * step);
                index++;
            }
        }

        private void LateUpdate()
        {
            var screen = new Vector2(Screen.width, Screen.height);
            if (screen != _lastScreen || (_background != null && PictureRect() != _lastPicture)) Fit();
            CentreTitle();
        }

        /// <summary>
        /// The logo sprites carry their own side bearings, and the logo auto-sizes after layout: keep what is drawn (its
        /// text bounds, in its own space) on the box centre. The bounds do not move with the position, so this is stable.
        /// </summary>
        private void CentreTitle()
        {
            if (_title == null) return;
            var drawn = _title.textBounds;
            if (drawn.size.x <= 0f) return;
            var wanted = new Vector2(-drawn.center.x, -drawn.center.y);
            if ((_title.rectTransform.anchoredPosition - wanted).sqrMagnitude > 0.25f) _title.rectTransform.anchoredPosition = wanted;
        }

        /// <summary>Fits the area to where the background picture is drawn (it keeps its aspect), in this panel's space.</summary>
        public void Fit()
        {
            if (_area == null) return;
            _lastScreen = new Vector2(Screen.width, Screen.height);
            var panel = (RectTransform)transform;
            Canvas.ForceUpdateCanvases();
            Rect picture = _background != null ? PictureRect() : new Rect(0f, 0f, 1f, 1f);
            _lastPicture = picture;
            _area.anchorMin = picture.min;
            _area.anchorMax = picture.max;
            _area.offsetMin = _area.offsetMax = Vector2.zero;
            _area.localScale = Vector3.one;
            if (panel.rect.width <= 0f || _title == null) return;
            Canvas.ForceUpdateCanvases();
            _title.ForceMeshUpdate();
            CentreTitle();
        }

        /// <summary>The drawn picture as fractions of this panel's rect.</summary>
        private Rect PictureRect()
        {
            var panel = (RectTransform)transform;
            var bg = _background.rectTransform;
            Rect r = bg.rect;
            var sprite = _background.sprite;
            if (_background.preserveAspect && sprite != null && r.width > 0f && r.height > 0f)
            {
                float aspect = sprite.rect.width / sprite.rect.height;
                float w = r.width, h = r.height;
                if (w / h > aspect) w = h * aspect; else h = w / aspect;
                r = new Rect(r.center.x - w * 0.5f, r.center.y - h * 0.5f, w, h);
            }
            Vector2 min = panel.InverseTransformPoint(bg.TransformPoint(r.min));
            Vector2 max = panel.InverseTransformPoint(bg.TransformPoint(r.max));
            Rect p = panel.rect;
            if (p.width <= 0f || p.height <= 0f) return new Rect(0f, 0f, 1f, 1f);
            return Rect.MinMaxRect((min.x - p.xMin) / p.width, (min.y - p.yMin) / p.height, (max.x - p.xMin) / p.width, (max.y - p.yMin) / p.height);
        }

        private Image FindBackground()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return null;
            Image best = null;
            float bestArea = 0f;
            foreach (var image in canvas.rootCanvas.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite == null || !image.preserveAspect) continue;
                if (image.transform.IsChildOf(transform)) continue;
                var r = image.rectTransform.rect;
                float area = r.width * r.height;
                if (area > bestArea) { bestArea = area; best = image; }
            }
            return best;
        }

        private static void Place(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        /// <summary>
        /// The four menu buttons in the theme's look (Theme Studio): Start is the main action, Quit the leaving one, and
        /// each shows its theme icon.
        /// </summary>
        public static void Style(Button button, int index)
        {
            var image = button.targetGraphic as Image;
            if (image == null) return;
            var kind = index == 0 ? SoulHunter.Gameplay.UI.UITheme.ButtonKind.Primary
                : index == 3 ? SoulHunter.Gameplay.UI.UITheme.ButtonKind.Danger : SoulHunter.Gameplay.UI.UITheme.ButtonKind.Normal;
            var icon = index switch
            {
                0 => SoulHunter.Gameplay.UI.UITheme.IconSlot.Start,
                1 => SoulHunter.Gameplay.UI.UITheme.IconSlot.PowerUp,
                2 => SoulHunter.Gameplay.UI.UITheme.IconSlot.Options,
                _ => SoulHunter.Gameplay.UI.UITheme.IconSlot.Quit
            };
            SoulHunter.Gameplay.UI.UITheme.ApplyButton(button, image, kind, icon);
        }
    }
}
