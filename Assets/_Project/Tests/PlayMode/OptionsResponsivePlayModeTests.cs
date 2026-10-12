using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.UI;
using SoulHunter.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Owner (2026-10-10): the same Options screen in landscape and portrait, adjusting itself. Laid out on a PC (16:9, 21:9,
    /// 5:4) and on the owner's phone held either way (1080x2400 at 480 dpi, touch), from the main menu and over the pause
    /// menu: every tab, every row in view and Back on the canvas, nothing overlapping, nothing under 48 dp on the phone,
    /// no text under 12 sp, each list scrolling to its last row, and the control under its label on a portrait phone.
    /// </summary>
    public class OptionsResponsivePlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator Options_FromTheMainMenu_FitsEveryScreen()
        {
            SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu");
            for (int i = 0; i < 3; i++) yield return null;
            var nav = Object.FindFirstObjectByType<MainMenuNavigation>();
            Click(nav.OptionsButton, "Options");
            yield return null;
            var options = OptionsMenuUI.Current;
            Assert.IsNotNull(options, "Options opened. " + Errors());
            try
            {
                yield return CheckEveryDeviceAndTab(options);
            }
            finally { Object.Destroy(options.gameObject); } // not Close(): nothing is saved
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator Options_OverThePauseMenu_FitsEveryScreen()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var system = GameSystemManager.Instance;
            system.TogglePause();
            yield return null;
            yield return null;
            var pause = Object.FindFirstObjectByType<PauseMenuUI>();
            Assert.IsNotNull(pause, "The pause menu.");
            Click(pause.OptionsButton, "Options");
            yield return null;
            var options = OptionsMenuUI.Current;
            Assert.IsNotNull(options, "Options opened over the pause menu. " + Errors());
            try
            {
                yield return CheckEveryDeviceAndTab(options);
            }
            finally { Object.Destroy(options.gameObject); }
            system.Resume();
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        /// <summary>The open section's controls that are fully in view, plus the tabs and Back.</summary>
        private static List<Selectable> ShownControls(OptionsMenuUI options)
        {
            var section = options.Sections[options.CurrentTab];
            Rect view = ScreenSimulation.UnitsRect((RectTransform)section.transform);
            var shown = section.content.GetComponentsInChildren<Selectable>()
                .Where(s => InView(s, view)).ToList();
            shown.AddRange(options.TabButtons);
            shown.Add(options.BackButton);
            return shown;
        }

        private static bool InView(Selectable control, Rect view)
        {
            Rect r = ScreenSimulation.UnitsRect((RectTransform)control.transform);
            return r.yMin >= view.yMin - 1f && r.yMax <= view.yMax + 1f;
        }

        private IEnumerator CheckEveryDeviceAndTab(OptionsMenuUI options)
        {
            var canvas = options.GetComponent<Canvas>();
            Assert.That(canvas.GetComponent<ResponsiveCanvas>(), Is.Not.Null, "Options takes the game's one UI scale.");

            // The shared check on the section that opens first.
            options.ShowTab(0);
            yield return ScreenSimulation.AssertFitsEveryDevice(() => ShownControls(options), canvas);

            var problems = new List<string>();
            try
            {
                foreach (var d in ScreenSimulation.Devices)
                {
                    yield return ScreenSimulation.At(d.Pixels, d.Dpi, d.Touch, canvas);
                    bool portraitPhone = d.Touch && d.Pixels.y > d.Pixels.x;
                    if (options.IsStacked != portraitPhone)
                        problems.Add($"{d.Name}: rows {(options.IsStacked ? "stacked" : "side by side")}, expected {(portraitPhone ? "stacked" : "side by side")}");
                    for (int tab = 0; tab < options.TabButtons.Count; tab++)
                    {
                        options.ShowTab(tab);
                        for (int f = 0; f < 2; f++) { Canvas.ForceUpdateCanvases(); yield return null; }
                        Canvas.ForceUpdateCanvases();
                        string where = $"{d.Name} {options.TabButtons[tab].name}";
                        var section = options.Sections[tab];
                        Rect view = ScreenSimulation.UnitsRect((RectTransform)section.transform);
                        Rect back = ScreenSimulation.UnitsRect((RectTransform)options.BackButton.transform);

                        problems.AddRange(ScreenSimulation.Problems(where, ShownControls(options)));
                        if (back.yMax > view.yMin + 1f) problems.Add($"{where}: Back {back} reaches into the list {view}");
                        var all = section.content.GetComponentsInChildren<Selectable>().ToList();
                        if (all.Count > 0 && !all.Any(s => InView(s, view))) problems.Add($"{where}: no row fits in the list's room {view}");

                        // Label and control of a row never overlap; the label stays inside its row.
                        foreach (var control in all)
                        {
                            var line = control.transform;
                            while (line.parent != null && line.parent != section.content) line = line.parent;
                            var label = line.Find("Label");
                            if (label == null) continue;
                            Rect l = ScreenSimulation.UnitsRect((RectTransform)label), c = ScreenSimulation.UnitsRect((RectTransform)control.transform),
                                 row = ScreenSimulation.UnitsRect((RectTransform)line);
                            if (l.Overlaps(c)) problems.Add($"{where}: {control.name}'s label {l} and control {c} overlap");
                            if (c.xMax > row.xMax + 1f || c.yMin < row.yMin - 1f) problems.Add($"{where}: {control.name} {c} runs out of its row {row}");
                        }

                        // No text under the floor (12 sp on the phone).
                        float floor = UiScreen.MinTextUnits;
                        foreach (var text in options.GetComponentsInChildren<TMP_Text>().Where(t => !string.IsNullOrWhiteSpace(t.text)))
                        {
                            text.ForceMeshUpdate();
                            if (text.fontSize < floor - 0.01f) problems.Add($"{where}: '{text.text}' at {text.fontSize:0.#} units, under the {floor:0.#} floor");
                        }

                        // The list scrolls to its last row, which then is fully in view.
                        var content = section.content;
                        float max = Mathf.Max(0f, content.rect.height - ((RectTransform)section.transform).rect.height);
                        content.anchoredPosition = new Vector2(0f, max);
                        Canvas.ForceUpdateCanvases();
                        yield return null;
                        Canvas.ForceUpdateCanvases();
                        if (all.Count > 0 && !InView(all[all.Count - 1], ScreenSimulation.UnitsRect((RectTransform)section.transform)))
                            problems.Add($"{where}: the last row {all[all.Count - 1].name} cannot be scrolled into view");
                        content.anchoredPosition = Vector2.zero;

                        // Keyboard/controller focus on the last row brings it into view.
                        if (all.Count > 1)
                        {
                            ResponsiveScroll.KeepInView(section, (RectTransform)all[all.Count - 1].transform, wholeRow: true);
                            Canvas.ForceUpdateCanvases();
                            if (!InView(all[all.Count - 1], ScreenSimulation.UnitsRect((RectTransform)section.transform)))
                                problems.Add($"{where}: focusing {all[all.Count - 1].name} does not scroll it into view");
                            content.anchoredPosition = Vector2.zero;
                        }
                    }
                }
            }
            finally { ScreenSimulation.Restore(); }
            options.ShowTab(0);
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }
    }
}
