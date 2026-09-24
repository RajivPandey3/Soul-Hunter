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
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Shared setup for end-to-end tests that boot the real game (Bootstrap -> MainMenu
    /// -> Main_Gameplay), capture logged errors, and leave a clean slate afterwards.
    /// </summary>
    public abstract class GameplayPlayModeTestBase
    {
        protected const float TimeoutSeconds = 20f;
        private readonly List<string> _errors = new List<string>();

        [SetUp]
        public void CaptureLogs()
        {
            _errors.Clear();
            Application.logMessageReceived += CaptureErrors;
            // Report unrelated runtime errors in the failure message instead of
            // failing on the first one, so the flow under test stays visible.
            LogAssert.ignoreFailingMessages = true;
        }

        [UnityTearDown]
        public IEnumerator CleanSlate()
        {
            Application.logMessageReceived -= CaptureErrors;
            Time.timeScale = 1f;

            // Later tests must not inherit this run's singletons
            // (LevelProgressionManager, GameSessionManager, GameServices, ...).
            var probe = new GameObject("DontDestroyOnLoadProbe");
            Object.DontDestroyOnLoad(probe);
            foreach (var root in probe.scene.GetRootGameObjects())
                Object.Destroy(root);

            var empty = SceneManager.CreateScene("PlayModeTestCleanup");
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

        protected string Errors() => _errors.Count == 0 ? "(no errors logged)" : "Logged errors:\n" + string.Join("\n", _errors);

        /// <summary>Boots like a real launch and enters Main_Gameplay through SceneService.</summary>
        protected IEnumerator BootIntoGameplay()
        {
            var load = SceneManager.LoadSceneAsync("Bootstrap");
            Assert.IsNotNull(load, "Bootstrap is not in Build Settings.");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load after Bootstrap");
            Assert.IsNotNull(GameServices.Instance, "GameServices should exist after Bootstrap.");

            GameServices.Instance.Get<SceneService>().LoadSceneAsync("Main_Gameplay");
            yield return WaitForFreshGameplay(previousPlayer: null);
        }

        protected IEnumerator WaitForFreshGameplay(PlayerController previousPlayer)
        {
            yield return WaitUntilRealtime(() =>
                SceneManager.GetActiveScene().name == "Main_Gameplay" &&
                PlayerController.Instance != null &&
                PlayerController.Instance != previousPlayer &&
                GameSessionManager.Instance != null,
                previousPlayer == null ? "Main_Gameplay to load" : "Main_Gameplay to reload");

            // Let Start() run on every object and the session tick once.
            for (int i = 0; i < 10; i++) yield return null;
        }

        protected IEnumerator WaitUntilRealtime(System.Func<bool> condition, string what)
        {
            float deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Timed out after {TimeoutSeconds}s waiting for {what}. Active scene: {SceneManager.GetActiveScene().name}. " + Errors());
                yield return null;
            }
        }

        /// <summary>Clicks a UI button through the EventSystem, like a real mouse click.</summary>
        protected static void Click(Button button, string label)
        {
            Assert.IsNotNull(button, $"{label} button is not assigned.");
            Assert.IsTrue(button.gameObject.activeInHierarchy && button.IsInteractable(), $"{label} button is not clickable.");
            Assert.IsNotNull(EventSystem.current, "No active EventSystem, so UI clicks cannot work.");
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            Assert.IsTrue(ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler),
                $"{label} button did not handle the click.");
        }

        protected static T GetPrivateField<T>(object owner, string name) where T : class
        {
            var field = owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"{owner.GetType().Name} has no field '{name}'.");
            return field.GetValue(owner) as T;
        }
    }
}
