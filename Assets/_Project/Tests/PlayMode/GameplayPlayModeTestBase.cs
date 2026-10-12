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
            // Every test plays on a copy of the owner's save; nothing a test does (a picked-up coin, an unlock) can
            // reach the real file. Tests that redirect again (UseFile) keep working.
            SoulHunter.Core.Persistence.SaveService.TestSavePath =
                System.IO.Path.Combine(Application.temporaryCachePath, "playmode_test_save_copy.json");
            _errors.Clear();
            // The start prompt (VS "PRESS TO START", 2026-10-12) shows once per launch and hides the menu's buttons until it
            // is answered. Tests reach the menu as on a later visit; the prompt's own test sets this back to false.
            SoulHunter.Gameplay.UI.StartPrompt.ShownThisLaunch = true;
            Application.logMessageReceived += CaptureErrors;
            // Report unrelated runtime errors in the failure message instead of
            // failing on the first one, so the flow under test stays visible.
            LogAssert.ignoreFailingMessages = true;
        }

        [UnityTearDown]
        public IEnumerator CleanSlate()
        {
            Application.logMessageReceived -= CaptureErrors;
            SoulHunter.Core.Services.GameTime.ResetAll();

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
        protected IEnumerator BootIntoGameplay(System.Action<SoulHunter.Core.Persistence.GameData> configureSave = null)
        {
            var load = SceneManager.LoadSceneAsync("Bootstrap");
            Assert.IsNotNull(load, "Bootstrap is not in Build Settings.");
            yield return WaitUntilRealtime(() => SceneManager.GetActiveScene().name == "MainMenu", "MainMenu to load after Bootstrap");
            Assert.IsNotNull(GameServices.Instance, "GameServices should exist after Bootstrap.");

            // Tests play as Kael whatever the owner last picked (their real save selected Kaelen on 2026-09-26, whose
            // starting weapon broke tests written for Kael's Whip), and every save the run makes goes to a throw-away
            // file, so the owner's save is never written. Tests that need another hero set it after this.
            var save = GameServices.Instance.Get<SoulHunter.Core.Persistence.SaveService>();
            save.UseFile(System.IO.Path.Combine(Application.temporaryCachePath, "playmode_test_save.json"));
            save.CurrentData.SelectedCharacterName = "Kael";
            // Tests play without VS run modes (Hurry, Hyper, ...) even if the owner turned some on; a test that needs
            // modes turns them on in configureSave, and the run copies them into RunModes when it starts.
            foreach (var mode in SoulHunter.Gameplay.Data.RunModeOptions.All)
                SoulHunter.Gameplay.Data.RunModeOptions.Set(save.CurrentData, mode, false);
            // Every maxed PowerUp on (VS On/Off, 2026-10-09), whatever the owner switched off; a test switches them itself.
            save.CurrentData.DisabledPowerUps?.Clear();
            // Each level's own music at normal speed, and the Inverse look on (the defaults), whatever the owner chose (P3-05).
            save.CurrentData.MusicChoice = 0;
            save.CurrentData.MusicTempo = 0;
            save.CurrentData.MusicLocked = false;
            save.CurrentData.VisuallyInvertStages = true;
            // Every item unlocked (VS item unlocks, 2026-10-12), so tests written for the whole level-up pool keep it; a
            // test of the locks calls ItemUnlocks.LockEverything in configureSave.
            SoulHunter.Core.Persistence.ItemUnlocks.UnlockEverything(save.CurrentData);
            configureSave?.Invoke(save.CurrentData);

            GameServices.Instance.Get<SceneService>().LoadSceneAsync("Main_Gameplay");
            yield return WaitForFreshGameplay(previousPlayer: null);
            yield return CloseOpenLevelUps();
            if (AutoCloseStuckLevelUps)
            {
                var levelUp = Object.FindFirstObjectByType<SoulHunter.Gameplay.Core.LevelUpManager>();
                if (levelUp != null && levelUp.GetComponent<LevelUpUnstick>() == null) levelUp.gameObject.AddComponent<LevelUpUnstick>();
            }
        }

        /// <summary>
        /// A test that waits on game time stalls forever behind a level-up screen nobody answers (three full runs stalled
        /// for an hour or more, 2026-10-02 to 2026-10-04, once Level 1 opened with 1-HP bats). With this on (the default),
        /// a level-up screen left open for <see cref="LevelUpUnstick.Seconds"/> real seconds takes its first card. A test
        /// that holds a level-up screen open longer turns it off before booting.
        /// </summary>
        protected bool AutoCloseStuckLevelUps = true;

        /// <summary>Takes the first card of a level-up screen open for longer than <see cref="Seconds"/> real seconds.</summary>
        public sealed class LevelUpUnstick : MonoBehaviour
        {
            public const float Seconds = 5f;
            private float _openSince = -1f;
            private SoulHunter.Gameplay.Core.LevelUpManager _levelUp;

            private float _reviveOpenSince = -1f;
            private float _arcanaOpenSince = -1f;

            private void Update()
            {
                // The same for the Revive screen (P1-16): a test that is not about revivals would otherwise wait behind it
                // forever if the save's Revivals bring a stray death back (they used to revive at once).
                var revive = SoulHunter.Gameplay.UI.ReviveScreen.Current;
                if (revive == null || !revive.IsOpen) _reviveOpenSince = -1f;
                else if (_reviveOpenSince < 0f) _reviveOpenSince = Time.realtimeSinceStartup;
                else if (Time.realtimeSinceStartup - _reviveOpenSince >= Seconds)
                {
                    _reviveOpenSince = -1f;
                    Debug.Log("[LevelUpUnstick] A Revive screen stayed open " + Seconds + " s in a test: pressed Revive.");
                    revive.PressRevive();
                }

                // The same for the Arcana choice (P2-01): an Arcana-mode test that is not about the choice takes a card.
                var arcana = SoulHunter.Gameplay.UI.ArcanaChoiceScreen.Current;
                if (arcana == null || !arcana.IsOpen) _arcanaOpenSince = -1f;
                else if (_arcanaOpenSince < 0f) _arcanaOpenSince = Time.realtimeSinceStartup;
                else if (Time.realtimeSinceStartup - _arcanaOpenSince >= Seconds)
                {
                    _arcanaOpenSince = -1f;
                    Debug.Log("[LevelUpUnstick] An Arcana choice stayed open " + Seconds + " s in a test: took the chosen card.");
                    arcana.PressTake();
                }

                if (_levelUp == null) _levelUp = GetComponent<SoulHunter.Gameplay.Core.LevelUpManager>();
                if (_levelUp == null || !_levelUp.IsLevelUpActive) { _openSince = -1f; return; }
                if (_openSince < 0f) { _openSince = Time.realtimeSinceStartup; return; }
                if (Time.realtimeSinceStartup - _openSince < Seconds) return;
                var field = typeof(SoulHunter.Gameplay.Core.LevelUpManager).GetField("_offered", BindingFlags.NonPublic | BindingFlags.Instance);
                var offered = field != null ? field.GetValue(_levelUp) as System.Collections.Generic.List<SoulHunter.Gameplay.Combat.UpgradeData> : null;
                _openSince = -1f;
                if (offered != null && offered.Count > 0)
                {
                    Debug.Log("[LevelUpUnstick] A level-up screen stayed open " + Seconds + " s in a test: took its first card.");
                    _levelUp.SelectUpgrade(offered[0]);
                }
            }
        }

        /// <summary>
        /// Takes the first card of any level-up screen left open. Level 1 opens with the VS Mad Forest's 1-5 HP bats
        /// (2026-10-03), so the starting weapon can level the hero during boot; a test that then waits on game time would
        /// wait forever behind the paused screen (it hung the Bible, Duplicator and Dash tests).
        /// </summary>
        protected IEnumerator CloseOpenLevelUps()
        {
            var levelUp = Object.FindFirstObjectByType<SoulHunter.Gameplay.Core.LevelUpManager>();
            for (int i = 0; i < 50 && levelUp != null && levelUp.IsLevelUpActive; i++)
            {
                var offered = GetPrivateField<System.Collections.Generic.List<SoulHunter.Gameplay.Combat.UpgradeData>>(levelUp, "_offered");
                if (offered == null || offered.Count == 0) break;
                levelUp.SelectUpgrade(offered[0]);
                yield return null;
            }
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

        /// <summary>As <see cref="WaitUntilRealtime(System.Func{bool}, string)"/>, and on a timeout also reports <paramref name="state"/>.</summary>
        protected IEnumerator WaitUntilRealtime(System.Func<bool> condition, string what, System.Func<string> state)
        {
            float deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Timed out after {TimeoutSeconds}s waiting for {what} ({state()}). Active scene: {SceneManager.GetActiveScene().name}. " + Errors());
                yield return null;
            }
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

        /// <summary>
        /// A death with a Revival left holds the player for the Revive screen (VS Revival, P1-16). Tests that kill the
        /// player until the run ends press Revive like a player and end the revival's invulnerability so the next hit lands.
        /// </summary>
        protected static void AnswerReviveScreen()
        {
            var screen = SoulHunter.Gameplay.UI.ReviveScreen.Current;
            if (screen == null || !screen.IsOpen) return;
            Click(screen.ReviveButton, "Revive");
            var player = PlayerController.Instance;
            if (player != null) player.GetComponent<SoulHunter.Gameplay.Combat.HealthController>().EndRevivalInvulnerability();
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
