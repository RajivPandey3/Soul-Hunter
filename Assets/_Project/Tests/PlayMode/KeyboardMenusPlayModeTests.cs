using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using SoulHunter.Core.Events;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Owner report 2026-10-18: playing on the keyboard, the level-up choice (and the chest screen) did not answer the keys.
    /// VS wiki (Options, patch 0.2.4): "Spacebar can be used just like Enter to interact with the UI". The UI module only
    /// submitted on Enter, and Space, which is also Dash, started a dash that ran when the menu closed.
    /// Key presses themselves cannot be checked here: in a windowless editor the Input System gives the UI no keyboard input
    /// (no Game view has focus). They are checked in a release build (AutoplayBot -sh-autoplay-manual-menus -sh-autoplay-menu-keys).
    /// </summary>
    public class KeyboardMenusPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator TheUiSubmitsOnSpaceAndEnter_AndMovesOnArrowsAndWasd()
        {
            yield return BootIntoGameplay();
            yield return null;
            var module = EventSystem.current != null ? EventSystem.current.currentInputModule as InputSystemUIInputModule : null;
            Assert.IsNotNull(module, "The gameplay scene's UI runs on the Input System's UI module.");
            var submit = module.submit.action;
            Assert.That(UiSubmitKeys.HasSpace(submit), Is.True, "VS: Space works like Enter in the UI.");
            Assert.That(submit.bindings.Any(b => b.path == "*/{Submit}" || b.path.Contains("enter")), Is.True, "Enter still submits.");
            Assert.That(submit.enabled, Is.True, "Submit stays on after Space is added.");
            var paths = module.move.action.bindings.Select(b => b.path).ToList();
            foreach (string key in new[] { "<Keyboard>/upArrow", "<Keyboard>/downArrow", "<Keyboard>/w", "<Keyboard>/s" })
                Assert.That(paths, Does.Contain(key), key + " moves between buttons.");

            // Loading the scene again adds no second Space binding.
            UiSubmitKeys.ApplyToAll();
            Assert.That(submit.bindings.Count(b => b.path == UiSubmitKeys.SpacePath), Is.EqualTo(1), "Space is added once.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator Dash_DoesNotStartWhileAMenuPausesTheGame()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            yield return null;
            var player = PlayerController.Instance;
            var bus = GameServices.Instance.Get<EventBus>();
            var owner = new object();
            GameTime.Pause(owner);
            try
            {
                bus.Raise(new PlayerDashEvent());
                yield return null;
                Assert.That(player.IsDashing, Is.False, "Space on a paused menu picks; it does not start a dash.");
            }
            finally { GameTime.Resume(owner); }
            yield return null;
            Assert.That(player.IsDashing, Is.False, "Nothing was queued for when the game resumes.");
            bus.Raise(new PlayerDashEvent());
            yield return null;
            Assert.That(player.IsDashing, Is.True, "In play, Space still dashes.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        // Bug MIS UI-01: Esc / Start on the level-up screen opened the pause menu drawn under it (HUD canvas order 0 against
        // 300) and moved keyboard focus to its hidden Resume. The Pause key is off while a level-up holds the pause, as on
        // the chest, Arcana and Revive screens, and works again once the level-ups are done.
        [UnityTest]
        public IEnumerator ThePauseKey_IsOffOnTheLevelUpScreen_AndBackAfterThePick()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            yield return null;
            var pause = PauseAction();
            Assert.IsNotNull(pause, "The Pause action exists. " + Errors());
            Assert.That(pause.enabled, Is.True, "In play the Pause key works.");

            var levelUp = Object.FindFirstObjectByType<LevelUpManager>();
            var experience = PlayerController.Instance.GetComponent<PlayerExperience>();
            System.Collections.Generic.List<UpgradeData> choices = null;
            System.Action<System.Collections.Generic.List<UpgradeData>> onRound = round => choices = round;
            levelUp.OnLevelUpUIRound += onRound;
            try
            {
                experience.AddXP(experience.XPToNextLevel - experience.CurrentXP);
                yield return WaitUntilRealtime(() => levelUp.IsLevelUpActive && choices != null, "the level-up screen");
                Assert.That(pause.enabled, Is.False, "Esc / Start must not open the pause menu under the level-up screen.");
                Assert.That(GameSystemManager.Instance == null || !GameSystemManager.Instance.IsPaused, Is.True, "The pause menu is not open.");

                yield return CloseOpenLevelUps(); // picks the first card of each chained level-up
                yield return null;
            }
            finally { levelUp.OnLevelUpUIRound -= onRound; }
            Assert.That(levelUp.IsLevelUpActive, Is.False, "The level-ups are done.");
            Assert.That(pause.enabled, Is.True, "The Pause key works again after the pick.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        // Bug MIS UI-01: the same with the Wandering Merchant's shop (canvas order 900): Quit from the hidden pause menu
        // showed the Results screen under the shop. Off while the shop is open, back when the player leaves, and back when
        // the merchant is destroyed with the shop open (stage change, scene unload).
        [UnityTest]
        public IEnumerator ThePauseKey_IsOffInTheMerchantShop_AndBackWhenItCloses()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            LevelProgressionManager.Instance.StartStage(3);
            yield return WaitUntilRealtime(() => Object.FindFirstObjectByType<WanderingMerchant>() != null, "Level 3 to spawn the merchant");
            var pause = PauseAction();
            Assert.That(pause.enabled, Is.True, "In play the Pause key works.");

            var merchant = Object.FindFirstObjectByType<WanderingMerchant>();
            merchant.OpenShop();
            yield return null;
            Assert.That(merchant.IsShopOpen, Is.True, "The shop opened. " + Errors());
            Assert.That(pause.enabled, Is.False, "Esc / Start must not open the pause menu under the shop.");
            Object.Destroy(merchant.gameObject);
            yield return null;
            Assert.That(pause.enabled, Is.True, "A merchant destroyed with its shop open gives the Pause key back.");
            Assert.That(GameTime.IsPaused, Is.False, "...and the game runs.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static UnityEngine.InputSystem.InputAction PauseAction()
        {
            GameServices.Instance.TryGet(out SoulHunter.Core.Input.InputService input);
            return input?.Actions?.FindAction(SoulHunter.Core.Input.InputService.PauseAction);
        }
    }
}
