using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Hazards;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// The stage bosses' own parts in the real game (raw 2026-10-28): Level 4's Giant Enemy Crab "has 2 Pincers ... can
    /// regenerate a short while after being destroyed", "Each pincer has 17 lives", and "can also summon a Drowner, if the
    /// player spends longer than 6 seconds below it"; Level 6's Orochimario "spawns with eight heads, the first head has
    /// double the health ... When they take damage, Orochimario itself also take that much ... The heads can lunge".
    /// </summary>
    public class BossPartsPlayModeTests : GameplayPlayModeTestBase
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        private IEnumerator SpawnGuardian(int level, System.Action<EnemyController> found)
        {
            yield return BootIntoGameplay();
            var player = PlayerController.Instance;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            player.GetComponent<HealthController>().IsInvincible = true;
            LevelProgressionManager.Instance.StartStage(level);
            yield return null;
            yield return null;
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            foreach (var stray in EnemyController.ActiveEnemies.ToList()) if (stray != null) stray.gameObject.SetActive(false);
            var bossPrefab = GetPrivateField<GameObject>(spawner, "_stageBossPrefab");
            Assert.IsTrue((bool)typeof(EnemySpawner).GetMethod("SpawnBoss", Private).Invoke(spawner, new object[] { bossPrefab }), Errors());
            yield return null;
            found(EnemyController.ActiveEnemies.Last(e => e != null && spawner.IsBoss(e)));
        }

        private static int Level => PlayerController.Instance.GetComponent<PlayerExperience>().CurrentLevel;

        [UnityTest]
        public IEnumerator LevelFour_TheCrab_HasTwoPincers_ThatRegrow_AndStandingBelowItSummonsTheDrowner()
        {
            EnemyController crab = null;
            yield return SpawnGuardian(4, c => crab = c);
            var parts = crab.GetComponent<BossParts>();
            Assert.That(parts, Is.Not.Null, Errors());
            Assert.That(parts.PartsAlive, Is.EqualTo(2), "2 Pincers.");
            var pincer = parts.LiveParts.First();
            Assert.That(pincer.GetComponent<HealthController>().MaxHealth, Is.EqualTo(5 * Level), "Pincer: 5 HP x Level.");
            Assert.That(parts.PincerLivesLeft(0), Is.EqualTo(BossParts.PincerLives - 1));

            // Destroyed, it grows back a short while later.
            var health = pincer.GetComponent<HealthController>();
            health.TakeDamage(new DamagePacket(health.CurrentHealth + 1, pincer.transform.position, Vector3.forward));
            yield return null;
            Assert.That(parts.PartsAlive, Is.EqualTo(1));
            yield return WaitUntilRealtime(() => parts.PartsAlive == 2, "the pincer growing back");
            Assert.That(parts.PincerLivesLeft(0) + parts.PincerLivesLeft(1), Is.EqualTo(2 * BossParts.PincerLives - 3));

            // The hero below the crab for 6 seconds: the Drowner comes.
            crab.Freeze(30f);
            var player = PlayerController.Instance;
            Vector3 below = crab.transform.position + Vector3.back * 3f;
            below.y = player.transform.position.y;
            float until = Time.realtimeSinceStartup + 12f;
            while (!parts.DrownerSummoned && Time.realtimeSinceStartup < until)
            {
                player.Rigidbody.position = below;
                player.transform.position = below;
                yield return null;
            }
            Assert.That(parts.DrownerSummoned, Is.True, "summon a Drowner, if the player spends longer than 6 seconds below it");
            Assert.That(Object.FindObjectsByType<RisingTide>(FindObjectsSortMode.None).Length, Is.EqualTo(1));

            // The crab's death takes its pincers with it.
            var crabHealth = crab.GetComponent<HealthController>();
            crabHealth.TakeDamage(new DamagePacket(crabHealth.CurrentHealth + 1, crab.transform.position, Vector3.forward));
            yield return null;
            Assert.That(parts.PartsAlive, Is.EqualTo(0));
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator LevelSix_TheHydra_HasEightHeads_TheirDamageReachesTheBody_AndTheyLunge()
        {
            EnemyController hydra = null;
            yield return SpawnGuardian(6, h => hydra = h);
            var parts = hydra.GetComponent<BossParts>();
            Assert.That(parts, Is.Not.Null, Errors());
            Assert.That(parts.PartsAlive, Is.EqualTo(BossParts.Heads), "eight heads");
            var heads = parts.LiveParts.Select(h => h.GetComponent<HealthController>()).ToArray();
            int one = heads[1].MaxHealth;
            Assert.That(one, Is.EqualTo(500 * Level), "a head: 500 HP x Level");
            Assert.That(heads[0].MaxHealth, Is.EqualTo(2 * one), "the first head has double the health");
            var body = hydra.GetComponent<HealthController>();
            Assert.That(body.MaxHealth, Is.EqualTo(500 * Level + heads.Sum(h => h.MaxHealth)), "total health is actually 5,000 x Level");
            Assert.That(body.MaxHealth, Is.EqualTo(5000 * Level));

            int before = body.CurrentHealth;
            heads[2].TakeDamage(new DamagePacket(100, heads[2].transform.position, Vector3.forward));
            Assert.That(before - body.CurrentHealth, Is.EqualTo(100), "Orochimario itself also take that much");

            heads[3].TakeDamage(new DamagePacket(heads[3].CurrentHealth + 50, heads[3].transform.position, Vector3.forward));
            yield return null;
            Assert.That(parts.PartsAlive, Is.EqualTo(BossParts.Heads - 1), "they can be individually defeated");

            yield return WaitUntilRealtime(() => parts.LungesStarted > 0, "a head's lunge (every 5 to 10 s)");
            body.TakeDamage(new DamagePacket(body.CurrentHealth + 1, hydra.transform.position, Vector3.forward));
            yield return null;
            Assert.That(parts.PartsAlive, Is.EqualTo(0), "the heads go with the body");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
