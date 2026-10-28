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
    
        [UnityTest]
        public IEnumerator LevelSeven_TheAvatarOfGaea_ClearsTheScreen_HealsByTheRunsKills_AndSummonsApparitions()
        {
            // Avatar_of_Gaea / Lake_Foscari (raw 2026-10-28): "Before the Avatar of Gaea spawns, all the enemies on the screen
            // are cleared to increase the enemy kill count"; "It starts off with very little health and once it is defeated,
            // it becomes invulnerable"; "spawn its own Ghostly Apparitions every 0.1 seconds 101 times"; "It increases its
            // max health by the number of enemies killed in this run".
            yield return BootIntoGameplay();
            var player = PlayerController.Instance;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            player.GetComponent<HealthController>().IsInvincible = true;
            LevelProgressionManager.Instance.StartStage(7);
            yield return null;
            yield return null;
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            yield return WaitUntilRealtime(() => EnemyController.ActiveEnemies.Count(e => e != null && e.isActiveAndEnabled) >= 5, "some of the horde");
            // The horde spawns past the screen edge: bring five of it onto the screen, beside the hero.
            int placed = 0;
            foreach (var enemy in EnemyController.ActiveEnemies.Where(e => e != null && e.isActiveAndEnabled).Take(5).ToList())
            {
                enemy.transform.position = player.transform.position + new Vector3(1.5f + placed++, 0f, 1f);
                enemy.Freeze(10f);
            }
            int killsBefore = RunStatsTracker.Instance.TotalKills;
            var bossPrefab = GetPrivateField<GameObject>(spawner, "_stageBossPrefab");
            Assert.IsTrue((bool)typeof(EnemySpawner).GetMethod("SpawnBoss", Private).Invoke(spawner, new object[] { bossPrefab }), Errors());
            Assert.That(RunStatsTracker.Instance.TotalKills, Is.GreaterThan(killsBefore), "The screen is cleared as kills.");
            var avatar = EnemyController.ActiveEnemies.Last(e => e != null && spawner.IsBoss(e));
            var gaea = avatar.GetComponent<AvatarOfGaea>();
            Assert.That(gaea, Is.Not.Null);
            Assert.That(spawner.MinimumOverride, Is.EqualTo(AvatarOfGaea.MinimumBefore), "The enemy minimum is 1 at first.");
            var health = avatar.GetComponent<HealthController>();
            Assert.That(health.MaxHealth, Is.EqualTo(AvatarOfGaea.StartHealth), "health = 8");

            health.TakeDamage(new DamagePacket(100, avatar.transform.position, Vector3.zero, DamageType.Holy));
            Assert.That(health.CurrentHealth, Is.GreaterThan(0), "Defeated the first time, it does not die.");
            Assert.That(gaea.Current, Is.EqualTo(AvatarOfGaea.Phase.Healing));
            Assert.That(health.IsInvincible, Is.True, "it becomes invulnerable");
            // 2 s countdown + 5 s + 101 x 0.1 s: longer than the usual 20 s wait.
            float until = Time.realtimeSinceStartup + 40f;
            while (gaea.Current != AvatarOfGaea.Phase.Vulnerable && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(gaea.Current, Is.EqualTo(AvatarOfGaea.Phase.Vulnerable), "the Avatar's healing and its apparitions. " + Errors());
            Assert.That(gaea.ApparitionsSummoned, Is.EqualTo(AvatarOfGaea.Apparitions), "101 Ghostly Apparitions");
            Assert.That(spawner.MinimumOverride, Is.EqualTo(-1), "The minimum is the minute's again (150).");
            Assert.That(health.IsInvincible, Is.False);
            Assert.That(health.MaxHealth, Is.EqualTo(AvatarOfGaea.HealedHealthFor(RunStatsTracker.Instance.TotalKills)).Within(5),
                "8 + the enemies killed this run");
            health.TakeDamage(new DamagePacket(health.CurrentHealth, avatar.transform.position, Vector3.zero, DamageType.Holy));
            yield return null;
            Assert.That(health.CurrentHealth, Is.EqualTo(0), "The second time, it is defeated.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
