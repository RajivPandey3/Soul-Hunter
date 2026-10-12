using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.Audio;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.UI
{
    /// <summary>
    /// The Music screen (VS Music Selection; release item P3-05), opened from the Music card beside the run modes on the
    /// character select screen. vampire.survivors.wiki/w/Stages: "customize the background music of a stage by choosing
    /// from any unlocked music tracks ... modifiers ... Normal, Hyper, and Forsaken"; Music: "lock the selection of a music
    /// track so it persists between runs", "Music and music mods can now be previewd in stage selection screen".
    ///
    /// Layout (ui-ux-pro-max: one size per kind of control, visible focus, state in words as well as colour, 4.5:1 text;
    /// frontend-design: quiet frame, one strong element, the track grid): a window over a veil, the title, a 3 x 4 grid of
    /// track cards (the chosen one framed with the theme's selected-card edge), a row of three speed tabs (the chosen one in
    /// the Primary kind and named in the caption), and a row with "Keep for next runs: ON/OFF" and Done. Every pick is saved
    /// at once and previewed in the menu at its speed; Done returns the menu theme. Everything is styled by the theme.
    /// </summary>
    public sealed class MusicSelectionScreen : MonoBehaviour
    {
        private readonly List<Button> _tracks = new List<Button>();
        private readonly List<Outline> _trackFrames = new List<Outline>();
        private readonly List<Button> _tempos = new List<Button>();
        private readonly List<(TMP_Text Text, Func<Color> Surface)> _readable = new List<(TMP_Text, Func<Color>)>();
        private Button _lock, _done;
        private TextMeshProUGUI _lockLabel, _speedCaption;
        private Action _onClose;
        private TMP_FontAsset _font;
        private RectTransform _window;

        public IReadOnlyList<Button> Tracks => _tracks;
        public IReadOnlyList<Button> Tempos => _tempos;
        public Button LockButton => _lock;
        public Button DoneButton => _done;
        public string LockText => _lockLabel != null ? _lockLabel.text : null;
        public bool IsOpen => gameObject.activeSelf;
        public RectTransform Window => _window;
        public bool IsFramed(int choice) => choice >= 0 && choice < _trackFrames.Count && _trackFrames[choice].enabled;

        /// <summary>Every text the screen shows, with the colour of the surface it sits on (tests check 4.5:1).</summary>
        public IEnumerable<(TMP_Text Text, Color Surface)> ReadableTexts()
        {
            foreach (var pair in _readable)
                if (pair.Text != null && pair.Text.isActiveAndEnabled && !string.IsNullOrWhiteSpace(pair.Text.text))
                    yield return (pair.Text, pair.Surface());
        }

        /// <summary>Above the menu canvas, below the unlock notice (500).</summary>
        public const int ScreenSortingOrder = 350;

        /// <summary>Builds the screen in <paramref name="parent"/>'s scene (the character select panel's), hidden until opened.</summary>
        public static MusicSelectionScreen Create(RectTransform parent, TMP_FontAsset font)
        {
            var go = new GameObject("MusicSelection", typeof(RectTransform));
            go.SetActive(false);
            // Its own root overlay canvas, Expand 1920x1080 (as the chest, level-up and Arcana screens): on the menu canvas
            // (match 0.5) a wide, short window left too few units for the fixed-size buttons (2026-10-10).
            if (parent != null && parent.gameObject.scene.IsValid())
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, parent.gameObject.scene);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = ScreenSortingOrder; // over the character screen
            SoulHunter.Gameplay.UI.ResponsiveCanvas.Apply(canvas); // the game's one UI scale (UiScreen), landscape and portrait
            go.AddComponent<GraphicRaycaster>();
            var screen = go.AddComponent<MusicSelectionScreen>();
            screen._font = font;
            screen.Build();
            return screen;
        }

        public void Open(Action onClose)
        {
            _onClose = onClose;
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            Relayout(); // the buttons restyled themselves on enable: place the rows by their sizes now
            Refresh();
            Preview();
            var data = Data;
            int choice = data != null ? Mathf.Clamp(data.MusicChoice, 0, _tracks.Count - 1) : 0;
            if (EventSystem.current != null && _tracks.Count > 0) EventSystem.current.SetSelectedGameObject(_tracks[choice].gameObject);
        }

        public void Close()
        {
            if (!gameObject.activeSelf) return;
            gameObject.SetActive(false);
            GameServices.Instance?.Get<SaveService>()?.SaveGame();
            var audio = AudioManager.Instance;
            if (audio != null) audio.PlayMusic(audio.MenuMusic); // back to the menu theme at normal speed
            var close = _onClose;
            _onClose = null;
            close?.Invoke();
        }

        /// <summary>Picks a track (0 = each level's own, 1 = main theme, 2-11 = Levels 1-10), saves it and previews it.</summary>
        public void SelectTrack(int choice)
        {
            var data = Data;
            if (data == null) return;
            data.MusicChoice = Mathf.Clamp(choice, 0, MusicSelection.ChoiceCount - 1);
            GameServices.Instance?.Get<SaveService>()?.SaveGame();
            Refresh();
            Preview();
        }

        /// <summary>Picks the speed (MusicSelection.Normal / Faster / Slower), saves it and previews it.</summary>
        public void SelectTempo(int tempo)
        {
            var data = Data;
            if (data == null) return;
            data.MusicTempo = Mathf.Clamp(tempo, 0, MusicSelection.TempoPitch.Length - 1);
            GameServices.Instance?.Get<SaveService>()?.SaveGame();
            Refresh();
            Preview();
        }

        /// <summary>Turns the lock on or off: on, the choice stays for every later run (VS Patch 1.0).</summary>
        public void ToggleLock()
        {
            var data = Data;
            if (data == null) return;
            data.MusicLocked = !data.MusicLocked;
            GameServices.Instance?.Get<SaveService>()?.SaveGame();
            Refresh();
        }

        /// <summary>
        /// VS previews music and its modifier on the selection screen. "Each level's own" previews Level 1's track, the level
        /// a run starts on.
        /// </summary>
        private void Preview()
        {
            var audio = AudioManager.Instance;
            var data = Data;
            if (audio == null || data == null) return;
            audio.PlayMusic(MusicSelection.ClipFor(audio, data.MusicChoice, 1), MusicSelection.PitchFor(data.MusicTempo));
        }

        private void Refresh()
        {
            var data = Data;
            int choice = data != null ? data.MusicChoice : 0;
            int tempo = data != null ? data.MusicTempo : 0;
            for (int i = 0; i < _trackFrames.Count; i++) _trackFrames[i].enabled = i == choice;
            for (int i = 0; i < _tempos.Count; i++)
                UITheme.ApplyButton(_tempos[i], _tempos[i].targetGraphic as Image, i == tempo ? UITheme.ButtonKind.Primary : UITheme.ButtonKind.Normal,
                    UITheme.IconSlot.None, true, ThemedElement.SizeRole.Tab);
            if (_speedCaption != null) _speedCaption.text = "Speed: " + MusicSelection.TempoNames[Mathf.Clamp(tempo, 0, 2)];
            if (_lockLabel != null) _lockLabel.text = data != null && data.MusicLocked ? "Keep for next runs: ON" : "Keep for next runs: OFF";
        }

        private static GameData Data
        {
            get
            {
                SaveService save = null;
                GameServices.Instance?.TryGet(out save);
                return save != null ? save.CurrentData : null;
            }
        }

        // ---------------------------------------------------------------- building

        private void Build()
        {
            var root = (RectTransform)transform;
            UITheme.AddVeil(root); // the character screen behind stays dark (full screen)
            // Responsive (owner, 2026-10-10: landscape and portrait from the same screen): the window sits in the safe area
            // and is placed by Relayout; nothing here is rebuilt on a rotation.
            _safe = new GameObject("SafeContent", typeof(RectTransform)).GetComponent<RectTransform>();
            _safe.SetParent(root, false);
            Anchor(_safe, 0f, 0f, 1f, 1f);
            SafeAreaFitter.Fit(_safe);
            _window = new GameObject("Window", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            _window.SetParent(_safe, false);
            var windowImage = _window.GetComponent<Image>();
            UITheme.ApplyPanel(windowImage);
            windowImage.raycastTarget = true; // clicks never reach the screen behind

            _title = Text("Title", _window, 0f, 1f, 1f, 1f, "Music", 52f, TextAlignmentOptions.Center, ThemedElement.TextRole.Title, () => UITheme.Panel);
            _trackCaption = Text("TrackCaption", _window, 0f, 1f, 1f, 1f, "Track for your next run", 26f, TextAlignmentOptions.Left,
                ThemedElement.TextRole.Soft, () => UITheme.Panel);

            // The track grid: 12 cards, up to 3 columns (fewer on a narrow screen), sized from the area (GridFit). On a short
            // screen the cards keep their touch size and the grid scrolls inside its area.
            _trackArea = new GameObject("Tracks", typeof(RectTransform)).GetComponent<RectTransform>();
            _trackArea.SetParent(_window, false);
            // The area is its own masked viewport.
            _trackScroll = ResponsiveScroll.Create(_trackArea, null, "TrackGrid", out var gridContent,
                sensitivity: 30f, dragSurface: false, fitContentHeight: true);
            _trackGrid = gridContent.gameObject.AddComponent<GridLayoutGroup>();
            float gap = UITheme.Layout.ButtonGap;
            _trackGrid.spacing = new Vector2(gap, gap);
            _trackGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _trackGrid.constraintCount = TrackColumns;
            _trackGrid.childAlignment = TextAnchor.UpperCenter;
            var fit = gridContent.gameObject.AddComponent<GridFit>();
            fit.Grid = _trackGrid;
            fit.Viewport = _trackArea;
            fit.Count = MusicSelection.ChoiceCount;
            fit.MinHeightRatio = 0f;
            fit.MinCellWidth = TrackMinWidth;
            fit.MaxColumns = TrackColumns;
            for (int i = 0; i < MusicSelection.ChoiceCount; i++) BuildTrack(gridContent, i);

            // Speed: three tabs in one themed row (the theme's tab size and gap; it wraps on a narrow screen).
            _speedCaption = Text("SpeedCaption", _window, 0f, 0f, 1f, 0f, "", 26f, TextAlignmentOptions.Left,
                ThemedElement.TextRole.Soft, () => UITheme.Panel);
            _speedRowRect = new GameObject("SpeedRow", typeof(RectTransform)).GetComponent<RectTransform>();
            _speedRowRect.SetParent(_window, false);
            for (int i = 0; i < MusicSelection.TempoNames.Length; i++)
            {
                int tempo = i;
                _tempos.Add(MakeButton(_speedRowRect, "Speed_" + MusicSelection.TempoNames[i], MusicSelection.TempoNames[i], () => SelectTempo(tempo),
                    UITheme.ButtonKind.Normal, UITheme.IconSlot.None, ThemedElement.SizeRole.Tab));
            }
            _speedRow = _speedRowRect.gameObject.AddComponent<ThemedRow>();

            // Keep (the VS lock) and Done, pinned to the window's bottom edge.
            _actionsRect = new GameObject("Actions", typeof(RectTransform)).GetComponent<RectTransform>();
            _actionsRect.SetParent(_window, false);
            _lock = MakeButton(_actionsRect, "Keep_Button", "", ToggleLock, UITheme.ButtonKind.Normal, UITheme.IconSlot.None, ThemedElement.SizeRole.Standard);
            _lockLabel = _lock.GetComponentInChildren<TextMeshProUGUI>();
            _done = MakeButton(_actionsRect, "Done_Button", "Done", Close, UITheme.ButtonKind.Primary, UITheme.IconSlot.Confirm, ThemedElement.SizeRole.Standard);
            _actionsRow = _actionsRect.gameObject.AddComponent<ThemedRow>();
            Relayout();
        }

        private const int TrackColumns = 3;      // the approved 3 x 4 grid on a PC
        private const float TrackMinWidth = 200f; // a track name needs this much; a phone held upright gets 2 columns

        private RectTransform _safe, _trackArea, _speedRowRect, _actionsRect;
        private TextMeshProUGUI _title, _trackCaption;
        private GridLayoutGroup _trackGrid;
        private ScrollRect _trackScroll;
        private ThemedRow _speedRow, _actionsRow;
        private Vector2 _laidOutSize = new Vector2(-1f, -1f);
        private int _linkedColumns = -1;

        /// <summary>The scrolling area that holds the track cards (tests).</summary>
        public RectTransform TrackArea => _trackArea;

        private void OnEnable()
        {
            UiScreen.Changed += Relayout;
            _laidOutSize = new Vector2(-1f, -1f); // buttons restyle themselves on enable: lay out again at the end of the frame
        }

        private void OnDisable() => UiScreen.Changed -= Relayout;

        private void LateUpdate()
        {
            if (_safe != null && _safe.rect.size != _laidOutSize) Relayout();
        }

        /// <summary>
        /// Places the window and its rows for the current safe area: a centred window on a PC, a full-screen sheet on a
        /// phone held upright, the whole height on a short screen. The rows stack up from the window's bottom edge by their
        /// real (wrapped) heights, so nothing overlaps on any screen; the track grid takes what is left and scrolls.
        /// </summary>
        public void Relayout()
        {
            if (this == null || _safe == null) return;
            Vector2 size = _safe.rect.size;
            if (size.x <= 1f || size.y <= 1f) return;
            _laidOutSize = size;
            var layout = UITheme.Layout;
            float gap = layout.ButtonGap;
            float W = size.x, H = size.y;
            bool sheet = W < 700f; // a phone held upright: the window is the whole safe area
            float w = sheet ? W : Mathf.Min(W - 2f * gap, Mathf.Max(W * 0.64f, 1100f));
            float h = sheet ? H : H < 700f ? H - 2f * gap : Mathf.Min(H - 2f * gap, H * 0.86f);
            _window.anchorMin = _window.anchorMax = new Vector2(0.5f, 0.5f);
            _window.pivot = new Vector2(0.5f, 0.5f);
            _window.anchoredPosition = Vector2.zero;
            _window.sizeDelta = new Vector2(w, h);

            float pad = gap;
            float titleH = h >= 700f ? 70f : 48f, captionH = 36f;
            // From the bottom: Keep and Done, then the speed tabs, then their caption.
            float y = pad;
            y += PlaceRowFromBottom(_actionsRect, _actionsRow, y) + gap;
            y += PlaceRowFromBottom(_speedRowRect, _speedRow, y) + gap * 0.5f;
            StretchBottom(_speedCaption.rectTransform, pad, y, captionH);
            float bottomUsed = y + captionH + gap;
            // From the top: the title and the track caption.
            StretchTop(_title.rectTransform, pad, pad, titleH);
            StretchTop(_trackCaption.rectTransform, pad, pad + titleH, captionH);
            float topUsed = pad + titleH + captionH + gap * 0.5f;
            _trackArea.anchorMin = Vector2.zero;
            _trackArea.anchorMax = Vector2.one;
            _trackArea.offsetMin = new Vector2(pad, bottomUsed);
            _trackArea.offsetMax = new Vector2(-pad, -topUsed);

            // The columns GridFit picks for this width: keyboard and controller focus follow them.
            int columns = UiScreen.ColumnsFor(_trackArea.rect.width, Mathf.Max(TrackMinWidth, UiScreen.MinTouchUnits), _trackGrid.spacing.x, TrackColumns);
            if (columns != _linkedColumns) LinkNavigation(columns);
            UiTextFloor.RaiseAll(_window); // no text under the readable floor
        }

        /// <summary>Puts a themed row's bottom edge at <paramref name="bottom"/> in the window and returns its wrapped height.</summary>
        private static float PlaceRowFromBottom(RectTransform rect, ThemedRow row, float bottom)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, bottom);
            row.Layout();
            return rect.sizeDelta.y * rect.localScale.y;
        }

        private static void StretchBottom(RectTransform rect, float pad, float fromBottom, float height)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.offsetMin = new Vector2(pad, fromBottom);
            rect.offsetMax = new Vector2(-pad, fromBottom + height);
        }

        private static void StretchTop(RectTransform rect, float pad, float fromTop, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(pad, -(fromTop + height));
            rect.offsetMax = new Vector2(-pad, -fromTop);
        }

        /// <summary>
        /// Keyboard and controller focus stays inside the screen (the character screen behind is still there): tracks in
        /// their 3-column grid, then the speed tabs, then Keep and Done.
        /// </summary>
        private void LinkNavigation(int columns)
        {
            _linkedColumns = columns;
            columns = Mathf.Max(1, columns);
            Selectable At(List<Button> list, int i) => i >= 0 && i < list.Count ? list[i] : null;
            var bottomRow = new List<Button> { _lock, _done };
            for (int i = 0; i < _tracks.Count; i++)
            {
                int row = i / columns, col = i % columns;
                bool lastRow = i + columns >= _tracks.Count;
                _tracks[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = col > 0 ? _tracks[i - 1] : null,
                    selectOnRight = col < columns - 1 ? At(_tracks, i + 1) : null,
                    selectOnUp = row > 0 ? _tracks[i - columns] : null,
                    selectOnDown = lastRow ? At(_tempos, Mathf.Min(col, _tempos.Count - 1)) : _tracks[i + columns],
                };
            }
            for (int i = 0; i < _tempos.Count; i++)
                _tempos[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = At(_tempos, i - 1),
                    selectOnRight = At(_tempos, i + 1),
                    selectOnUp = At(_tracks, _tracks.Count - columns + Mathf.Min(i, columns - 1)) ?? At(_tracks, _tracks.Count - 1),
                    selectOnDown = i < _tempos.Count / 2 ? (Selectable)_lock : _done,
                };
            for (int i = 0; i < bottomRow.Count; i++)
                bottomRow[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = At(bottomRow, i - 1),
                    selectOnRight = At(bottomRow, i + 1),
                    selectOnUp = At(_tempos, i == 0 ? 0 : _tempos.Count - 1),
                };
        }

        private void BuildTrack(RectTransform area, int choice)
        {
            var button = MakeButton(area, "Track_" + choice, MusicSelection.ChoiceName(choice), () => SelectTrack(choice),
                UITheme.ButtonKind.Card, UITheme.IconSlot.None, ThemedElement.SizeRole.Free);
            // The chosen track wears the theme's selected-card frame (shape as well as colour; the character cards' rule).
            var frame = button.gameObject.AddComponent<Outline>();
            var cards = UiThemeAsset.Active.Cards;
            frame.effectColor = cards.SelectedEdge;
            frame.effectDistance = new Vector2(cards.SelectedEdgeWidth, -cards.SelectedEdgeWidth);
            frame.useGraphicAlpha = true;
            frame.enabled = false;
            _tracks.Add(button);
            _trackFrames.Add(frame);
        }

        private Button MakeButton(RectTransform parent, string name, string label, UnityEngine.Events.UnityAction onClick,
            UITheme.ButtonKind kind, UITheme.IconSlot icon, ThemedElement.SizeRole size)
        {
            var rect = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            var image = rect.GetComponent<Image>();
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() =>
            {
                var audio = AudioManager.Instance;
                if (audio != null) audio.PlaySFX(audio.UIClickSound);
                onClick();
            });
            var text = Text("Label", rect, 0.04f, 0.06f, 0.96f, 0.94f, label, size == ThemedElement.SizeRole.Free ? 24f : 26f,
                TextAlignmentOptions.Center, kind == UITheme.ButtonKind.Card ? ThemedElement.TextRole.Card : ThemedElement.TextRole.Body,
                () => button.colors.normalColor); // the plate the player sees; card plates take the theme's card text
            text.textWrappingMode = size == ThemedElement.SizeRole.Free ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            UITheme.ApplyButton(button, image, kind, icon, true, size); // after the label exists, so it styles it too
            return button;
        }

        private TextMeshProUGUI Text(string name, RectTransform parent, float xMin, float yMin, float xMax, float yMax, string text,
            float maxSize, TextAlignmentOptions alignment, ThemedElement.TextRole role, Func<Color> surface)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Anchor(rect, xMin, yMin, xMax, yMax);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (_font != null) label.font = _font;
            label.fontSize = maxSize; // a size before auto-size (TMP's defaults would replace the limits)
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Min(14f, maxSize);
            label.fontSizeMax = maxSize;
            label.alignment = alignment;
            label.text = text;
            label.raycastTarget = false;
            UITheme.ApplyText(label, role);
            _readable.Add((label, surface));
            return label;
        }

        private static void Anchor(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
