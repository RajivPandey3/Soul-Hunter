using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>The owner's story is told in play: each stage opens with its story card, without pausing the game.</summary>
    public class StoryCardPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator EachStage_OpensWithItsStory_WithoutPausing()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            // No kills, so no level-up screen pauses the game while the card counts down (flaky before 2026-09-28).
            foreach (var weapon in SoulHunter.Gameplay.Player.PlayerController.Instance.GetComponentsInChildren<SoulHunter.Gameplay.Combat.AutoAttackWeapon>())
                weapon.gameObject.SetActive(false);
            foreach (var stray in EnemyController.ActiveEnemies.ToArray())
                if (stray != null) stray.gameObject.SetActive(false);
            var card = Object.FindFirstObjectByType<StoryCardUI>();
            Assert.IsNotNull(card, "The HUD has no story card. " + Errors());
            Assert.That(card.IsShowing, Is.True, "Level 1 opens with the story.");
            Assert.That(card.Title, Is.EqualTo("Level 1: The Cursed Graveyard"));
            StringAssert.StartsWith(CampaignStory.Premise, card.Body);
            Assert.That(GameTime.IsPaused, Is.False, "The story card does not pause the game.");

            LevelProgressionManager.Instance.StartStage(4);
            yield return null;
            Assert.That(card.IsShowing, Is.True);
            Assert.That(card.Title, Is.EqualTo("Level 4: The Ruined Castle"));
            Assert.That(card.Body, Is.EqualTo("The gateway to the Underworld. The Gatekeeper warns Kael that he is being deceived."));

            // A menu (level-up, chest, pause) hides the card; it comes back when the menu closes.
            yield return new WaitForSecondsRealtime(1f);
            Assert.That(card.Alpha, Is.GreaterThan(0.5f));
            var menu = new object();
            GameTime.Pause(menu);
            yield return null;
            Assert.That(card.IsShowing && card.Alpha == 0f, Is.True, "Hidden, not closed, while a menu is open.");
            GameTime.Resume(menu);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(card.Alpha, Is.GreaterThan(0.5f), "Back once the menu closes.");

            float limit = Time.realtimeSinceStartup + StoryCardUI.DurationFor(card.Body) + 3f;
            while (card.IsShowing && Time.realtimeSinceStartup < limit) yield return null;
            Assert.That(card.IsShowing, Is.False, "The card goes away on its own.");

            LevelProgressionManager.Instance.StartStage(10);
            yield return null;
            Assert.That(card.Body, Is.EqualTo(CampaignStory.Twist), "Level 10 reveals the twist.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
