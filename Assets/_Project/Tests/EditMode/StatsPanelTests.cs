using System.Linq;
using NUnit.Framework;
using SoulHunter.Gameplay.Player;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>The in-game stats list follows vampire.survivors.wiki/w/Player_stats (order) and the character select formats.</summary>
    public class StatsPanelTests
    {
        [Test]
        public void LiveRows_FollowTheWikiOrder_AndShowTheLiveValues()
        {
            var go = new GameObject("stats");
            try
            {
                var stats = go.AddComponent<PlayerStats>();
                stats.AddMight(0.1f);
                stats.AddAmount(2);
                var rows = CharacterStatSheet.Live(stats, 120, 2, 0, 1).InGameRows();
                Assert.That(rows.Select(r => r.Label), Is.EqualTo(new[]
                {
                    "Max Health", "Recovery", "Armour", "Move Speed", "Might", "Area", "Speed", "Duration", "Amount", "Cooldown",
                    "Luck", "Growth", "Greed", "Curse", "Magnet", "Revival", "Reroll", "Skip", "Banish",
                }));
                string Value(string label) => rows.First(r => r.Label == label).Value;
                Assert.That(Value("Max Health"), Is.EqualTo("120"));
                Assert.That(Value("Might"), Is.EqualTo("+10%"));
                Assert.That(Value("Amount"), Is.EqualTo("+2"));
                Assert.That(Value("Area"), Is.EqualTo("-"), "Unchanged stats show a dash.");
                Assert.That(Value("Reroll"), Is.EqualTo("+2"));
                Assert.That(Value("Skip"), Is.EqualTo("-"));
                Assert.That(Value("Banish"), Is.EqualTo("+1"));
                Assert.That(rows.First(r => r.Label == "Max Health").Changed, Is.True);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
