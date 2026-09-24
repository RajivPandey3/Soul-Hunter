using NUnit.Framework;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.UI;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: RunHudTests verifies the VS-style HUD data: gold shown is the gold collected this run
    /// (event fires with the running total and resets to 0), and the level label format.
    /// </summary>
    public class RunHudTests
    {
        private GameObject _object;

        [TearDown]
        public void TearDown()
        {
            if (_object != null) Object.DestroyImmediate(_object);
        }

        [Test]
        public void RunGold_EventReportsRunningTotal_AndResetsToZero()
        {
            // Learning Comment: HUD ko is run ka gold dikhana hai, saved total nahi.
            _object = new GameObject("RunStats");
            var stats = _object.AddComponent<RunStatsTracker>();
            int shown = -1;
            stats.OnGoldChanged += gold => shown = gold;

            stats.AddGold(10);
            stats.AddGold(15);
            Assert.That(shown, Is.EqualTo(25));
            Assert.That(stats.TotalGoldCollected, Is.EqualTo(25));

            stats.ResetStats();
            Assert.That(shown, Is.EqualTo(0), "Naye run par HUD gold 0 dikhaye.");
        }

        [Test]
        public void LevelLabel_ShowsLevelNumber()
        {
            Assert.That(XPBarUI.FormatLevel(1), Is.EqualTo("LV 1"));
            Assert.That(XPBarUI.FormatLevel(42), Is.EqualTo("LV 42"));
        }
    }
}
