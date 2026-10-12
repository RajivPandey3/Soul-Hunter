using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.UI;
using SoulHunter.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Owner (2026-10-10): the game plays in landscape and portrait from the same screens, which adjust themselves. The
    /// character select screen and its Music screen are laid out on a PC (16:9, 21:9, 5:4) and on the owner's phone held
    /// either way (1080x2400 at 480 dpi, touch).
    /// - Back and Confirm (the screen's closing buttons) are always on the canvas: on a PC at their approved corners (Back top
    ///   right, Confirm bottom right), on the phone pinned in the action row under the scrolling body.
    /// - Every other control sits in the scrolling body: inside its width, none overlapping another, none under 48 dp on the
    ///   phone. A control below the fold is reached by scrolling, so it is checked against the body's content, not the canvas.
    /// - No text under the phone's 12 sp floor.
    /// - On the phone the body is one scroll that starts with the chosen character and the card grid (owner's phone,
    ///   2026-10-10: the modes and the Music card had pushed the cards off the screen): at least two whole rows of cards
    ///   are in view held upright, at least one held sideways.
    /// - The Music card's two lines read at 4.5:1 on the card, on every screen.
    /// - On a PC the approved arrangement stays: the grid under the mode row and above the chosen character's box.
    /// - A rotation lays the screen out again without rebuilding it: the chosen character and the controls stay.
    /// Saves go to a throw-away file.
    /// </summary>
    public class CharacterSelectResponsivePlayModeTests : GameplayPlayModeTestBase
    {
        private IEnumerator OpenSelect()
        {
            SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load");
            GameServices.Instance.Get<SaveService>().UseFile(System.IO.Path.Combine(Application.temporaryCachePath, "playmode_test_save.json"));
            Object.FindFirstObjectByType<MainMenuNavigation>().ShowCharacters();
            for (int i = 0; i < 3; i++) { Canvas.ForceUpdateCanvases(); yield return null; }
        }

        [UnityTest]
        public IEnumerator CharacterSelect_FitsEveryScreen_ClosingButtonsPinned_TextReadable()
        {
            yield return OpenSelect();
            var ui = Object.FindFirstObjectByType<CharacterSelectionUI>();
            Assert.IsNotNull(ui, "The character select screen. " + Errors());
            Assert.IsNotNull(ui.ContentRoot, "It built its responsive layout.");
            Assert.IsNotNull(ui.ContentRoot.GetComponent<SafeAreaFitter>(), "The content sits in the safe area.");
            var canvas = ui.ContentRoot.GetComponentInParent<Canvas>().rootCanvas;
            int selected = ui.SelectedIndex;
            int cards = ui.Cards.Count;
            var cardIds = ui.Cards.Select(c => c.GetInstanceID()).ToList();

            var problems = new List<string>();
            try
            {
                foreach (var d in ScreenSimulation.Devices)
                {
                    yield return ScreenSimulation.At(d.Pixels, d.Dpi, d.Touch, canvas);
                    yield return null; // the screen's own end-of-frame pass and the grid's cells
                    Canvas.ForceUpdateCanvases();
                    problems.AddRange(Check(d.Name, ui, d.Touch));

                    var expected = !d.Touch ? CharacterSelectionUI.ScreenLayout.ThreeColumns : CharacterSelectionUI.ScreenLayout.Stacked;
                    if (ui.CurrentLayout != expected) problems.Add($"{d.Name}: laid out as {ui.CurrentLayout}, expected {expected}");
                    bool portrait = d.Pixels.y > d.Pixels.x;
                    var grid = ui.Cards[0].transform.parent.GetComponent<GridLayoutGroup>();
                    // At 16 sp body text the phone's canvas is about 630 units wide: four 140-unit cards fit (three at the old 20 sp).
                    if (d.Touch && portrait && grid.constraintCount != 4) problems.Add($"{d.Name}: the card grid has {grid.constraintCount} columns, expected 4");
                    if (d.Touch)
                    {
                        // The cards are the first thing seen: whole rows of them inside the body's window, no scrolling.
                        int rows = FullRowsInView(ui);
                        int need = portrait ? 2 : 1;
                        if (rows < need) problems.Add($"{d.Name}: {rows} whole row(s) of cards in view, expected at least {need}");
                        // One scroll: the grid shows every card (its own scroll is off) and the chosen character comes first.
                        var gridScroll = ui.GridBox.GetComponent<ScrollRect>();
                        if (gridScroll != null && gridScroll.enabled) problems.Add($"{d.Name}: the grid still scrolls inside its own box");
                        Rect info = ScreenSimulation.UnitsRect(ui.InfoBox), gridBox = ScreenSimulation.UnitsRect(ui.GridBox), modes = ScreenSimulation.UnitsRect(ui.ModesBox);
                        if (info.yMin < gridBox.yMax - 1f) problems.Add($"{d.Name}: the chosen character's box {info} is not above the grid {gridBox}");
                        if (gridBox.yMin < modes.yMax - 1f) problems.Add($"{d.Name}: the grid {gridBox} is not above the modes {modes}");
                    }
                    else
                    {
                        // A PC keeps the approved arrangement: the mode row, the grid under it, the chosen character under that.
                        Rect info = ScreenSimulation.UnitsRect(ui.InfoBox), gridBox = ScreenSimulation.UnitsRect(ui.GridBox), modes = ScreenSimulation.UnitsRect(ui.ModesBox);
                        if (gridBox.yMin < info.yMax - 1f) problems.Add($"{d.Name}: the grid {gridBox} is not above the chosen character's box {info}");
                        if (modes.yMin < gridBox.yMax - 1f) problems.Add($"{d.Name}: the grid {gridBox} reaches into the mode row {modes}");
                        if (Mathf.Abs(gridBox.center.x - info.center.x) > 1f || Mathf.Abs(gridBox.width - info.width) > 1f || Mathf.Abs(gridBox.width - modes.width) > 1f)
                            problems.Add($"{d.Name}: the modes {modes}, grid {gridBox} and info {info} are not one column");
                        if (ui.BodyScroll.vertical) problems.Add($"{d.Name}: the body scrolls on a PC");
                        if (d.Name == "PC 1920x1080")
                        {
                            if (grid.constraintCount != 5) problems.Add($"{d.Name}: the card grid has {grid.constraintCount} columns, the approved look has 5");
                            foreach (var card in ui.Cards.Where(c => c.gameObject.activeInHierarchy))
                                if (!Inside(ScreenSimulation.UnitsRect((RectTransform)card.transform), gridBox)) problems.Add($"{d.Name}: {card.name} is outside the grid's box");
                        }
                    }
                    // Every card shows its character's name (owner's phone, 2026-10-12: upright, the name's band was lower
                    // than 12 sp text and the name was cut away whole).
                    foreach (var card in ui.Cards.Where(c => c.gameObject.activeInHierarchy))
                    {
                        var name = card.transform.Find("Name").GetComponent<TMPro.TMP_Text>();
                        name.ForceMeshUpdate();
                        int drawn = name.textInfo.characterInfo.Take(name.textInfo.characterCount).Count(ch => ch.isVisible);
                        if (drawn < Mathf.Min(3, name.text.Length)) problems.Add($"{d.Name}: {card.name} draws {drawn} letter(s) of its name '{name.text}' (band {name.rectTransform.rect.height:0.#} units, text {name.fontSize:0.#})");
                    }
                    // The Music card's lines read on the card (they were light text on the pale card).
                    var music = ui.MusicButton;
                    if (ui.MusicCardLabels.Count < 2) problems.Add($"{d.Name}: the Music card has {ui.MusicCardLabels.Count} lines, expected 2");
                    foreach (var label in ui.MusicCardLabels)
                    {
                        float normal = UITheme.Contrast(label.color, music.colors.normalColor);
                        float chosen = UITheme.Contrast(label.color, music.colors.selectedColor);
                        if (normal < 4.5f) problems.Add($"{d.Name}: Music card '{label.text}' reads at {normal:0.0}:1 on the card");
                        if (chosen < 4.5f) problems.Add($"{d.Name}: Music card '{label.text}' reads at {chosen:0.0}:1 when chosen");
                    }
                }
            }
            finally { ScreenSimulation.Restore(); }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));

            // Not rebuilt: the same cards, the same choice.
            Assert.That(ui.Cards.Count, Is.EqualTo(cards));
            Assert.That(ui.Cards.Select(c => c.GetInstanceID()), Is.EqualTo(cardIds), "The rotation kept the same cards (no rebuild).");
            Assert.That(ui.SelectedIndex, Is.EqualTo(selected), "The chosen character stays.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator MusicScreen_FitsEveryScreen_DonePinned_TextReadable()
        {
            AudioManagerSetup();
            yield return OpenSelect();
            var ui = Object.FindFirstObjectByType<CharacterSelectionUI>();
            ui.OpenMusic();
            for (int i = 0; i < 3; i++) { Canvas.ForceUpdateCanvases(); yield return null; }
            var screen = ui.MusicScreen;
            Assert.That(screen != null && screen.IsOpen, Is.True, "The Music screen opens. " + Errors());
            var canvas = screen.GetComponent<Canvas>().rootCanvas;
            Assert.IsNotNull(screen.Window.GetComponentInParent<SafeAreaFitter>(), "The window sits in the safe area.");

            var problems = new List<string>();
            try
            {
                foreach (var d in ScreenSimulation.Devices)
                {
                    yield return ScreenSimulation.At(d.Pixels, d.Dpi, d.Touch, canvas);
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    var fixedControls = screen.Tempos.Cast<Selectable>().Concat(new Selectable[] { screen.LockButton, screen.DoneButton }).ToList();
                    problems.AddRange(ScreenSimulation.Problems(d.Name, fixedControls));
                    Rect window = ScreenSimulation.UnitsRect(screen.Window);
                    Vector2 size = ((RectTransform)canvas.transform).rect.size;
                    if (window.xMin < -1f || window.yMin < -1f || window.xMax > size.x + 1f || window.yMax > size.y + 1f)
                        problems.Add($"{d.Name}: the window {window} is off the {size} canvas");
                    foreach (var c in fixedControls)
                        if (!Inside(ScreenSimulation.UnitsRect((RectTransform)c.transform), window)) problems.Add($"{d.Name}: {c.name} is outside the window");
                    // The tracks scroll inside their area: inside its width, clear of each other and of the rows below.
                    Rect area = ScreenSimulation.UnitsRect(screen.TrackArea);
                    foreach (var c in fixedControls)
                        if (Overlap(area, ScreenSimulation.UnitsRect((RectTransform)c.transform))) problems.Add($"{d.Name}: the track area covers {c.name}");
                    problems.AddRange(ScrolledProblems(d.Name, screen.Tracks.Cast<Selectable>().ToList(), area.xMin, area.xMax, d.Touch));
                    if (d.Touch) problems.AddRange(TextFloor(d.Name, screen.Window));
                }
            }
            finally { ScreenSimulation.Restore(); }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
            Click(screen.DoneButton, "Done");
            Assert.That(screen.IsOpen, Is.False, "Done closes the screen.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        /// <summary>Rows of the card grid whose every card lies wholly inside the scrolling body's window.</summary>
        private static int FullRowsInView(CharacterSelectionUI ui)
        {
            Rect body = ScreenSimulation.UnitsRect(ui.BodyViewport);
            var rows = new Dictionary<int, bool>();
            foreach (var card in ui.Cards.Where(c => c.gameObject.activeInHierarchy))
            {
                Rect r = ScreenSimulation.UnitsRect((RectTransform)card.transform);
                int row = Mathf.RoundToInt(r.center.y);
                bool inside = Inside(r, body);
                rows[row] = rows.TryGetValue(row, out bool soFar) ? soFar && inside : inside;
            }
            return rows.Values.Count(v => v);
        }

        private static void AudioManagerSetup() => SoulHunter.Gameplay.Audio.AudioManager.EnsureInstance();

        /// <summary>Every rule for the character select screen on one simulated device.</summary>
        private static List<string> Check(string device, CharacterSelectionUI ui, bool touch)
        {
            var problems = new List<string>();
            var canvasSize = ((RectTransform)ui.ContentRoot.GetComponentInParent<Canvas>().rootCanvas.transform).rect.size;

            // Back and Confirm: on the canvas, apart, 48 dp on the phone.
            var closing = new Selectable[] { ui.BackButton, ui.ConfirmButton };
            if (closing.Any(c => c == null)) { problems.Add($"{device}: Back or Confirm missing"); return problems; }
            problems.AddRange(ScreenSimulation.Problems(device, closing));
            Rect body = ScreenSimulation.UnitsRect(ui.BodyViewport);
            if (body.xMin < -1f || body.yMin < -1f || body.xMax > canvasSize.x + 1f || body.yMax > canvasSize.y + 1f)
                problems.Add($"{device}: the scrolling body {body} is off the {canvasSize} canvas");
            if (!touch)
            {
                // PC keeps the approved look: Back centred at 86.5% across and 93.5% up (top right, above the body), Confirm at
                // 85.5% across and 13.5% up (bottom right, under the filter column), both at the theme's button size.
                var theme = UiThemeAsset.Active;
                Vector2 standard = new Vector2(theme.Layouts.ButtonWidth, theme.Layouts.ButtonHeight);
                Rect content = ScreenSimulation.UnitsRect(ui.ContentRoot);
                foreach (var (button, spot) in new[] { (closing[0], new Vector2(0.865f, 0.935f)), (closing[1], new Vector2(0.855f, 0.135f)) })
                {
                    Rect r = ScreenSimulation.UnitsRect((RectTransform)button.transform);
                    Vector2 expected = content.min + Vector2.Scale(content.size, spot);
                    if ((r.center - expected).magnitude > 1.5f) problems.Add($"{device}: {button.name} is centred at {r.center}, its approved spot is {expected}");
                    if ((r.size - standard).magnitude > 1f) problems.Add($"{device}: {button.name} is {r.size}, the theme's button is {standard}");
                    // Clear of every section (no panel drawn under it).
                    foreach (RectTransform section in ui.ScrollContent)
                        if (section.gameObject.activeInHierarchy && Overlap(r, ScreenSimulation.UnitsRect(section))) problems.Add($"{device}: {button.name} covers the {section.name} section");
                }
                if (ScreenSimulation.UnitsRect((RectTransform)closing[0].transform).yMin < body.yMax - 1f) problems.Add($"{device}: Back reaches into the body");
            }
            else
            {
                // A phone: both pinned in the action row under the body.
                foreach (var c in closing)
                    if (Overlap(body, ScreenSimulation.UnitsRect((RectTransform)c.transform))) problems.Add($"{device}: the body covers {c.name}");
                foreach (var c in closing)
                    if (ScreenSimulation.UnitsRect((RectTransform)c.transform).yMax > body.yMin + 1f) problems.Add($"{device}: {c.name} is not pinned under the body");
            }

            // Everything in the body: inside the body's width, apart, 48 dp on the phone; and reachable by scrolling.
            var scrolled = ui.ScrollContent.GetComponentsInChildren<Selectable>().Where(s => s.isActiveAndEnabled).ToList();
            if (scrolled.Count < ui.Cards.Count + 8 + 3 + 10 + 2) problems.Add($"{device}: only {scrolled.Count} controls in the body");
            problems.AddRange(ScrolledProblems(device, scrolled, body.xMin, body.xMax, touch));
            problems.AddRange(ScreenSimulation.LabelProblems(device, scrolled)); // every tab's label inside it
            // A row's tabs share one width (the widest label's), so the chips read as one set.
            foreach (var row in ui.ScrollContent.GetComponentsInChildren<ThemedRow>())
            {
                var widths = row.GetComponentsInChildren<ThemedElement>().Where(e => e.isActiveAndEnabled && e.transform.parent == row.transform && e.Size == ThemedElement.SizeRole.Tab)
                    .Select(e => ((RectTransform)e.transform).rect.width).ToList();
                if (widths.Count > 1 && widths.Max() - widths.Min() > 0.5f) problems.Add($"{device}: the tabs of {row.name} are {widths.Min():0.#} to {widths.Max():0.#} wide, not one width");
            }
            Rect sections = ScreenSimulation.UnitsRect(ui.ScrollContent);
            foreach (var s in scrolled)
            {
                Rect r = ScreenSimulation.UnitsRect((RectTransform)s.transform);
                if (s.transform.parent != null && s.transform.parent.GetComponent<GridLayoutGroup>() != null && s.name.StartsWith("Card_")) continue; // checked by the grid
                if (r.yMin < sections.yMin - 1f || r.yMax > sections.yMax + 1f) problems.Add($"{device}: {s.name} {r} is outside the body's content {sections}");
            }
            if (ui.BodyScroll.vertical != sections.height > body.height + 1f) problems.Add($"{device}: the body scrolls = {ui.BodyScroll.vertical}, content {sections.height:0} vs view {body.height:0}");
            if (touch) problems.AddRange(TextFloor(device, ui.ContentRoot));
            return problems;
        }

        /// <summary>Controls that may scroll: inside [xMin, xMax], none overlapping another, none under 48 dp on touch.</summary>
        private static List<string> ScrolledProblems(string device, List<Selectable> controls, float xMin, float xMax, bool touch)
        {
            var problems = new List<string>();
            var boxes = controls.Select(c => (c.name, ScreenSimulation.UnitsRect((RectTransform)c.transform))).ToList();
            float min = UiScreen.MinTouchUnits;
            foreach (var (name, box) in boxes)
            {
                if (box.xMin < xMin - 1f || box.xMax > xMax + 1f) problems.Add($"{device}: {name} {box} sticks out of [{xMin:0}, {xMax:0}]");
                if (touch && Mathf.Min(box.width, box.height) < min - 0.5f) problems.Add($"{device}: {name} is {box.size}, under the 48 dp touch target ({min:0.#} units)");
            }
            for (int i = 0; i < boxes.Count; i++)
                for (int j = i + 1; j < boxes.Count; j++)
                    if (Overlap(boxes[i].Item2, boxes[j].Item2)) problems.Add($"{device}: {boxes[i].Item1} and {boxes[j].Item1} overlap");
            return problems;
        }

        private static List<string> TextFloor(string device, Transform root)
        {
            var problems = new List<string>();
            float floor = UiScreen.MinTextUnits;
            foreach (var text in root.GetComponentsInChildren<TMP_Text>().Where(t => t.isActiveAndEnabled && !string.IsNullOrWhiteSpace(t.text)))
            {
                text.ForceMeshUpdate();
                if (text.fontSize < floor - 0.01f) problems.Add($"{device}: '{text.text}' ({text.name}) is {text.fontSize:0.#} units, under the {floor:0.#}-unit (12 sp) floor");
            }
            return problems;
        }

        private static bool Overlap(Rect a, Rect b) =>
            Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin) > 1f && Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin) > 1f;

        private static bool Inside(Rect r, Rect box) =>
            r.xMin >= box.xMin - 1f && r.xMax <= box.xMax + 1f && r.yMin >= box.yMin - 1f && r.yMax <= box.yMax + 1f;
    }
}
