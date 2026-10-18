using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// End-to-end end of a run (vampire.survivors.wiki/w/Results): die, see the Results screen with the game paused, press
    /// DONE and land on the main menu; a second run ends with a working Results screen again. Quitting from the pause menu
    /// also shows the Results screen first (v0.2.2: "quitting from the Pause menu doesn't skip the score page").
    /// </summary>
    public class GameOverFlowPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator PlayerDeath_ShowsResults_AndDoneGoesToTheMainMenu()
        {
            yield return BootIntoGameplay();

            yield return KillPlayer();
            var screen = AssertResultsVisible("first run", RunResults.EndKind.Death);
            Assert.AreEqual("GAME OVER", screen.TitleText.text, "A death's Results title.");

            Click(screen.DoneButton, "DONE");
            yield return WaitForMenuStayingPaused("death");
            Assert.AreEqual(1f, Time.timeScale, "The menu runs unpaused after DONE. " + Errors());

            // Second run from the menu (as Start would): the Results screen works again.
            SoulHunter.Core.Services.GameServices.Instance.Get<SoulHunter.Core.Scenes.SceneService>().LoadSceneAsync("Main_Gameplay");
            yield return WaitForFreshGameplay(previousPlayer: null);
            Assert.IsFalse(GameSessionManager.Instance.IsRunFinished, "A new run does not start finished. " + Errors());
            Assert.IsNull(UI().Screen, "No Results screen at the start of a run.");
            yield return KillPlayer();
            AssertResultsVisible("second run", RunResults.EndKind.Death);
        }

        [UnityTest]
        public IEnumerator QuitFromPause_ShowsResults_ThenDoneGoesToTheMainMenu()
        {
            yield return BootIntoGameplay();
            var system = GameSystemManager.Instance;
            Assert.IsNotNull(system, "GameSystemManager missing. " + Errors());
            system.TogglePause();
            yield return null;
            system.QuitToMainMenu();
            yield return null;

            Assert.AreEqual("Main_Gameplay", SceneManager.GetActiveScene().name, "Quit must not skip the Results screen.");
            Assert.IsTrue(GameSessionManager.Instance.IsQuit && GameSessionManager.Instance.IsRunFinished, "Quit ends the run.");
            var screen = AssertResultsVisible("quit", RunResults.EndKind.Quit);
            Assert.AreEqual("RESULTS", screen.TitleText.text);

            Click(screen.DoneButton, "DONE");
            yield return WaitForMenuStayingPaused("quit");
            Assert.AreEqual(1f, Time.timeScale, "The menu runs unpaused. " + Errors());
        }

        // Bug MIS UI-03: DONE used to resume time before the async MainMenu load, so gameplay ran under the Results
        // screen for the frames the load took. Every frame until the scene changes must still be paused.
        private IEnumerator WaitForMenuStayingPaused(string when)
        {
            float start = Time.realtimeSinceStartup;
            int frames = 0;
            while (SceneManager.GetActiveScene().name != "MainMenu")
            {
                if (GameSessionManager.Instance != null)
                    Assert.AreEqual(0f, Time.timeScale,
                        $"[{when}] Gameplay resumed under the Results screen {frames} frame(s) after DONE, before the menu loaded. " + Errors());
                Assert.Less(Time.realtimeSinceStartup - start, 30f, $"[{when}] DONE never loaded the main menu. " + Errors());
                frames++;
                yield return null;
            }
        }

        private IEnumerator KillPlayer()
        {
            var health = PlayerController.Instance.GetComponent<HealthController>();
            Assert.IsNotNull(health, "Player has no HealthController.");

            // Revivals (meta upgrades) can bring the player back through the Revive screen; keep killing until the run ends.
            for (int attempt = 0; attempt < 10 && !GameSessionManager.Instance.IsRunFinished; attempt++)
            {
                AnswerReviveScreen();
                health.IsInvincible = false;
                health.KillSilently();
                yield return null;
            }

            Assert.IsTrue(GameSessionManager.Instance.IsRunFinished, "Player death did not end the run (GameOver never fired). " + Errors());
            yield return null;
        }

        private RunResultsScreen AssertResultsVisible(string when, RunResults.EndKind end)
        {
            Assert.AreEqual(1, Object.FindObjectsByType<GameOverUI>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                $"[{when}] Expected exactly one GameOverUI.");
            var screen = UI().Screen;
            Assert.IsNotNull(screen, $"[{when}] No Results screen after the run ended. " + Errors());
            Assert.IsTrue(screen.isActiveAndEnabled, $"[{when}] The Results screen is not shown.");
            Assert.AreEqual(end, screen.Results.End, $"[{when}] Results end kind.");
            Assert.AreEqual(0f, Time.timeScale, $"[{when}] The game is paused under the Results screen. " + Errors());
            Assert.IsTrue(screen.DoneButton.gameObject.activeInHierarchy && screen.DoneButton.IsInteractable(), $"[{when}] DONE is not clickable.");
            Assert.AreEqual(1, Object.FindObjectsByType<RunResultsScreen>(FindObjectsSortMode.None).Length, $"[{when}] One Results screen.");
            Assert.AreEqual(1f, screen.transform.Find("Backdrop").GetComponent<UnityEngine.UI.Image>().color.a,
                $"[{when}] The backdrop is opaque (the game showed through a 0.97 layer in a build).");
            return screen;
        }

        private static GameOverUI UI()
        {
            var ui = Object.FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);
            Assert.IsNotNull(ui, "No GameOverUI in the loaded scenes.");
            return ui;
        }
    }
}
