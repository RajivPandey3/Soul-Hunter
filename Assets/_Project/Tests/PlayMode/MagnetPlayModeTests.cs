using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS Magnet (vampire.survivors.wiki/w/Magnet, Pickups, Treasure_Chest, Vacuum; fetched 2026-10-09): gems "and other
    /// pickups" inside the Magnet radius fly to the player, chests are never pulled, and the Vacuum pulls only gems.
    /// Before, every pickup but the gem and the chicken waited to be walked over (1.5 units).
    /// </summary>
    public class MagnetPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator CoinsAndPickupsInsideTheMagnetRadius_FlyToThePlayer_FarOnesStay()
        {
            yield return BootIntoGameplay();
            StopTheHorde();
            var player = PlayerController.Instance;
            var stats = player.GetComponent<PlayerStats>();
            var pool = PickupPoolManager.Instance;
            float radius = stats.MagnetRadius;
            Assert.That(radius * 0.8f, Is.GreaterThan(1.6f), "The test needs the radius to reach past the walk-over distance.");
            var p = player.transform.position;
            var health = player.GetComponent<HealthController>();
            health.TakeDamage(new DamagePacket(30, p, Vector3.zero, DamageType.Normal));

            var coin = pool.SpawnGold(p + Vector3.right * radius * 0.8f, 1);
            var freeze = pool.SpawnTimeFreeze(p + Vector3.forward * radius * 0.8f);
            var chicken = pool.SpawnChicken(p + Vector3.back * radius * 0.8f);
            var far = pool.SpawnGold(p + Vector3.left * radius * 1.4f, 1);
            var farStart = far.transform.position;

            yield return WaitUntilRealtime(() => !coin.gameObject.activeInHierarchy && !freeze.activeInHierarchy && !chicken.activeInHierarchy,
                "the coin, Time Freeze and chicken inside the Magnet radius to fly in", () => $"radius {radius:0.00}, coin {coin.gameObject.activeInHierarchy}, freeze {freeze.activeInHierarchy}, chicken {chicken.activeInHierarchy}");
            yield return new WaitForSeconds(0.5f);
            Assert.That(far.gameObject.activeInHierarchy, Is.True, "A coin outside the radius stays on the ground.");
            Assert.That(far.IsPulled, Is.False);
            Assert.That(Vector3.Distance(far.transform.position, farStart), Is.LessThan(0.01f));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator Attractorb_WidensTheRadius_ForCoins()
        {
            yield return BootIntoGameplay();
            StopTheHorde();
            var player = PlayerController.Instance;
            var stats = player.GetComponent<PlayerStats>();
            float radius = stats.MagnetRadius;
            var coin = PickupPoolManager.Instance.SpawnGold(player.transform.position + Vector3.right * radius * 1.25f, 1);
            yield return new WaitForSeconds(0.6f);
            Assert.That(coin.gameObject.activeInHierarchy && !coin.IsPulled, Is.True, "Outside the radius the coin waits.");

            stats.MultiplyMagnet(WeaponManager.AttractorbStep(1)); // x1.5: the coin is now inside
            yield return WaitUntilRealtime(() => !coin.gameObject.activeInHierarchy, "the coin to fly in once Attractorb widens the radius");
        }

        [UnityTest]
        public IEnumerator AChest_IsNeverPulled_EvenWithAHugeMagnet()
        {
            yield return BootIntoGameplay();
            StopTheHorde();
            var player = PlayerController.Instance;
            player.GetComponent<PlayerStats>().MultiplyMagnet(10f);
            var chest = PickupPoolManager.Instance.SpawnChest(player.transform.position + Vector3.right * 3f);
            var start = chest.transform.position;
            yield return new WaitForSeconds(1f);
            Assert.That(chest.activeInHierarchy, Is.True, "wiki Treasure_Chest: \"Doesn't get attracted to the player.\"");
            Assert.That(Vector3.Distance(chest.transform.position, start), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator TheVacuum_PullsOnlyGems()
        {
            yield return BootIntoGameplay();
            StopTheHorde();
            var player = PlayerController.Instance;
            var p = player.transform.position;
            var pool = PickupPoolManager.Instance;
            var before = new HashSet<XPGem>(XPGem.ActiveGems);
            for (int i = 0; i < 5; i++) pool.SpawnGem(p + Quaternion.Euler(0f, i * 72f, 0f) * Vector3.forward * 20f, 1);
            var gems = new List<XPGem>();
            foreach (var gem in XPGem.ActiveGems) if (!before.Contains(gem)) gems.Add(gem);
            Assert.That(gems.Count, Is.EqualTo(5));
            var coin = pool.SpawnGold(p + Vector3.right * 25f, 1);
            yield return new WaitForSeconds(0.6f); // a fresh gem cannot be pulled for 0.5 s

            var vacuum = pool.SpawnMagnet(p);
            yield return WaitUntilRealtime(() => !vacuum.activeInHierarchy, "the hero to pick up the Vacuum");
            foreach (var gem in gems) Assert.That(!gem.gameObject.activeInHierarchy || gem.IsMagnetized, Is.True, "wiki Vacuum: every gem flies in.");
            Assert.That(coin.gameObject.activeInHierarchy && !coin.IsPulled, Is.True, "wiki Vacuum: \"Ignores all pickups.\"");
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
