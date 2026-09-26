using System.Collections;
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
