using NUnit.Framework;
using SoulHunter.Gameplay.Player;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// VS DOT Phase 1 row 3 (2026-10-01). vampire.survivors.wiki/w/Player_stats, "Max value": Might 1000% (not for Soul
    /// Eater), Area 1000%, Speed 500%, Duration 500%, Amount 10, Armor 50.
    /// </summary>
    public class StatCapTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown() { if (_go != null) Object.DestroyImmediate(_go); }

        [Test]
        public void Stats_StopAtTheVsMaximum_ButKeepWhatWasGathered()
        {
            _go = new GameObject("Stats");
            var stats = _go.AddComponent<PlayerStats>();
            stats.AddMight(20f);
            stats.AddArea(20f);
            stats.AddProjectileSpeed(20f);
            stats.AddDuration(20f);
            stats.AddAmount(15);
            stats.AddArmor(60);

            Assert.That(stats.Might, Is.EqualTo(10f), "Might 1000%");
            Assert.That(stats.Area, Is.EqualTo(10f), "Area 1000%");
            Assert.That(stats.ProjectileSpeed, Is.EqualTo(5f), "Speed 500%");
            Assert.That(stats.Duration, Is.EqualTo(5f), "Duration 500%");
            Assert.That(stats.Amount, Is.EqualTo(10), "Amount 10");
            Assert.That(stats.Armor, Is.EqualTo(50), "Armor 50");
            Assert.That(stats.RawMight, Is.EqualTo(21f), "What was gathered is kept (Soul Eater reads it).");

            // Under the caps nothing changes.
            var plain = new GameObject("Plain").AddComponent<PlayerStats>();
            try
            {
                plain.AddMight(0.5f);
                plain.AddArmor(3);
                Assert.That(plain.Might, Is.EqualTo(1.5f));
                Assert.That(plain.Armor, Is.EqualTo(3));
            }
            finally { Object.DestroyImmediate(plain.gameObject); }
        }
    }
}
