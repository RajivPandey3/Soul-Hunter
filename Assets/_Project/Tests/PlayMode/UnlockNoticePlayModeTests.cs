using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Bug MIS B05 (2026-09-30): an unlock during a level-up drew "New character unlocked" on top of "LEVEL UP!". The
    /// notice now waits, hidden, while a menu has the game paused.
    /// </summary>
    public class UnlockNoticePlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator AnUnlockDuringAPause_WaitsUntilPlayResumes()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var tracker = Object.FindFirstObjectByType<CharacterUnlockTracker>();
            Assert.IsNotNull(tracker, "The run has an unlock tracker.");
            var character = GameContentCatalog.Load().Characters.First(c => c != null && c.CharacterName != "Kael");
            var show = typeof(CharacterUnlockTracker).GetMethod("ShowNotice", BindingFlags.Instance | BindingFlags.NonPublic);

            GameTime.Pause(this);
            try
            {
                show.Invoke(tracker, new object[] { $"New character unlocked: {character.CharacterName}!" });
                yield return null;
                Assert.That(tracker.Notice, Is.Not.Null, "The notice exists.");
                Assert.That(tracker.Notice.activeSelf, Is.False, "Hidden while a menu has the game paused.");
                yield return new WaitForSecondsRealtime(4f); // longer than the notice's 3.5 s
                Assert.That(tracker.Notice, Is.Not.Null, "Its time does not run while paused.");
            }
            finally { GameTime.Resume(this); }
            yield return null;
            yield return null;
            Assert.That(tracker.Notice != null && tracker.Notice.activeSelf, Is.True, "Shown once play resumes.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
