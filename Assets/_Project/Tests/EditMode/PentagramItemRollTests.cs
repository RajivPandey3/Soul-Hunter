using NUnit.Framework;
using SoulHunter.Gameplay.Pickups;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// vampire.survivors.wiki/w/Pentagram, "Chance to not destroy items": x ~ U[0,1] rounded to 2 decimals; items are
    /// destroyed if x / totalLuck > threshold. The wiki's own examples.
    /// </summary>
    public class PentagramItemRollTests
    {
        [Test]
        public void TheWikiExamples_Hold()
        {
            // "assume a level 5 Pentagram, with x = 0.5. This leads to a threshold of 0.45."
            Assert.That(GroundItemEraser.ErasesItems(0.45f, 1f, 0.5f), Is.True, "+0% Luck: 0.5 > 0.45, destruction triggered.");
            Assert.That(GroundItemEraser.ErasesItems(0.45f, 1.6f, 0.5f), Is.False, "+60% Luck: 0.31 < 0.45, not triggered.");
            // "At level 1, the threshold is 10%, +900% Luck is required (=1000% total Luck)" to never destroy, even at x = 1.
            Assert.That(GroundItemEraser.ErasesItems(0.1f, 10.01f, 1f), Is.False);
            Assert.That(GroundItemEraser.ErasesItems(0.1f, 1f, 0.11f), Is.True);
            Assert.That(GroundItemEraser.ErasesItems(0.1f, 1f, 0.1f), Is.False, "x rounded to 0.10 is not above 0.10.");
        }
    }
}
