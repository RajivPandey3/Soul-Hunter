using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;
using UpgradeType = SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS light sources in the real game: Soul Lanterns appear just off-screen (never inside an obstacle, never more
    /// than 10), weapons break them, and a broken lantern drops an item without counting as a kill.
    /// </summary>
    public class LightSourcePlayModeTests : GameplayPlayModeTestBase
    {
        private IEnumerator QuietArena()
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null) stray.gameObject.SetActive(false);
            PlayerController.Instance.GetComponent<HealthController>().IsInvincible = true;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Lanterns_AppearJustOffScreen_OnOpenGround_AtMostTen()
        {
            yield return QuietArena();
            var player = PlayerController.Instance;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            var lanterns = Object.FindFirstObjectByType<LightSourceSpawner>();
            Assert.IsNotNull(lanterns, "Every run has a light-source spawner. " + Errors());
            player.Stats.AddLuck(9f); // chance 10% x Luck 10 = the 50% cap, so the test does not wait on luck

            var cam = Camera.main;
            var seen = new HashSet<LightSource>();
            float deadline = Time.realtimeSinceStartup + 60f;
            int most = 0;
            while (Time.realtimeSinceStartup < deadline && most < LightSourceSpawner.MaxActive)
            {
                yield return null;
                foreach (var lantern in lanterns.Active)
                {
                    if (lantern == null || !seen.Add(lantern)) continue;
                    Vector3 view = cam.WorldToViewportPoint(lantern.transform.position + Vector3.up * 0.6f);
                    Assert.That(view.x < 0f || view.x > 1f || view.y < 0f || view.y > 1f, Is.True,
                        $"A lantern appeared on screen at viewport {view}.");
                    Assert.That(Physics.CheckSphere(lantern.transform.position + Vector3.up * 0.8f, 0.5f, LayerMask.GetMask("Obstacle"),
                        QueryTriggerInteraction.Collide), Is.False, "A lantern appeared inside a stage obstacle.");
                }
                most = Mathf.Max(most, lanterns.Active.Count);
            }
            Assert.That(most, Is.EqualTo(LightSourceSpawner.MaxActive), "Lanterns keep appearing up to the limit. " + Errors());
            float hold = Time.realtimeSinceStartup + 5f;
            while (Time.realtimeSinceStartup < hold)
            {
                yield return null;
                Assert.That(lanterns.Active.Count, Is.LessThanOrEqualTo(LightSourceSpawner.MaxActive), "Never more than 10 at once.");
            }
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static IEnumerable<UpgradeType> Breakers() => new[] { UpgradeType.Whip, UpgradeType.Garlic, UpgradeType.Bible };

        [UnityTest]
        public IEnumerator Weapons_BreakALantern_WhichDropsAnItem_AndIsNotAKill([ValueSource(nameof(Breakers))] UpgradeType type)
        {
            yield return QuietArena();
            var player = PlayerController.Instance;
            var weapons = WeaponManager.Instance;
            var prefab = weapons.GetWeaponObject(type);
            foreach (var existing in player.GetComponentsInChildren<AutoAttackWeapon>())
                if (!existing.gameObject.name.StartsWith(prefab.name)) existing.gameObject.SetActive(false);
            weapons.GiveWeapon(type, 1);

            var spawner = Object.FindFirstObjectByType<LightSourceSpawner>();
            // Bible books orbit at radius 3, so its lantern stands on the ring; the others reach 1.8.
            Vector3 at = player.transform.position + player.FacingDirection * (type == UpgradeType.Bible ? 3f : 1.8f);
            var lantern = spawner.Spawn(at);
            Assert.IsNotNull(lantern, "The Soul Lantern prefab is wired. " + Errors());
            Physics.SyncTransforms();
            LightSourceDrops.Drop? dropped = null;
            int itemsAtLantern = 0;
            // Checked the moment it breaks: a coin this close to the player is magnetised away within frames.
            lantern.Broken += (l, d) =>
            {
                dropped = d;
                itemsAtLantern = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                    .Count(b => (b is Pickup || b is HealthPickup) && b.gameObject.activeInHierarchy &&
                                Vector3.Distance(b.transform.position, l.transform.position) < 1f);
            };
            int killsBefore = RunStatsTracker.Instance != null ? RunStatsTracker.Instance.TotalKills : 0;

            float deadline = Time.realtimeSinceStartup + 10f;
            while (lantern.gameObject.activeSelf && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(lantern.gameObject.activeSelf, Is.False, $"{type} broke the lantern next to the player within 10 s. " + Errors());
            Assert.That(dropped.HasValue, Is.True, "Breaking it rolls a drop.");
            Assert.That(itemsAtLantern, Is.GreaterThan(0), $"A {dropped} lies where the lantern stood.");
            int killsAfter = RunStatsTracker.Instance != null ? RunStatsTracker.Instance.TotalKills : 0;
            Assert.That(killsAfter, Is.EqualTo(killsBefore), "A lantern is not a kill.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
