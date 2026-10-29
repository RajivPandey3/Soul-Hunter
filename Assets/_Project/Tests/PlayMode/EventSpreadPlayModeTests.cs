using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// A map event's members are made a few a frame (2026-10-29): Level 3's trap Swarm of 200 Smoke Clumps and Level 7's
    /// Thorn ring of 100 Tide Thorns, made in one frame, took that frame to 29-56 ms in a release build
    /// (`run-20261029-wrapped`). All of them still come, at the places the event gave them, a few frames later.
    /// </summary>
    public class EventSpreadPlayModeTests : GameplayPlayModeTestBase
    {
        private IEnumerator EnterLevel(int level, System.Action<EnemySpawner> found)
        {
            yield return BootIntoGameplay();
            EnemySpawner.EventSpawnsPerFrame = 16; // the game's own budget (the test base lifts it)
            EnemySpawner.EventSpawnMsPerFrame = 2f;
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            var player = PlayerController.Instance;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            player.GetComponent<HealthController>().IsInvincible = true;
            LevelProgressionManager.Instance.StartStage(level);
            yield return null;
            yield return null;
            foreach (var stray in EnemyController.ActiveEnemies.ToList()) if (stray != null) stray.gameObject.SetActive(false);
            found(spawner);
        }

        [UnityTest]
        public IEnumerator LevelThree_ATwoHundredSwarm_ComesSixteenAFrame_AndAllOfItArrives()
        {
            EnemySpawner spawner = null;
            yield return EnterLevel(3, s => spawner = s);
            var row = StageTraps.For(3).First(r => r.EnemyTypeId == "SmokeClump" && r.Count == 200);
            var swarm = spawner.TriggerMapEvent(row);
            int first = swarm.Count;
            Assert.That(first, Is.InRange(1, EnemySpawner.EventSpawnsPerFrame), "The first frame makes only its share. " + Errors());
            Assert.That(spawner.PendingEventSpawns, Is.EqualTo(200 - first));

            // Bug hunt 2026-10-29: with the queue busy a second row makes nothing this frame, but it did start.
            var second = spawner.TriggerMapEvent(row);
            Assert.That(second, Is.Empty, "the queue is busy: nothing made this frame");
            Assert.That(spawner.LastEventStarted, Is.True, "yet it started (its members are queued), so a plate takes its cooldown");

            // A pause holds the rest.
            GameTime.Pause(this);
            for (int i = 0; i < 5; i++) yield return null;
            Assert.That(swarm.Count, Is.EqualTo(first), "Nothing is made under a pause.");
            GameTime.Resume(this);

            yield return WaitUntilRealtime(() => spawner.PendingEventSpawns == 0, "the rest of the swarm");
            Assert.That(swarm.Count, Is.EqualTo(200), "Every Smoke Clump comes.");
            Assert.That(swarm.All(e => e != null && e.isActiveAndEnabled), Is.True);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator LevelSeven_AHundredThornRing_IsBuiltOverFrames_AndAStageChangeDropsTheRest()
        {
            EnemySpawner spawner = null;
            yield return EnterLevel(7, s => spawner = s);
            var row = StageTraps.For(7).First(r => r.Type == StageEventType.TrapRing && r.EnemyTypeId == "TideThorn");
            var ring = spawner.TriggerMapEvent(row);
            Assert.That(ring.Count, Is.InRange(1, EnemySpawner.EventSpawnsPerFrame), Errors());
            var circle = Object.FindObjectsByType<SoulHunter.Gameplay.Hazards.ThornCircle>(FindObjectsSortMode.None).Single();
            yield return WaitUntilRealtime(() => spawner.PendingEventSpawns == 0, "the rest of the ring");
            Assert.That(circle.Members.Count(), Is.EqualTo(row.Count), "The whole ring stands.");
            Assert.That(circle.Posts, Is.EqualTo(row.Count), "Each member has its post.");

            // Fired again and the stage changes at once: the members still waiting are not made.
            yield return null;
            spawner.TriggerMapEvent(row);
            Assert.That(spawner.PendingEventSpawns, Is.GreaterThan(0));
            LevelProgressionManager.Instance.StartStage(8);
            Assert.That(spawner.PendingEventSpawns, Is.EqualTo(0), "The next stage drops the queue.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
