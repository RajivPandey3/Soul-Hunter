using System.Linq;
using NUnit.Framework;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// The owner-approved story layer (2026-10-13, StoryTwist): every sealed hunter has a line, the casket card is built
    /// from them, the keepsakes are in their order, and the owner's own story is untouched.
    /// </summary>
    public class StoryTwistTests
    {
        [Test]
        public void EveryHeroButKael_HasAHunterLine()
        {
            var characters = GameContentCatalog.Load().Characters.Where(c => c != null).ToList();
            Assert.That(characters.Count, Is.EqualTo(13));
            foreach (var character in characters)
            {
                string name = GameContentCatalog.CurrentName(character.CharacterName);
                if (name == "Kael") Assert.That(StoryTwist.HunterLine(name), Is.Null, "Kael sealed the caskets; he is in none.");
                else Assert.That(StoryTwist.HunterLine(name), Is.Not.Null.And.Not.Empty, name);
            }
        }

        [Test]
        public void CasketCard_PutsTheSealFirst_TheHunterNext_AndTheCountLast()
        {
            string middle = StoryTwist.CasketText("Aria", firstEver: false, lastOne: false);
            Assert.That(middle, Is.EqualTo(StoryTwist.HunterLine("Aria")));
            string first = StoryTwist.CasketText("Aria", firstEver: true, lastOne: false);
            Assert.That(first, Does.StartWith(StoryTwist.FirstCasket).And.EndsWith(StoryTwist.HunterLine("Aria")));
            string last = StoryTwist.CasketText("Sylas", firstEver: false, lastOne: true);
            Assert.That(last, Does.StartWith(StoryTwist.HunterLine("Sylas")).And.EndsWith(StoryTwist.LastCasket));
            Assert.That(StoryTwist.CasketText("Nobody", false, false), Is.Empty, "An unknown name shows no card.");
        }

        [Test]
        public void Keepsakes_ComeInOrder_AndTheOwnersStoryIsUnchanged()
        {
            Assert.That(StoryTwist.Keepsakes.Count, Is.EqualTo(11), "One line per relic.");
            Assert.That(StoryTwist.Keepsakes.Count, Is.EqualTo(SoulHunter.Core.Persistence.RelicBook.All.Length));
            Assert.That(StoryTwist.KeepsakeLine(0), Does.StartWith("A wooden bird"));
            Assert.That(StoryTwist.KeepsakeLine(7), Does.StartWith("A lock of hair"));
            Assert.That(StoryTwist.KeepsakeLine(10), Does.StartWith("Her name"));
            Assert.That(StoryTwist.KeepsakeLine(11), Is.Null);
            Assert.That(StoryTwist.KeepsakeLine(-1), Is.Null);
            Assert.That(StoryTwist.TrueEnding, Does.Contain("at the grave"));
            Assert.That(CampaignStory.FinalLine, Is.EqualTo("To free his daughter's soul, he must destroy himself."), "The owner's last line stays.");
        }
    }
}
