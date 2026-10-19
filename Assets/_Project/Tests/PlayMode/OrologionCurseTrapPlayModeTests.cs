using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS DOT Phase 1 rows 10-12 (2026-10-01).
    /// - wiki Orologion: "Enemies will continue to spawn, and new enemies will also become frozen"; it "slows down Gold
    ///   Fever's duration decay by half".
    /// - wiki Curse: the health effect applies to new enemies at once; "All other effects are applied at the start of the
    ///   next minute."
    /// - wiki Enemies (Traps): one global trap cooldown, "effectiveCooldown = trapCooldown x totalLuck".
    /// </summary>
    public class OrologionCurseTrapPlayModeTests : GameplayPlayModeTestBase
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        private EnemySpawner Spawner => Object.FindFirstObjectByType<EnemySpawner>();

        private IEnumerator Boot()
        {
            yield return BootIntoGameplay();
            Spawner.enabled = false;
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>()) weapon.enabled = false;
            yield return null;
        }

        [UnityTest]
        public IEnumerator DuringAnOrologion_NewEnemiesFreeze_AndGoldFeverDecaysAtHalfSpeed()
        {
            yield return Boot();
            var player = PlayerController.Instance;
            var fever = GoldFever.Begin(player.gameObject);
            TimeFreezePickup.BeginWorldFreeze(3f);
            var prefab = (GameObject)typeof(EnemySpawner).GetField("_stageEnemyPrefab", Private).GetValue(Spawner);
            var enemy = ((GameObject)typeof(EnemySpawner).GetMethod("SpawnEnemy", Private).Invoke(Spawner, new object[] { prefab })).GetComponent<EnemyController>();
            Assert.That(enemy.IsFrozen, Is.True, "A new enemy spawns frozen during the Orologion.");

            float before = fever.RemainingSeconds;
            yield return new WaitForSeconds(1f);
            float spent = before - fever.RemainingSeconds;
            Assert.That(spent, Is.InRange(0.4f, 0.65f), $"Gold Fever lost {spent:0.00} s in 1 s of freeze (half speed).");

            yield return new WaitForSeconds(2.2f);
            Assert.That(TimeFreezePickup.WorldFrozen, Is.False);
            var later = ((GameObject)typeof(EnemySpawner).GetMethod("SpawnEnemy", Private).Invoke(Spawner, new object[] { prefab })).GetComponent<EnemyController>();
            Assert.That(later.IsFrozen, Is.False, "After the freeze new enemies move again.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        /// <summary>
        /// Bug MIS AI-06. A map-event flier or a hazard member spawned during an Orologion was frozen by SpawnEnemy, then its
        /// own state (the flight, the hold) replaced the freeze: formations flew and pile shooters fired through a Time
        /// Freeze. They now stay frozen for what is left of it and then do what they came for.
        /// </summary>
        [UnityTest]
        public IEnumerator DuringAnOrologion_NewFliersAndHazardMembersFreeze_ThenResumeTheirOwnBehaviour()
        {
            yield return Boot();
            TimeFreezePickup.BeginWorldFreeze(1.5f);
            var fliers = Spawner.TriggerStageEvent(SoulHunter.Gameplay.Data.StageEventType.Swarm, 4);
            Assert.That(fliers.Count, Is.GreaterThan(0), "The swarm spawned. " + Errors());
            Assert.That(fliers.All(f => f.IsFrozen && f.IsFlying), Is.True, "New fliers are frozen and keep their flight to resume.");
            var shooters = SoulHunter.Gameplay.Hazards.StageHazards.Instance.StartPileAssault(3, 20f);
            Assert.That(shooters.Count, Is.EqualTo(3), "The pile ring spawned. " + Errors());
            var still = SoulHunter.Gameplay.Hazards.StillEnemy.Begin(
                Spawner.SpawnHazardEnemy(Spawner.StageEnemyPrefab, PlayerController.Instance.transform.position + Vector3.right * 6f));
            Assert.IsNotNull(still);
            Assert.That(shooters.All(p => p.GetComponent<EnemyController>().IsFrozen), Is.True, "Pile shooters spawned during the freeze are frozen.");
            Assert.That(still.GetComponent<EnemyController>().IsFrozen, Is.True, "A still enemy spawned during the freeze is frozen.");
            var flier = fliers[0];
            Vector3 at = flier.transform.position;
            yield return new WaitForSeconds(1f);
            Assert.That(shooters.Sum(p => p.ShotsFired), Is.Zero, "A frozen pile shooter does not fire.");
            if (flier != null && flier.gameObject.activeInHierarchy)
                Assert.That(Vector3.Distance(flier.transform.position, at), Is.LessThan(0.3f), "A frozen flier stays put.");

            yield return new WaitForSeconds(1.2f);
            Assert.That(TimeFreezePickup.WorldFrozen, Is.False);
            var live = fliers.Where(f => f != null && f.gameObject.activeInHierarchy).ToList();
            Assert.That(live.Count, Is.GreaterThan(0));
            Assert.That(live.All(f => f.IsFlying && !f.IsFrozen), Is.True, "Afterwards the fliers resume their path.");
            Assert.That(shooters.All(p => p.GetComponent<EnemyController>().CurrentState is SoulHunter.Gameplay.Hazards.EnemyHoldState),
                Is.True, "Afterwards the shooters hold their spot again.");
            Assert.That(still.GetComponent<EnemyController>().CurrentState is SoulHunter.Gameplay.Hazards.EnemyHoldState, Is.True,
                "Afterwards the still enemy stands again.");
            foreach (var p in shooters) p.Leave();
            still.Leave();
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator AnOrologion_DoesNotCarryIntoTheNextRun()
        {
            // Bug MIS ITM-07: the freeze's end time was a static on Time.time, which runs on across a restart.
            yield return Boot();
            TimeFreezePickup.BeginWorldFreeze(10f);
            Assert.That(TimeFreezePickup.WorldFrozen, Is.True);
            GameSessionManager.Instance.ResetSession(); // what a restart or a new run does
            Assert.That(TimeFreezePickup.WorldFrozen, Is.False, "A new run starts with no Orologion freeze.");
            var prefab = (GameObject)typeof(EnemySpawner).GetField("_stageEnemyPrefab", Private).GetValue(Spawner);
            var enemy = ((GameObject)typeof(EnemySpawner).GetMethod("SpawnEnemy", Private).Invoke(Spawner, new object[] { prefab })).GetComponent<EnemyController>();
            Assert.That(enemy.IsFrozen, Is.False, "The new run's first enemies move.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator CurseGainedMidMinute_SpeedsUpEnemiesOnlyFromTheNextMinute()
        {
            yield return Boot();
            var progression = LevelProgressionManager.Instance;
            progression.SkipTo(60f);
            yield return null;
            float start = Spawner.CurseForSpeedAndSpawns;
            PlayerController.Instance.Stats.AddCurse(0.5f);
            Assert.That(Spawner.CurseForSpeedAndSpawns, Is.EqualTo(start).Within(1e-4f), "Not this minute.");
            progression.SkipTo(120f);
            yield return null;
            Assert.That(Spawner.CurseForSpeedAndSpawns, Is.EqualTo(start + 0.5f).Within(1e-4f), "From the next minute.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator Traps_ShareOneCooldown_TimesLuck()
        {
            Assert.That(PressurePlate.EffectiveCooldown(20f, 1f), Is.EqualTo(20f));
            Assert.That(PressurePlate.EffectiveCooldown(20f, 1.3f), Is.EqualTo(26f).Within(1e-4f), "130 totalLuck -> 26 s.");
            yield return Boot();
            var items = Object.FindFirstObjectByType<StageItemSpawner>();
            yield return WaitUntilRealtime(() => items.Plates.Count >= 2, "the rune plates");
            var a = items.Plates[0];
            var b = items.Plates[1];
            Assert.That(a.IsReady && b.IsReady, Is.True);
            Assert.That(a.Step(), Is.True, "The first plate fires.");
            Assert.That(b.IsReady, Is.False, "Every plate waits: the cooldown is global.");
            Assert.That(b.Step(), Is.False);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
