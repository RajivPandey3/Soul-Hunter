using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Core.Persistence;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Audit 2026-10-30 (Docs/AUDIT_20261030.md), in the real game. The run-start Soul Egg copy is never null. A stage
    /// change ends an Orologion freeze and takes an unopened Armoury Cache. The map's Guides switch is kept on the save.
    /// A stage start places its items once. The single-loop runners keep going past one item's error.
    /// </summary>
    public class AuditFixesPlayModeTests : GameplayPlayModeTestBase
    {
        private IEnumerator Boot()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null) stray.gameObject.SetActive(false);
            PlayerController.Instance.GetComponent<HealthController>().IsInvincible = true;
        }

        [UnityTest]
        public IEnumerator ARun_StartsWithANonNullEggCopy_SoResultsNeverReadTheLiveRecord()
        {
            yield return Boot();
            var copy = SoulEggs.AppliedThisRun;
            Assert.That(copy, Is.Not.Null, "A run with no eggs has an empty copy (A-03), not null.");
            Assert.That(copy.Eggs, Is.EqualTo(0), "The test save's eggs are cleared by the test base.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator AStageChange_EndsTheOrologionFreeze_AndTakesAnUnopenedArmouryCache()
        {
            yield return Boot();
            TimeFreezePickup.BeginWorldFreeze(10f);
            Assert.That(TimeFreezePickup.WorldFreezeRemaining, Is.GreaterThan(9f));
            var cache = WeaponCachePickup.Create(PlayerController.Instance.transform.position + new Vector3(6f, 0f, 0f));
            Assert.That(WeaponCachePickup.LiveCount, Is.EqualTo(1));

            LevelProgressionManager.Instance.StartStage(2);
            yield return null;
            yield return null;

            Assert.That(TimeFreezePickup.WorldFreezeRemaining, Is.EqualTo(0f), "L-09: the new stage's horde is not frozen.");
            Assert.That(cache == null, Is.True, "L-11: an unopened cache goes with its stage.");
            Assert.That(WeaponCachePickup.LiveCount, Is.EqualTo(0));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator TheGuidesSwitch_IsKeptOnTheSave()
        {
            yield return Boot();
            GameServices.Instance.TryGet(out SaveService save);
            Assert.That(save, Is.Not.Null);
            MapGuides.Enabled = true;
            Assert.That(save.CurrentData.ShowMapGuides, Is.True, "L-06: switching the guides on writes the save's field.");
            Assert.That(MapGuides.Enabled, Is.True, "The switch reads the save.");
            MapGuides.Enabled = false;
            Assert.That(save.CurrentData.ShowMapGuides, Is.False);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator AStageStart_PlacesItsItemsOnce()
        {
            yield return Boot();
            var items = Object.FindFirstObjectByType<StageItemSpawner>();
            yield return WaitUntilRealtime(() => items.Placed.Count > 0, "the stage's items");
            for (int i = 0; i < 5; i++) yield return null;
            Assert.That(items.QueuedPlacements, Is.EqualTo(1), "L-03: Start and the first stage start are one placement.");
            LevelProgressionManager.Instance.StartStage(2);
            for (int i = 0; i < 5; i++) yield return null;
            Assert.That(items.QueuedPlacements, Is.EqualTo(2), "The next stage places once more.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private sealed class FakePickup : ITickedPickup
        {
            public int TickerIndex { get; set; } = -1;
            public bool Throws;
            public int Ticks;
            public void Tick()
            {
                Ticks++;
                if (Throws) throw new System.InvalidOperationException("audit test: a broken pickup");
            }
        }

        [UnityTest]
        public IEnumerator OnePickupsError_DoesNotStopTheOtherPickups()
        {
            yield return Boot();
            var broken = new FakePickup { Throws = true };
            var good = new FakePickup();
            PickupTicker.Add(good);   // added first: the runner ticks backwards, so the broken one goes first
            PickupTicker.Add(broken);
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("audit test: a broken pickup"));
            yield return null; // one ticker frame: one logged exception
            PickupTicker.Remove(broken);
            PickupTicker.Remove(good);
            Assert.That(broken.Ticks, Is.GreaterThan(0));
            Assert.That(good.Ticks, Is.GreaterThan(0), "A-02: the pickup after a broken one is still ticked.");
        }
    }
}