using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS rules re-read from the raw wiki pages on 2026-10-12 (the open rows of Docs/VS_DOT_Table.md), in the real game:
    /// a frozen enemy is pushed by walking into it (Freeze); a rune hits an enemy it stays inside once per 0.5 s and
    /// bounces off the screen edge (Runetracer).
    /// </summary>
    public class VsParityWave4PlayModeTests : GameplayPlayModeTestBase
    {
        private EnemySpawner _spawner;

        private IEnumerator QuietArena()
        {
            yield return BootIntoGameplay();
            _spawner = Object.FindFirstObjectByType<EnemySpawner>();
            _spawner.enabled = false;
            yield return null;
            yield return null;
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null) stray.gameObject.SetActive(false);
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
        }

        private EnemyController Enemy(Vector3 offset, int health = 100000)
        {
            var prefab = GetPrivateField<List<GameObject>>(_spawner, "_stageEnemyPrefabs")[LevelProgressionManager.Instance.CurrentStage - 1];
            var player = PlayerController.Instance;
            var enemy = Object.Instantiate(prefab, player.transform.position + offset, Quaternion.identity).GetComponent<EnemyController>();
            enemy.Target = player.transform;
            var touch = enemy.GetComponent<TouchDamage>();
            if (touch != null) touch.DamageAmount = 0;
            var hp = enemy.GetComponent<HealthController>();
            hp.IncreaseMaxHealth(health);
            return enemy;
        }

        private static IEnumerator Seconds(float s)
        {
            float until = Time.time + s;
            while (Time.time < until) yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator AFrozenEnemy_StaysPut_UntilThePlayerWalksIntoIt()
        {
            yield return QuietArena();
            var player = PlayerController.Instance;
            var enemy = Enemy(Vector3.right * 3f);
            yield return null;
            enemy.Freeze(30f);
            yield return Seconds(0.3f);
            Vector3 frozenAt = enemy.transform.position;
            yield return Seconds(0.5f);
            Assert.That(Vector3.Distance(enemy.transform.position, frozenAt), Is.LessThan(0.05f), "A frozen enemy nobody touches does not move.");

            // The player steps into the frozen enemy from the left, half a unit at a time.
            var body = player.GetComponent<Rigidbody>();
            for (int step = 0; step < 6; step++)
            {
                Vector3 to = player.transform.position + Vector3.right * 0.5f;
                if (body != null) body.position = to;
                player.transform.position = to;
                yield return Seconds(0.2f);
            }
            Assert.That(enemy.IsFrozen, Is.True, "Still frozen while it is pushed.");
            float moved = enemy.transform.position.x - frozenAt.x;
            Assert.That(moved, Is.GreaterThan(0.4f), $"The frozen enemy was pushed along the player's way (moved {moved:0.00}).");
            float gap = Mathf.Abs(enemy.transform.position.x - player.transform.position.x);
            Assert.That(gap, Is.GreaterThan(EnemyFrozenState.PlayerPushRadius * 0.9f), $"It ends up outside the player's body (gap {gap:0.00}).");

            Vector3 rest = enemy.transform.position;
            yield return Seconds(0.5f);
            Assert.That(Vector3.Distance(enemy.transform.position, rest), Is.LessThan(0.1f), "Once the player stops, it stays where it was pushed.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator LevelOne_SendsAChestlessWaveBoss_At8Minutes()
        {
            // Raw Mad_Forest "Waves": 8:00 "Bosses & Treasure" = Giant Bat, with no {{treasure}}.
            yield return BootIntoGameplay();
            _spawner = Object.FindFirstObjectByType<EnemySpawner>();
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            Assert.That(_spawner.ChestlessBossesSpawned, Is.EqualTo(0));
            string bossName = SoulHunter.Gameplay.Data.MadForestEnemies.PrefabName("GreatGraveBat");
            Assert.That(EnemyController.ActiveEnemies.Any(e => e != null && e.name.StartsWith(bossName)), Is.False, "No Great Grave Bat before 8:00.");

            LevelProgressionManager.Instance.SkipTo(8f * 60f);
            yield return WaitUntilRealtime(() => _spawner.ChestlessBossesSpawned == 1, "the 8:00 wave boss");
            var boss = EnemyController.ActiveEnemies.FirstOrDefault(e => e != null && e.gameObject.activeInHierarchy && e.name.StartsWith(bossName));
            Assert.That(boss, Is.Not.Null, "One Great Grave Bat (VS Giant Bat) comes at 8:00.");
            Assert.That(EnemyController.ActiveEnemies.Count(e => e != null && e.gameObject.activeInHierarchy && e.name.StartsWith(bossName)), Is.EqualTo(1));
            var drop = boss.GetComponent<EnemyDrop>();
            Assert.That(drop == null || !drop.WillDropChest, Is.True, "It carries no chest.");
            yield return null;
            yield return null;
            Assert.That(_spawner.ChestlessBossesSpawned, Is.EqualTo(1), "Only one.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator ARune_HitsAnEnemyItStaysInside_OncePerHalfSecond_AndBouncesOffTheScreenEdge()
        {
            yield return QuietArena();
            var player = PlayerController.Instance;
            var weapons = WeaponManager.Instance;
            weapons.GiveWeapon(UpgradeData.UpgradeType.Runetracer, 1);
            yield return null;
            var runetracer = player.GetComponentInChildren<RunetracerWeapon>(true);
            Assert.That(runetracer, Is.Not.Null);

            // A frozen target so it does not walk, right next to the hero where the rune starts.
            var enemy = Enemy(Vector3.right * 1.2f);
            yield return null;
            enemy.Freeze(60f);
            var health = enemy.GetComponent<HealthController>();
            runetracer.gameObject.SetActive(true);
            runetracer.ResetCooldownAndFire();
            yield return Seconds(0.05f);

            var rune = Object.FindObjectsByType<HitboxDelay>(FindObjectsSortMode.None).FirstOrDefault(d => d.isActiveAndEnabled);
            Assert.That(rune, Is.Not.Null, "The rune carries the VS hitbox delay.");
            Assert.That(rune.Delay, Is.EqualTo(RunetracerWeapon.VsHitboxDelay));
            var bounce = rune.GetComponent<ScreenBounce>();
            Assert.That(bounce != null && bounce.enabled, Is.True, "The rune bounces off the screen edges.");
            runetracer.gameObject.SetActive(false); // one rune only

            // Hold the rune still inside the enemy and count its hits over 1.6 s: at 0, 0.5, 1.0 and 1.5 s.
            var runeBody = rune.GetComponent<Rigidbody>();
            var lifetime = rune.GetComponent<PooledLifetime>();
            if (lifetime != null) lifetime.Arm(30f);
            int hits = 0;
            float last = health.CurrentHealth;
            float start = Time.time;
            // Count from the first held frame; hits the rune dealt on its way in are not counted.
            runeBody.linearVelocity = Vector3.zero;
            runeBody.position = enemy.transform.position;
            rune.transform.position = enemy.transform.position;
            yield return new WaitForFixedUpdate();
            last = health.CurrentHealth;
            start = Time.time;
            var times = new List<float>();
            while (Time.time - start < 1.6f)
            {
                runeBody.linearVelocity = Vector3.zero;
                runeBody.position = enemy.transform.position;
                yield return new WaitForFixedUpdate();
                if (health.CurrentHealth < last - 0.5f)
                {
                    hits++;
                    times.Add(Time.time - start);
                    last = health.CurrentHealth;
                }
            }
            string when = string.Join(", ", times.Select(t => t.ToString("0.00")));
            Assert.That(hits, Is.InRange(2, 4), $"About one hit per 0.5 s while it stays inside (hits at {when}).");
            for (int i = 1; i < times.Count; i++)
                Assert.That(times[i] - times[i - 1], Is.GreaterThan(0.4f), $"Never twice within the delay (hits at {when}).");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
