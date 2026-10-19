using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Core.Events;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Owner decision (2026-09-30): the dash only moves and avoids damage, as the GDD says ("Press Shift to dash through
    /// enemies and avoid damage"). It used to deal a flat 50 damage to every enemy it passed.
    /// </summary>
    public class DashPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator TheDash_MovesThroughEnemies_WithoutHurtingThem_AndAvoidsDamage()
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            var player = PlayerController.Instance;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
            yield return null;

            // An enemy right in the dash path (the dash covers 15 x 0.2 = 3 units along the facing).
            var prefab = GetPrivateField<List<GameObject>>(spawner, "_stageEnemyPrefabs")[0];
            Vector3 start = player.transform.position;
            var enemy = Object.Instantiate(prefab, start + player.FacingDirection * 1.5f, Quaternion.identity);
            var enemyHealth = enemy.GetComponent<HealthController>();
            yield return null;
            int enemyBefore = enemyHealth.CurrentHealth;
            var playerHealth = player.GetComponent<HealthController>();

            GameServices.Instance.Get<EventBus>().Raise(new PlayerDashEvent());
            yield return null;
            Assert.That(player.IsDashing, Is.True, "The dash starts.");
            Assert.That(playerHealth.IsAbilityInvulnerable, Is.True, "The dash avoids damage.");
            yield return new WaitForSeconds(0.3f);

            Assert.That(Vector3.Distance(start, player.transform.position), Is.GreaterThan(2f), "The dash moves the hero.");
            Assert.That(enemyHealth.CurrentHealth, Is.EqualTo(enemyBefore), "The dash deals no damage.");
            Assert.That(playerHealth.IsAbilityInvulnerable, Is.False, "Invulnerability ends with the dash.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator TheDash_StopsAtAWall()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var player = PlayerController.Instance;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
            yield return null;

            // A wall on the Obstacle layer (like stage obstacles and the Castle corridor walls) 1.5 units ahead.
            Vector3 start = player.transform.position;
            Vector3 facing = player.FacingDirection;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.layer = LayerMask.NameToLayer("Obstacle");
            wall.transform.position = start + facing * 2f;
            wall.transform.rotation = Quaternion.LookRotation(facing);
            wall.transform.localScale = new Vector3(6f, 4f, 1f);
            Physics.SyncTransforms();

            GameServices.Instance.Get<EventBus>().Raise(new PlayerDashEvent());
            yield return new WaitForSeconds(0.4f);
            float travelled = Vector3.Dot(player.transform.position - start, facing);
            Assert.That(travelled, Is.LessThan(1.5f - 0.35f), $"The dash stops before the wall face (1.5 ahead); it went {travelled:0.00}.");
            Assert.That(travelled, Is.GreaterThan(0.5f), "It still moved up to the wall.");
            Object.Destroy(wall);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        /// <summary>
        /// Bug MIS CMB-10 / CORE-06 (2026-10-19): the dash and Soul Burst shared HealthController.IsInvincible, so the
        /// first to end cleared the other's immunity and the bot's or benchmark's god mode (IsInvincible = true).
        /// </summary>
        [UnityTest]
        public IEnumerator TheDash_EndsNeitherGodMode_NorSoulBurstsImmunity()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var player = PlayerController.Instance;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
            var health = player.GetComponent<HealthController>();
            var bus = GameServices.Instance.Get<EventBus>();
            // No horde: the burst's kills would drop gems whose level-up pauses the game mid-check.
            foreach (var stray in new List<EnemyController>(EnemyController.ActiveEnemies))
                if (stray != null) stray.gameObject.SetActive(false);
            yield return null;

            // God mode, as -sh-autoplay-god and the benchmark set it, outlives a dash.
            health.IsInvincible = true;
            bus.Raise(new PlayerDashEvent());
            yield return null;
            Assert.That(player.IsDashing, Is.True, "The dash starts.");
            yield return new WaitForSeconds(0.35f);
            Assert.That(health.IsInvincible, Is.True, "Ending the dash does not clear god mode.");
            health.IsInvincible = false;
            yield return new WaitUntil(() => !player.IsDashing); // the dash cooldown

            // Soul Burst's 2 s immunity outlives a dash that starts and ends inside it.
            var burst = Object.FindFirstObjectByType<SoulBurstController>();
            Assert.IsNotNull(burst, "The player has the Soul Burst ultimate.");
            typeof(SoulBurstController).GetMethod("TriggerSoulBurst", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(burst, null);
            float slowEnds = Time.realtimeSinceStartup + 0.6f; // its 0.5 s real-time hit-stop
            while (Time.realtimeSinceStartup < slowEnds) yield return null;
            Assert.That(health.IsAbilityInvulnerable, Is.True, "Soul Burst makes the hero immune.");
            float burstStarted = Time.time;
            bus.Raise(new PlayerDashEvent());
            yield return null;
            Assert.That(player.IsDashing, Is.True, "A dash starts during the burst's immunity.");
            yield return new WaitForSeconds(0.35f);
            Assert.That(Time.time - burstStarted, Is.LessThan(1.4f), "Still inside the burst's 1.5 s of game time.");
            Assert.That(health.IsAbilityInvulnerable, Is.True, "Ending the dash does not end Soul Burst's immunity.");
            int before = health.CurrentHealth;
            health.TakeDamage(new DamagePacket(10, player.transform.position, Vector3.zero) { IgnoresHitInvulnerability = true });
            Assert.That(health.CurrentHealth, Is.EqualTo(before), "No damage while the burst's immunity runs.");

            yield return new WaitForSeconds(1.5f);
            Assert.That(health.IsAbilityInvulnerable, Is.False, "The burst's immunity ends on its own.");
            Assert.That(health.IsInvincible, Is.False);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
