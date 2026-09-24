using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SoulHunter.Core.Scenes;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// End-to-end game over flow: boot like a real launch, enter Main_Gameplay,
    /// die, see the game over panel, click Restart, and get a fresh playable run
    /// whose game over panel works again.
    /// </summary>
    public class GameOverFlowPlayModeTests
    {
        private const float TimeoutSeconds = 20f;
        private readonly List<string> _errors = new List<string>();

        [SetUp]
        public void SetUp()
        {
            _errors.Clear();
            Application.logMessageReceived += CaptureErrors;
            // Report unrelated runtime errors in the failure message instead of
            // failing on the first one, so the game over result stays visible.
            LogAssert.ignoreFailingMessages = true;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Application.logMessageReceived -= CaptureErrors;
            Time.timeScale = 1f;

            // Leave a clean slate so later tests don't inherit this run's singletons
            // (LevelProgressionManager, GameSessionManager, GameServices, ...).
            var probe = new GameObject("DontDestroyOnLoadProbe");
            Object.DontDestroyOnLoad(probe);
            foreach (var root in probe.scene.GetRootGameObjects())
                Object.Destroy(root);

            var empty = SceneManager.CreateScene("GameOverFlowTestCleanup");
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene != empty && scene.isLoaded)
                    yield return SceneManager.UnloadSceneAsync(scene);
            }
            yield return null;
        }

        private void CaptureErrors(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
                _errors.Add($"{type}: {condition}");
        }

        [UnityTest]
        public IEnumerator PlayerDeath_ShowsGameOverPanel_AndRestartStartsFreshRun()
        {
            yield return BootToMainMenu();

            var sceneService = GameServices.Instance.Get<SceneService>();
            sceneService.LoadSceneAsync("Main_Gameplay");
            yield return WaitForFreshGameplay(previousPlayer: null);

            // First run: die and expect the panel.
            yield return KillPlayer();
            AssertGameOverPanelVisible("first run");

            // Click Restart through the EventSystem, like a real mouse click.
            var firstPlayer = PlayerController.Instance;
            ClickRestart();
            yield return WaitForFreshGameplay(firstPlayer);

            Assert.AreEqual(1f, Time.timeScale, "Game should be running after Restart. " + Errors());
            Assert.IsFalse(GameSessionManager.Instance.IsRunFinished, "New run should not start finished. " + Errors());
            Assert.IsFalse(GetPanel().activeInHierarchy, "Game over panel should be hidden at the start of the new run. " + Errors());

            // Second run: the panel must work again after a restart.
            yield return KillPlayer();
            AssertGameOverPanelVisible("second run after Restart");
        }

        private IEnumerator BootToMainMenu()
        {
            var load = SceneManager.LoadSceneAsync("Bootstrap");
            Assert.IsNotNull(load, "Bootstrap is not in Build Settings.");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load after Bootstrap");
            Assert.IsNotNull(GameServices.Instance, "GameServices should exist after Bootstrap.");
        }

        private IEnumerator WaitForFreshGameplay(PlayerController previousPlayer)
        {
            yield return WaitUntilRealtime(() =>
                SceneManager.GetActiveScene().name == "Main_Gameplay" &&
                PlayerController.Instance != null &&
                PlayerController.Instance != previousPlayer &&
                GameSessionManager.Instance != null &&
                Object.FindFirstObjectByType<GameOverUI>() != null,
                previousPlayer == null ? "Main_Gameplay to load" : "Main_Gameplay to reload after Restart");

            // Let Start() run on every object and the session tick once.
            for (int i = 0; i < 10; i++) yield return null;
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
            Assert.IsTrue(panel.activeInHierarchy, $"[{when}] Game over panel is not visible after death. {Diagnose(panel)} " + Errors());
            Assert.AreEqual(0f, Time.timeScale, $"[{when}] Game should be paused on game over. " + Errors());

            var button = GetRestartButton();
            Assert.IsNotNull(button, $"[{when}] GameOverUI has no Restart button assigned.");
            Assert.IsTrue(button.gameObject.activeInHierarchy && button.IsInteractable(), $"[{when}] Restart button is not clickable.");
            Assert.IsNotNull(EventSystem.current, $"[{when}] No active EventSystem, so UI clicks cannot work.");
        }

        private void ClickRestart()
        {
            var button = GetRestartButton();
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            bool handled = ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.IsTrue(handled, "Restart button did not handle the click.");
        }

        private static GameObject GetPanel() => GetField<GameObject>("_gameOverPanel");
        private static Button GetRestartButton() => GetField<Button>("_restartButton");

        private static T GetField<T>(string name) where T : class
        {
            var ui = Object.FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);
            Assert.IsNotNull(ui, "No GameOverUI in the loaded scenes.");
            var field = typeof(GameOverUI).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            return field.GetValue(ui) as T;
        }

        private IEnumerator WaitUntilRealtime(System.Func<bool> condition, string what)
        {
            float deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Timed out after {TimeoutSeconds}s waiting for {what}. Active scene: {SceneManager.GetActiveScene().name}. " + Errors());
                yield return null;
            }
        }

        private static string Diagnose(GameObject panel)
        {
            var uis = Object.FindObjectsByType<GameOverUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var sb = new System.Text.StringBuilder();
            sb.Append($"\npanel '{panel.name}' activeSelf={panel.activeSelf}; inactive ancestors:");
            for (var t = panel.transform.parent; t != null; t = t.parent)
                if (!t.gameObject.activeSelf) sb.Append($" '{t.name}'");
            sb.Append($"\nGameOverUI count={uis.Length}");
            var sessionField = typeof(GameOverUI).GetField("_session", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var ui in uis)
            {
                var session = sessionField.GetValue(ui) as GameSessionManager;
                var uiPanel = typeof(GameOverUI).GetField("_gameOverPanel", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui) as GameObject;
                sb.Append($"\n - on '{ui.gameObject.name}' activeInHierarchy={ui.gameObject.activeInHierarchy} enabled={ui.enabled} " +
                          $"subscribedSession={(session == null ? "NULL" : session == GameSessionManager.Instance ? "current" : "STALE")} " +
                          $"panel={(uiPanel == null ? "NULL" : uiPanel == panel ? "same" : uiPanel.name + " activeSelf=" + uiPanel.activeSelf)}");
            }
            return sb.ToString();
        }

        private string Errors() => _errors.Count == 0 ? "(no errors logged)" : "Logged errors:\n" + string.Join("\n", _errors);
    }
}
