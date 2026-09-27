using System.IO;
using NUnit.Framework;
using SoulHunter.Gameplay.Data;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// The owner's story (owner, 2026-09-27: "MERE STORY KO FOLLOW KARO", "STORY MEIN NO CHANGES"): every story text
    /// the game shows is word for word from Docs/Game_Design_Document.md.
    /// </summary>
    public class CampaignStoryTests
    {
        private static string Gdd() =>
            File.ReadAllText(Path.Combine(Application.dataPath, "..", "Docs", "Game_Design_Document.md"));

        [Test]
        public void EveryStageStoryLine_IsTheOwnersWords()
        {
            string gdd = Gdd();
            for (int stage = 1; stage <= 10; stage++)
            {
                var level = CampaignLevelCatalog.Get(stage);
                StringAssert.Contains($"* **Story:** {level.Narrative}", gdd, $"Stage {stage}'s story line is not the GDD's.");
            }
        }

        [Test]
        public void PremiseAndTwist_AreTheOwnersWords()
        {
            string gdd = Gdd();
            StringAssert.Contains($"* **The Goal:** {CampaignStory.Premise}", gdd);
            StringAssert.Contains($"* **The Twist (Level 10):** {CampaignStory.Twist}", gdd);
            StringAssert.EndsWith(CampaignStory.FinalLine, CampaignStory.Twist, "The victory line is the twist's own last sentence.");
        }

        [Test]
        public void Cards_OpenWithThePremise_AndLevel10WithTheTwist()
        {
            StringAssert.StartsWith(CampaignStory.Premise, CampaignStory.CardText(1));
            StringAssert.EndsWith(CampaignLevelCatalog.Get(1).Narrative, CampaignStory.CardText(1));
            Assert.That(CampaignStory.CardText(4), Is.EqualTo(CampaignLevelCatalog.Get(4).Narrative));
            Assert.That(CampaignStory.CardText(10), Is.EqualTo(CampaignStory.Twist));
        }
    }
}
