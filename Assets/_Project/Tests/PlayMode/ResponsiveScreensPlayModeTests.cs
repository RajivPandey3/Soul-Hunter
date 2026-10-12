using System.Collections;
using System.Linq;
using NUnit.Framework;
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
    /// Owner (2026-10-10): the game plays in landscape and portrait from the same screens, which adjust themselves. Each
    /// screen is laid out on a PC (16:9, 21:9, 5:4) and on the owner's phone held either way (1080x2400 at 480 dpi,
    /// touch): every control on the canvas, none overlapping another, none under 48 dp on the phone.
    /// </summary>
    public class ResponsiveScreensPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator TheMainMenu_FitsEveryScreen()
        {
            SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu");
            for (int i = 0; i < 5; i++) yield return null;
            var layout = Object.FindFirstObjectByType<MainMenuLayout>();
            Assert.IsNotNull(layout, "The main menu layout.");
            var canvas = layout.GetComponentInParent<Canvas>().rootCanvas;
            Assert.That(canvas.GetComponent<ResponsiveCanvas>(), Is.Not.Null, "The menu takes the game's one UI scale.");
            yield return ScreenSimulation.AssertFitsEveryDevice(() => layout.Buttons.Where(b => b != null), canvas);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator ThePauseScreen_FitsEveryScreen_WithItsStats()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var system = GameSystemManager.Instance;
            system.TogglePause(); // paused
            yield return null;
            yield return null;
            var pause = Object.FindFirstObjectByType<PauseMenuUI>();
            Assert.IsNotNull(pause, "The pause menu.");
            var canvas = pause.GetComponentInParent<Canvas>().rootCanvas;
            var stats = pause.GetComponentInChildren<StatsPanel>();
            Assert.IsNotNull(stats, "The stats panel shows while paused.");
            var problems = new System.Collections.Generic.List<string>();
            try
            {
                foreach (var d in ScreenSimulation.Devices)
                {
                    yield return ScreenSimulation.At(d.Pixels, d.Dpi, d.Touch, canvas);
                    problems.AddRange(ScreenSimulation.Problems(d.Name, pause.GetComponentsInChildren<Selectable>()));
                    // The stats never cover a button, and their text stays readable on the phone.
                    Rect s = ScreenSimulation.UnitsRect((RectTransform)stats.transform);
                    foreach (var b in pause.GetComponentsInChildren<Button>().Where(x => x.isActiveAndEnabled))
                        if (s.Overlaps(ScreenSimulation.UnitsRect((RectTransform)b.transform))) problems.Add($"{d.Name}: the stats cover {b.name}");
                    if (stats.FontSize < UiScreen.MinTextUnits - 0.01f) problems.Add($"{d.Name}: stats text {stats.FontSize:0.#} under {UiScreen.MinTextUnits:0.#} units");
                }
            }
            finally { ScreenSimulation.Restore(); }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
            system.Resume();
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
