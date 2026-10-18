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
    }
}
