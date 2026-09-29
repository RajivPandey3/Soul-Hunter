using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.VFX;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Heavy-kill performance pass (2026-09-29). A build profile of a strong hero showed hundreds of uncollected coins and
    /// chickens each running a physics overlap query every frame (about 2 ms), and 850 bobbing pickups each running their
    /// own Update (about 1.2 ms). Pickups now ask PlayerReach (no physics query) and one runner moves every bobbing pickup.
    /// These tests check that gameplay is unchanged: the same reach as an overlap sphere, coins and chickens still
    /// collected, and pickups still bob.
    /// </summary>
    public class PickupPerformancePlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator PlayerReach_MatchesAnOverlapSphere_OnThePlayerLayer()
        {
            yield return BootIntoGameplay();
            StopTheHorde();
            yield return new WaitForFixedUpdate();
            Physics.SyncTransforms();
            var player = PlayerController.Instance.transform.position;
            int mask = LayerMask.GetMask("Player");
            var random = new System.Random(7);
            int inside = 0, outside = 0;
            for (int i = 0; i < 400; i++)
            {
                var offset = new Vector3((float)random.NextDouble() * 8f - 4f, (float)random.NextDouble() * 3f - 1.5f, (float)random.NextDouble() * 8f - 4f);
                float radius = 0.5f + (float)random.NextDouble() * 2.5f;
                var point = player + offset;
                bool physics = Physics.CheckSphere(point, radius, mask, QueryTriggerInteraction.Collide);
                bool reach = PlayerReach.Within(point, radius) != null;
                Assert.That(reach, Is.EqualTo(physics), $"Point {offset} radius {radius:0.00}: overlap sphere says {physics}.");
                if (physics) inside++; else outside++;
            }
            Assert.That(inside, Is.GreaterThan(20), "The sample reaches the player.");
            Assert.That(outside, Is.GreaterThan(20), "The sample also misses the player.");
        }

        [UnityTest]
        public IEnumerator CoinsAndChickens_AreStillCollected_AndFarOnesStayPut()
        {
            yield return BootIntoGameplay();
            StopTheHorde();
            var pool = PickupPoolManager.Instance;
            var player = PlayerController.Instance;
            var health = player.GetComponent<HealthController>();
            int gold = GameServices.Instance.Get<EconomyService>().CurrentGold;

            var near = pool.SpawnGold(player.transform.position + Vector3.right * 0.8f, 1);
            var far = pool.SpawnGold(player.transform.position + Vector3.right * 12f, 1);
            health.TakeDamage(new DamagePacket(40, player.transform.position, Vector3.zero, DamageType.Normal));
            int hurt = health.CurrentHealth;
            var chicken = pool.SpawnChicken(player.transform.position + Vector3.forward * 1.5f);
            yield return WaitUntilRealtime(() => !near.gameObject.activeInHierarchy && !chicken.activeInHierarchy, "the coin and chicken next to the hero to be collected");
            Assert.That(GameServices.Instance.Get<EconomyService>().CurrentGold, Is.GreaterThan(gold), "The coin paid out.");
            Assert.That(health.CurrentHealth, Is.GreaterThan(hurt), "The chicken healed.");
            Assert.That(far.gameObject.activeInHierarchy, Is.True, "A coin 12 units away stays on the ground.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator BobbingPickups_AreMovedByOneRunner()
        {
            yield return BootIntoGameplay();
            StopTheHorde();
            var pool = PickupPoolManager.Instance;
            var origin = PlayerController.Instance.transform.position + Vector3.forward * 20f;
            var spawned = new List<GameObject>();
            for (int i = 0; i < 60; i++) spawned.Add(pool.SpawnChicken(origin + new Vector3(i % 10, 0f, i / 10) * 1.5f));
            yield return null;

            var motions = new List<PickupIdleMotion>();
            foreach (var go in spawned) motions.AddRange(go.GetComponentsInChildren<PickupIdleMotion>());
            Assert.That(motions, Is.Not.Empty, "Chickens bob.");
            int enabled = Object.FindObjectsByType<PickupIdleMotion>(FindObjectsSortMode.None).Length;
            Assert.That(PickupIdleMotionRunner.Count, Is.EqualTo(enabled), "Every enabled bobbing pickup is on the runner, once.");

            var visual = motions[0].Visual;
            var start = visual.localPosition;
            var turn = visual.rotation;
            yield return new WaitForSeconds(0.3f);
            Assert.That(visual.localPosition != start || visual.rotation != turn, Is.True, "The runner moves the visual.");

            foreach (var go in spawned) go.SetActive(false);
            yield return null;
            Assert.That(PickupIdleMotionRunner.Count, Is.EqualTo(Object.FindObjectsByType<PickupIdleMotion>(FindObjectsSortMode.None).Length),
                "Collected pickups leave the runner.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator EveryGroundPickup_IsTickedOnce_ByThePickupTicker()
        {
            yield return BootIntoGameplay();
            StopTheHorde();
            var pool = PickupPoolManager.Instance;
            var origin = PlayerController.Instance.transform.position + Vector3.forward * 15f;
            for (int i = 0; i < 20; i++) pool.SpawnGold(origin + Vector3.right * i, 1);
            for (int i = 0; i < 10; i++) pool.SpawnChicken(origin + Vector3.back * 3f + Vector3.right * i);
            yield return null;
            var all = new List<ITickedPickup>();
            all.AddRange(Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None));
            all.AddRange(Object.FindObjectsByType<HealthPickup>(FindObjectsSortMode.None));
            int enabled = all.Count;
            var missing = all.FindAll(p => p.TickerIndex < 0);
            Assert.That(missing, Is.Empty, "Pickups not on the ticker: " + string.Join(", ", missing.ConvertAll(p => ((Component)p).name + (((Component)p).gameObject.activeInHierarchy ? " (active)" : " (inactive)"))));
            var indexes = all.ConvertAll(p => p.TickerIndex);
            var duplicates = indexes.FindAll(i => indexes.FindAll(j => j == i).Count > 1);
            Assert.That(PickupTicker.Count, Is.EqualTo(enabled),
                $"Every enabled coin, chicken and item is on the ticker, once. Indexes: {string.Join(",", indexes)}; duplicates: {string.Join(",", duplicates)}; " +
                $"types: {string.Join(",", all.ConvertAll(p => p.GetType().Name))}");
            Assert.That(enabled, Is.GreaterThanOrEqualTo(30));
        }

        [UnityTest]
        public IEnumerator ABurstOfGold_IsSavedOnce_AndThePendingSaveIsWrittenSoonAfter()
        {
            // Gold pickups used to write the save file on every coin (several a second with a strong hero).
            yield return BootIntoGameplay();
            StopTheHorde();
            var save = GameServices.Instance.Get<SoulHunter.Core.Persistence.SaveService>();
            var economy = GameServices.Instance.Get<EconomyService>();
            save.SaveGame();
            int before = SoulHunter.Core.Persistence.SaveService.SavesWritten;
            int gold = economy.CurrentGold;
            for (int i = 0; i < 10; i++) economy.AddGold(1);
            Assert.That(SoulHunter.Core.Persistence.SaveService.SavesWritten, Is.EqualTo(before), "Ten coins right after a save do not write ten saves.");
            Assert.That(save.HasPendingSave, Is.True, "The gold waits to be saved.");
            Assert.That(economy.CurrentGold, Is.EqualTo(gold + 10), "The gold itself is counted at once.");
            yield return new WaitForSecondsRealtime(SoulHunter.Core.Persistence.SaveService.SaveSoonSeconds + 0.5f);
            Assert.That(SoulHunter.Core.Persistence.SaveService.SavesWritten, Is.EqualTo(before + 1), "One save writes the whole burst.");
            Assert.That(save.HasPendingSave, Is.False);
            Assert.That(save.CurrentData.Currency, Is.EqualTo(gold + 10));
        }

        private static void StopTheHorde()
        {
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            foreach (var enemy in new List<EnemyController>(EnemyController.ActiveEnemies)) if (enemy != null) enemy.gameObject.SetActive(false);
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
            PlayerController.Instance.GetComponent<HealthController>().IsInvincible = false;
        }
    }
}
