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
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
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
            UITheme.AddVeil(root); // the character screen behind stays dark
            _window = new GameObject("Window", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            _window.SetParent(root, false);
            Anchor(_window, 0.18f, 0.07f, 0.82f, 0.93f);
            var windowImage = _window.GetComponent<Image>();
            UITheme.ApplyPanel(windowImage);
            windowImage.raycastTarget = true; // clicks never reach the screen behind

            Text("Title", _window, 0.1f, 0.88f, 0.9f, 0.97f, "Music", 52f, TextAlignmentOptions.Center, ThemedElement.TextRole.Title, () => UITheme.Panel);
            Text("TrackCaption", _window, 0.04f, 0.81f, 0.96f, 0.87f, "Track for your next run", 26f, TextAlignmentOptions.Left,
                ThemedElement.TextRole.Soft, () => UITheme.Panel);

            // The track grid: 12 cards, 3 columns, sized from the area (GridFit).
            var area = new GameObject("Tracks", typeof(RectTransform)).GetComponent<RectTransform>();
            area.SetParent(_window, false);
            Anchor(area, 0.04f, 0f, 0.96f, 0.8f);
            var layout = UITheme.Layout;
            float gapU = layout.ButtonGap, captionU = 40f;
            float actionsY = gapU + layout.ButtonHeight * 0.5f;                       // Keep and Done, on the bottom edge
            float speedY = actionsY + layout.ButtonHeight * 0.5f + gapU + layout.TabHeight * 0.5f;
            float captionY = speedY + layout.TabHeight * 0.5f + gapU * 0.5f + captionU * 0.5f;
            area.offsetMin = new Vector2(0f, captionY + captionU * 0.5f + gapU);       // the grid fills what is left
            var grid = area.gameObject.AddComponent<GridLayoutGroup>();
            float gap = UITheme.Layout.ButtonGap;
            grid.spacing = new Vector2(gap, gap);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;
            var fit = area.gameObject.AddComponent<GridFit>();
            fit.Grid = grid;
            fit.Viewport = area;
            fit.Count = MusicSelection.ChoiceCount;
            fit.MinHeightRatio = 0f;
            for (int i = 0; i < MusicSelection.ChoiceCount; i++) BuildTrack(area, i);

            // Speed: three tabs in one themed row (the theme's tab size and gap).
            // The rows below the grid stack up from the window's bottom edge in theme units, so the fixed-size buttons
            // never overlap or stick out of a short window (2026-10-10).
            _speedCaption = Text("SpeedCaption", _window, 0.04f, 0f, 0.96f, 0f, "", 26f, TextAlignmentOptions.Left,
                ThemedElement.TextRole.Soft, () => UITheme.Panel);
            var captionRect = _speedCaption.rectTransform;
            captionRect.offsetMin = new Vector2(0f, captionY - captionU * 0.5f);
            captionRect.offsetMax = new Vector2(0f, captionY + captionU * 0.5f);
            var speedRow = new GameObject("SpeedRow", typeof(RectTransform)).GetComponent<RectTransform>();
            speedRow.SetParent(_window, false);
            Anchor(speedRow, 0.5f, 0f, 0.5f, 0f);
            speedRow.anchoredPosition = new Vector2(0f, speedY);
            for (int i = 0; i < MusicSelection.TempoNames.Length; i++)
            {
                int tempo = i;
                _tempos.Add(MakeButton(speedRow, "Speed_" + MusicSelection.TempoNames[i], MusicSelection.TempoNames[i], () => SelectTempo(tempo),
                    UITheme.ButtonKind.Normal, UITheme.IconSlot.None, ThemedElement.SizeRole.Tab));
            }
            speedRow.gameObject.AddComponent<ThemedRow>();

            // Keep (the VS lock) and Done.
            var actions = new GameObject("Actions", typeof(RectTransform)).GetComponent<RectTransform>();
            actions.SetParent(_window, false);
            // Pinned to the window's bottom edge with the theme gap: the buttons have a fixed size, so a row placed at a
            // fraction of the window's height stuck out of a short window (test runner, 2026-10-10).
            Anchor(actions, 0.5f, 0f, 0.5f, 0f);
            actions.anchoredPosition = new Vector2(0f, actionsY);
            _lock = MakeButton(actions, "Keep_Button", "", ToggleLock, UITheme.ButtonKind.Normal, UITheme.IconSlot.None, ThemedElement.SizeRole.Standard);
            _lockLabel = _lock.GetComponentInChildren<TextMeshProUGUI>();
            _done = MakeButton(actions, "Done_Button", "Done", Close, UITheme.ButtonKind.Primary, UITheme.IconSlot.Confirm, ThemedElement.SizeRole.Standard);
            actions.gameObject.AddComponent<ThemedRow>();
            LinkNavigation();
        }

        /// <summary>
        /// Keyboard and controller focus stays inside the screen (the character screen behind is still there): tracks in
        /// their 3-column grid, then the speed tabs, then Keep and Done.
        /// </summary>
        private void LinkNavigation()
        {
            const int columns = 3;
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
                    selectOnUp = At(_tracks, _tracks.Count - columns + Mathf.Min(i, columns - 1)),
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
