using NUnit.Framework;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>Bug MIS B10 (2026-09-30): the Gold Pouch and Roast Chicken level-up cards had no icon.</summary>
    public class RewardIconTests
    {
        [Test]
        public void MaxedRewardCards_HaveIcons()
        {
            var content = GameContentCatalog.Load();
            Assert.That(content.GoldPouchIcon, Is.Not.Null, "The catalog has the Gold Pouch icon.");
            Assert.That(content.RoastChickenIcon, Is.Not.Null, "The catalog has the Roast Chicken icon.");
            var cards = MaxedRewards.BuildCards();
            try
            {
                Assert.That(cards.Count, Is.EqualTo(2));
                foreach (var card in cards) Assert.That(card.Icon, Is.Not.Null, $"{card.UpgradeName} has an icon.");
            }
            finally { foreach (var card in cards) Object.DestroyImmediate(card); }
        }
    }
}
