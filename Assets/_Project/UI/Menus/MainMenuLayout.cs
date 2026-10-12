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
        // Owner review 2026-10-08: the logo and buttons are one group in the middle of the frame's open area (y 0.10-0.60),
        // with the HD logo larger and a clear gap above Start. Sizes in canvas units (1920x1080 reference).
        public const float GroupCentreY = 0.35f, OpenTop = 0.60f, OpenBottom = 0.10f;
        public const float LogoWidth = 640f, LogoGapButtons = 3f; // the logo's gap = 3 x the theme's button gap


        private Image _background;
        private RectTransform _area;
        private TMP_Text _title;
        private Image _logo;
        private RectTransform _group;
        private Button[] _buttons;
        private SoulHunter.Gameplay.UI.UITheme.IconSlot[] _icons;
        private Vector2 _lastScreen;
        private Rect _lastPicture;

        /// <summary>The frame's open area (a full-screen-anchored rect fitted to the drawn picture), for tests.</summary>
        public RectTransform Area => _area;
        public TMP_Text Title => _title;
        /// <summary>The HD logo picture (GameContent.MenuLogo), or null when the catalog has none (then the text logo shows).</summary>
        public Image Logo => _logo;
        public Button[] Buttons => _buttons;

        /// <summary>
        /// Buttons per row: one column for up to four buttons (the owner-approved menu of 2026-10-08), two columns beyond
        /// that (P3-02, 2026-10-09: Collection, Bestiary, Unlocks and Credits joined Start, Power Up, Options and Quit). Two
        /// columns of four keep the group exactly as tall as the approved column of four, so nothing shrinks further, and
        /// the buttons read left to right, top to bottom, as the wiki's Main_menu lists VS's ("from top to bottom, left to
        /// right"). A short last row is centred. The two-column layout waits for the owner's approval.
        /// </summary>
        public const int MaxSingleColumn = 4;
        /// <summary>The columns the buttons use now: two beyond four buttons when they fit side by side, else one.</summary>
        public int Columns { get; private set; } = 1;

        /// <summary>
        /// Responsive (owner, 2026-10-10: landscape and portrait from the same screen): true when the frame art's open area
        /// cannot hold the menu at full size (a portrait phone, a short landscape phone). The menu then centres in the safe
        /// area at full size instead of shrinking into the frame, and the background art covers the screen.
        /// </summary>
        public bool FreeOfFrame { get; private set; }

        private int WantedColumns(float roomWidth, int count)
        {
            if (count <= MaxSingleColumn) return 1;
            var layout = SoulHunter.Gameplay.UI.UITheme.Layout;
            return SoulHunter.Gameplay.UI.UiScreen.ColumnsFor(roomWidth, layout.ButtonWidth, layout.ButtonGap, 2);
        }

        public static MainMenuLayout Attach(GameObject homePanel, Button[] buttons, SoulHunter.Gameplay.UI.UITheme.IconSlot[] icons = null)
        {
            if (homePanel == null) return null;
            var layout = homePanel.GetComponent<MainMenuLayout>();
            if (layout == null) layout = homePanel.AddComponent<MainMenuLayout>();
            layout.Setup(buttons, icons);
            return layout;
        }

        private static int Count(Button[] buttons)
        {
            int n = 0;
            foreach (var button in buttons) if (button != null) n++;
            return n;
        }

        private void Setup(Button[] buttons, SoulHunter.Gameplay.UI.UITheme.IconSlot[] icons)
        {
            _buttons = buttons;
            _icons = icons;
            _background = FindBackground();
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
                if (text.name == "GameLogo_Text") { _title = text; break; }

            var go = new GameObject("FrameArea", typeof(RectTransform));
            _area = (RectTransform)go.transform;
            _area.SetParent(transform, false);
            _area.SetAsFirstSibling();

            // The group: logo and buttons together, centred in the frame's open area, scaled down only if a window is too short.
            var groupGo = new GameObject("MenuGroup", typeof(RectTransform));
            _group = (RectTransform)groupGo.transform;
            _group.SetParent(_area, false);
            _group.anchorMin = _group.anchorMax = new Vector2(FrameCentreX, GroupCentreY);
            _group.pivot = new Vector2(0.5f, 0.5f);
            _group.sizeDelta = Vector2.zero;

            var logoSprite = SoulHunter.Gameplay.Data.GameContentCatalog.Load()?.MenuLogo;
            if (logoSprite != null)
            {
                // The HD logo (owner review 2026-10-08): the two small text sprites were stretched about 3x and blurred.
                var logoGo = new GameObject("GameLogo", typeof(RectTransform), typeof(Image));
                logoGo.transform.SetParent(_group, false);
                _logo = logoGo.GetComponent<Image>();
                _logo.sprite = logoSprite;
                _logo.preserveAspect = true;
                _logo.raycastTarget = false;
                if (_title != null) _title.gameObject.SetActive(false);
            }

            if (_title != null && _logo == null)
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

            int index = 0, count = Count(_buttons);
            for (int b = 0; b < _buttons.Length; b++)
            {
                var button = _buttons[b];
                if (button == null) continue;
                button.transform.SetParent(_logo != null ? _group : _area, false);
                var icon = _icons != null && b < _icons.Length ? _icons[b] : DefaultIcon(index, count);
                Style(button, index == 0, index == count - 1, icon);
                index++;
            }
            // "Power Up", as the shop's title writes it (the scene's label said "Power UP").
            if (_buttons.Length > 1 && _buttons[1] != null)
                foreach (var label in _buttons[1].GetComponentsInChildren<TMP_Text>(true))
                    if (label.text.Trim().Equals("Power UP", System.StringComparison.Ordinal)) label.text = "Power Up";
            PlaceButtons();
            SoulHunter.Gameplay.UI.UiThemeAsset.Changed += PlaceButtons;
            SoulHunter.Gameplay.UI.UiScreen.Changed += Fit;
            Fit();
        }

        private void OnDestroy()
        {
            SoulHunter.Gameplay.UI.UiThemeAsset.Changed -= PlaceButtons;
            SoulHunter.Gameplay.UI.UiScreen.Changed -= Fit;
        }

        /// <summary>
        /// A column of the theme's standard buttons with the theme's gap, centred on the frame's ornament in the open area
        /// under the logo (one button size in the whole game, owner 2026-10-07; the frame no longer stretches them).
        /// </summary>
        private void PlaceButtons()
        {
            if (this == null || _buttons == null) return;
            var layout = SoulHunter.Gameplay.UI.UITheme.Layout;
            int count = Count(_buttons);
            Vector2 room = Room(out bool free);
            int columns = WantedColumns(room.x, count);
            if (!free && !FitsAtFullSize(room, columns, count, layout))
            {
                // The frame's open area is too small for the menu at full size: leave the frame (responsive, 2026-10-10).
                free = true;
                room = FreeRoom();
                columns = WantedColumns(room.x, count);
            }
            SetFree(free);
            Columns = columns;
            int rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)columns));
            float buttonHeight = ButtonHeight(layout);
            float stepY = buttonHeight + layout.ButtonGap;
            float stepX = layout.ButtonWidth + layout.ButtonGap;
            float blockHeight = rows * buttonHeight + (rows - 1) * layout.ButtonGap;
            float blockWidth = columns * layout.ButtonWidth + (columns - 1) * layout.ButtonGap;
            if (_logo != null)
            {
                // One group: the logo, a gap, then the button block, centred on the group's middle.
                float gap = layout.ButtonGap * LogoGapButtons;
                // The logo gives way first: it takes the width and height the buttons leave; the buttons keep their size.
                float aspect = _logo.sprite.rect.height / _logo.sprite.rect.width;
                float logoW = Mathf.Min(LogoWidth, room.x * 0.92f);
                if (room.y > 0f) logoW = Mathf.Min(logoW, Mathf.Max(0f, room.y - blockHeight - gap) / aspect);
                float logoHeight = logoW * aspect;
                float total = logoHeight + gap + blockHeight;
                float top = total * 0.5f;
                var logoRect = _logo.rectTransform;
                logoRect.anchorMin = logoRect.anchorMax = logoRect.pivot = new Vector2(0.5f, 0.5f);
                logoRect.sizeDelta = new Vector2(logoW, logoHeight);
                logoRect.anchoredPosition = new Vector2(0f, top - logoHeight * 0.5f);
                float firstCentre = top - logoHeight - gap - buttonHeight * 0.5f;
                PlaceBlock(new Vector2(0.5f, 0.5f), firstCentre, columns, count, stepX, stepY, layout);
                _group.sizeDelta = new Vector2(Mathf.Max(logoW, blockWidth), total);
                FitGroup();
                return;
            }
            PlaceBlock(new Vector2(FrameCentreX, (ButtonsTop + ButtonsBottom) * 0.5f), (rows - 1) * 0.5f * stepY, columns, count, stepX, stepY, layout);
        }

        /// <summary>Lays the buttons in rows of <paramref name="columns"/>, the first row's centre at <paramref name="firstCentre"/>.</summary>
        private void PlaceBlock(Vector2 anchor, float firstCentre, int columns, int count, float stepX, float stepY,
            SoulHunter.Gameplay.UI.UiThemeAsset.Layout layout)
        {
            int i = 0;
            foreach (var button in _buttons)
            {
                if (button == null) continue;
                int row = i / columns, column = i % columns;
                int inRow = Mathf.Min(columns, count - row * columns);
                var rect = (RectTransform)button.transform;
                rect.anchorMin = rect.anchorMax = anchor;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(layout.ButtonWidth, ButtonHeight(layout));
                rect.anchoredPosition = new Vector2((column - (inRow - 1) * 0.5f) * stepX, firstCentre - row * stepY);
                i++;
            }
        }

        /// <summary>Shrinks the group as one piece when the frame's open area is too short or narrow for it (a short window).</summary>
        private void FitGroup()
        {
            if (_group == null || _area == null) return;
            Vector2 room = Room(out _);
            var size = _group.sizeDelta;
            float scale = 1f;
            if (size.y > 0f && room.y > 0f) scale = Mathf.Min(scale, room.y / size.y);
            if (size.x > 0f && room.x > 0f) scale = Mathf.Min(scale, room.x / size.x);
            // Never below the touch target: a button stays at least 48 dp on a touch screen (UiScreen).
            float height = ButtonHeight(SoulHunter.Gameplay.UI.UITheme.Layout);
            if (height > 0f) scale = Mathf.Max(scale, SoulHunter.Gameplay.UI.UiScreen.MinTouchUnits / height);
            _group.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>
        /// The theme's button height, grown to the touch target on a touch screen: the same rule as every other button
        /// (ThemedElement.StandardSize), so the menu's text and icons keep the phone's text size and only the plate grows.
        /// </summary>
        private static float ButtonHeight(SoulHunter.Gameplay.UI.UiThemeAsset.Layout layout) =>
            Mathf.Max(layout.ButtonHeight, SoulHunter.Gameplay.UI.UiScreen.MinTouchUnits);

        /// <summary>The room the group has: the frame's open area, or (free of the frame) the safe area.</summary>
        private Vector2 Room(out bool free)
        {
            free = FreeOfFrame;
            if (free) return FreeRoom();
            if (_area == null) return Vector2.zero;
            return new Vector2(_area.rect.width * 0.6f, _area.rect.height * (OpenTop - OpenBottom) * 0.94f);
        }

        private Vector2 FreeRoom()
        {
            var panel = (RectTransform)transform;
            Rect safe = SoulHunter.Gameplay.UI.UiScreen.SafeAreaNormalized;
            return new Vector2(panel.rect.width * safe.width * 0.94f, panel.rect.height * safe.height * 0.94f);
        }

        private static bool FitsAtFullSize(Vector2 room, int columns, int count, SoulHunter.Gameplay.UI.UiThemeAsset.Layout layout)
        {
            int rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)columns));
            float w = columns * layout.ButtonWidth + (columns - 1) * layout.ButtonGap;
            // Room for the buttons plus at least a small logo above them.
            float h = rows * ButtonHeight(layout) + (rows - 1) * layout.ButtonGap + layout.ButtonGap * LogoGapButtons + 80f;
            if (w <= 0f || h <= 0f) return true;
            float scale = Mathf.Min(1f, room.x / w, room.y / h);
            // A PC window may shrink the menu inside the frame (owner-approved 2026-10-08), down to 60%. On a touch screen
            // a button never shrinks under the touch target.
            float floor = SoulHunter.Gameplay.UI.UiScreen.IsTouch ? 1f : 0.6f;
            return scale >= floor - 0.001f;
        }

        /// <summary>
        /// Free of the frame, the group centres in the safe area and the background art covers the screen (cropped at the
        /// sides in portrait) instead of drawing as a thin band with bars.
        /// </summary>
        private void SetFree(bool free)
        {
            FreeOfFrame = free;
            if (_group == null) return;
            Rect safe = SoulHunter.Gameplay.UI.UiScreen.SafeAreaNormalized;
            var parent = free ? (RectTransform)transform : _area;
            if (_group.parent != parent) _group.SetParent(parent, false);
            _group.anchorMin = _group.anchorMax = free ? safe.center : new Vector2(FrameCentreX, GroupCentreY);
            _group.anchoredPosition = Vector2.zero;
            if (_background == null) return;
            var cover = _background.GetComponent<AspectRatioFitter>();
            if (free && cover == null && _background.sprite != null)
            {
                cover = _background.gameObject.AddComponent<AspectRatioFitter>();
                cover.aspectRatio = _background.sprite.rect.width / _background.sprite.rect.height;
            }
            if (cover == null) return;
            cover.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            cover.enabled = free;
            if (!free)
            {
                var bg = _background.rectTransform;
                bg.anchorMin = Vector2.zero;
                bg.anchorMax = Vector2.one;
                bg.offsetMin = bg.offsetMax = Vector2.zero;
            }
        }

        private void LateUpdate()
        {
            var screen = new Vector2(Screen.width, Screen.height);
            // Free of the frame the art covers the screen, so its drawn rect is not the frame's: only a size change refits.
            if (screen != _lastScreen || (!FreeOfFrame && _background != null && PictureRect() != _lastPicture)) Fit();
            CentreTitle();
        }

        /// <summary>
        /// The logo sprites carry their own side bearings, and the logo auto-sizes after layout: keep what is drawn (its
        /// text bounds, in its own space) on the box centre. The bounds do not move with the position, so this is stable.
        /// </summary>
        private void CentreTitle()
        {
            if (_title == null || _logo != null) return;
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
            SetFree(false); // measure the frame where the art is drawn fitted, not covering
            Canvas.ForceUpdateCanvases();
            Rect picture = _background != null ? PictureRect() : new Rect(0f, 0f, 1f, 1f);
            _lastPicture = picture;
            _area.anchorMin = picture.min;
            _area.anchorMax = picture.max;
            _area.offsetMin = _area.offsetMax = Vector2.zero;
            _area.localScale = Vector3.one;
            Canvas.ForceUpdateCanvases();
            FreeOfFrame = false; // decide again for this size
            PlaceButtons();
            if (panel.rect.width <= 0f || _title == null || _logo != null) return;
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

        /// <summary>The icon a button shows when the caller names none: the four approved buttons in their order.</summary>
        private static SoulHunter.Gameplay.UI.UITheme.IconSlot DefaultIcon(int index, int count) =>
            index == 0 ? SoulHunter.Gameplay.UI.UITheme.IconSlot.Start
            : index == count - 1 ? SoulHunter.Gameplay.UI.UITheme.IconSlot.Quit
            : index == 1 ? SoulHunter.Gameplay.UI.UITheme.IconSlot.PowerUp
            : SoulHunter.Gameplay.UI.UITheme.IconSlot.Options;

        /// <summary>
        /// A menu button in the theme's look (Theme Studio): Start (the first) is the main action, Quit (the last) the
        /// leaving one, and each shows its theme icon.
        /// </summary>
        public static void Style(Button button, bool first, bool last, SoulHunter.Gameplay.UI.UITheme.IconSlot icon)
        {
            var image = button.targetGraphic as Image;
            if (image == null) return;
            var kind = first ? SoulHunter.Gameplay.UI.UITheme.ButtonKind.Primary
                : last ? SoulHunter.Gameplay.UI.UITheme.ButtonKind.Danger : SoulHunter.Gameplay.UI.UITheme.ButtonKind.Normal;
            SoulHunter.Gameplay.UI.UITheme.ApplyButton(button, image, kind, icon);
        }

        /// <summary>The four-button menu's look by position (0 Start, 1 Power Up, 2 Options, 3 Quit).</summary>
        public static void Style(Button button, int index) => Style(button, index == 0, index == 3, DefaultIcon(index, 4));
    }
}
