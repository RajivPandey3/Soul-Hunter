using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// End-to-end game over flow: die, see the game over panel, click Restart, and get
    /// a fresh playable run whose game over panel works again.
    /// </summary>
    public class GameOverFlowPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator PlayerDeath_ShowsGameOverPanel_AndRestartStartsFreshRun()
        {
            yield return BootIntoGameplay();

            // First run: die and expect the panel.
            yield return KillPlayer();
            AssertGameOverPanelVisible("first run");

            var firstPlayer = PlayerController.Instance;
            Click(GetRestartButton(), "Restart");
            yield return WaitForFreshGameplay(firstPlayer);

            Assert.AreEqual(1f, Time.timeScale, "Game should be running after Restart. " + Errors());
            Assert.IsFalse(GameSessionManager.Instance.IsRunFinished, "New run should not start finished. " + Errors());
            Assert.IsFalse(GetPanel().activeInHierarchy, "Game over panel should be hidden at the start of the new run. " + Errors());

            // Second run: the panel must work again after a restart.
            yield return KillPlayer();
            AssertGameOverPanelVisible("second run after Restart");
        }

        private IEnumerator KillPlayer()
        {
            var health = PlayerController.Instance.GetComponent<HealthController>();
            Assert.IsNotNull(health, "Player has no HealthController.");

            // Revivals (meta upgrades) can bring the player back; keep killing until the run ends.
            for (int attempt = 0; attempt < 10 && !GameSessionManager.Instance.IsRunFinished; attempt++)
            {
                health.IsInvincible = false;
                health.KillSilently();
                yield return null;
            }

            Assert.IsTrue(GameSessionManager.Instance.IsRunFinished, "Player death did not end the run (GameOver never fired). " + Errors());
            yield return null;
        }

        private void AssertGameOverPanelVisible(string when)
        {
            var panel = GetPanel();
            Assert.IsNotNull(panel, $"[{when}] GameOverUI has no panel assigned.");
            Assert.AreEqual(1, Object.FindObjectsByType<GameOverUI>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                $"[{when}] Expected exactly one GameOverUI; duplicates fight over the same panel.");
            Assert.IsTrue(panel.activeInHierarchy, $"[{when}] Game over panel is not visible after death. " + Errors());
            Assert.AreEqual(0f, Time.timeScale, $"[{when}] Game should be paused on game over. " + Errors());

            var button = GetRestartButton();
            Assert.IsNotNull(button, $"[{when}] GameOverUI has no Restart button assigned.");
            Assert.IsTrue(button.gameObject.activeInHierarchy && button.IsInteractable(), $"[{when}] Restart button is not clickable.");
        }

        private static GameOverUI UI()
        {
            var ui = Object.FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);
            Assert.IsNotNull(ui, "No GameOverUI in the loaded scenes.");
            return ui;
        }

        private static GameObject GetPanel() => GetPrivateField<GameObject>(UI(), "_gameOverPanel");
        private static Button GetRestartButton() => GetPrivateField<Button>(UI(), "_restartButton");
    }
}
