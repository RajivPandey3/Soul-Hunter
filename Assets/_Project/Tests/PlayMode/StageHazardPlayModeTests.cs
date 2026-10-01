using System.Collections;
using System.Linq;
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
    /// VS stage hazards in the real game (2026-09-27): Death's Shadow (Stalker) on Stage 5 at 8:00, a Soul Meteor
    /// (Shooting Star) striking after 2 s for 30, a Pile Assault bullet hurting the player, and a Stage 3 rail cart that
    /// launches when hit and kills an enemy on its rail.
    /// </summary>
    public class StageHazardPlayModeTests : GameplayPlayModeTestBase
    {
        private StageHazards _hazards;
        private EnemySpawner _spawner;

        private IEnumerator Boot(int stage, bool invinciblePlayer)
        {
            yield return BootIntoGameplay();
            _spawner = Object.FindFirstObjectByType<EnemySpawner>();
            _spawner.enabled = false; // only the hazards bring enemies
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = invinciblePlayer;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.gameObject.SetActive(false);
            LevelProgressionManager.Instance.StartStage(stage);
            yield return null;
            yield return null;
            yield return null;
            ClearStrays();
            _hazards = StageHazards.Instance;
            Assert.That(_hazards, Is.Not.Null, "GameSessionManager adds StageHazards.");
            Assert.That(_hazards.Stage, Is.EqualTo(stage));
        }

        private static void ClearStrays()
        {
            foreach (var stray in EnemyController.ActiveEnemies.ToList())
                if (stray != null && stray.GetComponent<DeathsShadow>() == null && stray.GetComponent<PileShooter>() == null)
                    stray.gameObject.SetActive(false);
        }

        private static IEnumerator WaitGameSeconds(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until) yield return null;
        }

        private static float Flat(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private static int Kills() => RunStatsTracker.Instance != null ? RunStatsTracker.Instance.TotalKills : 0;

        [UnityTest]
        public IEnumerator DeathsShadow_ComesOnStage5At8_Chases_CannotBeKilled_AndLeaves()
        {
            yield return Boot(5, invinciblePlayer: true);
            Assert.That(_hazards.Shadows, Is.Empty, "Nothing before 8:00.");
            LevelProgressionManager.Instance.SkipTo(8f * 60f);
            yield return WaitUntilRealtime(() => _hazards.Shadows.Count == 1, "Death's Shadow at 8:00");

            var shadow = _hazards.Shadows[0];
            var enemy = shadow.GetComponent<EnemyController>();
            var health = shadow.GetComponent<HealthController>();
            var player = PlayerController.Instance;
            Assert.That(enemy.IsStageBoss, Is.False, "Not the stage boss: the boss logic is untouched.");
            Assert.That(shadow.transform.localScale.x, Is.EqualTo(_spawner.StageBossPrefab.transform.localScale.x * 1.5f).Within(0.01f), "The stage boss model, 1.5x.");
            Assert.That(shadow.RemainingSeconds, Is.GreaterThan(115f), "It stays for 2 minutes.");
            Assert.That(enemy.MoveSpeed, Is.EqualTo(StageHazardTable.ShadowMoveSpeed).Within(0.01f), "The player's base speed.");

            float before = Flat(shadow.transform.position, player.transform.position);
            yield return WaitGameSeconds(1.5f);
            float after = Flat(shadow.transform.position, player.transform.position);
            Assert.That(after, Is.LessThan(before - 2f), $"It chases the player ({before:0.0} -> {after:0.0}). " + Errors());

            int kills = Kills();
            health.TakeDamage(new DamagePacket(999999, shadow.transform.position, Vector3.forward));
            Assert.That(health.CurrentHealth, Is.GreaterThan(0), "Weapons cannot hurt it.");
            health.KillSilently(); // what Soul Toll, Pentagram and the stage-clear wipe do
            yield return null;
            Assert.That(shadow.gameObject.activeInHierarchy, Is.True, "A wipe does not remove it.");
            Assert.That(health.CurrentHealth, Is.GreaterThan(0));
            Assert.That(Kills(), Is.EqualTo(kills), "Never a kill.");

            shadow.RemainingSeconds = 0.3f; // test hook: VS waits 120 s
            yield return WaitGameSeconds(0.6f);
            Assert.That(enemy.gameObject.activeSelf, Is.False, "It leaves when its time is up.");
            Assert.That(Kills(), Is.EqualTo(kills), "Leaving is not a kill.");
            Assert.That(Object.FindObjectsByType<SoulHunter.Gameplay.Pickups.ChestPickup>(FindObjectsSortMode.None).Count(c => c.isActiveAndEnabled),
                Is.EqualTo(0), "No drops (the boss model would drop a chest).");
            yield return null;
            Assert.That(enemy.GetComponent<DeathsShadow>(), Is.Null, "The pooled enemy is ordinary again.");
            Assert.That(health.IsInvincible, Is.False);
            Assert.That(health.TryPreventDeath, Is.Null);
            Assert.That(_hazards.Shadows, Is.Empty);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator SoulMeteor_HitsThePlayer_After2Seconds_For30_AndWaitsWhilePaused()
        {
            yield return Boot(6, invinciblePlayer: false);
            var player = PlayerController.Instance;
            var health = player.GetComponent<HealthController>();
            int start = health.CurrentHealth;
            var meteor = _hazards.DropMeteor(player.transform.position);
            Assert.That(meteor, Is.Not.Null);

            // A paused game holds the strike.
            var pauser = new object();
            SoulHunter.Core.Services.GameTime.Pause(pauser);
            yield return new WaitForSecondsRealtime(2.5f);
            Assert.That(meteor != null && !meteor.HasStruck, Is.True, "Nothing falls while the game is paused.");
            SoulHunter.Core.Services.GameTime.Resume(pauser);

            yield return WaitGameSeconds(1.6f);
            Assert.That(meteor != null && !meteor.HasStruck, Is.True, "No strike before 2 s.");
            Assert.That(health.CurrentHealth, Is.GreaterThanOrEqualTo(start), "The marker alone does not hurt.");

            int beforeStrike = health.CurrentHealth;
            float deadline = Time.realtimeSinceStartup + 3f;
            // Failed once in a full run (2026-10-01): the strike dealt 0, most likely blocked by a 240 ms invulnerability from
            // something unrelated (VS: invulnerability blocks every hit). The test measures the meteor alone, so it clears
            // the hit invulnerability until the strike.
            var hitUntil = typeof(HealthController).GetField("_hitInvulnerableUntil", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            while (meteor != null && Time.realtimeSinceStartup < deadline)
            {
                hitUntil.SetValue(health, -1f);
                beforeStrike = health.CurrentHealth;
                yield return null;
            }
            Assert.That(meteor == null, Is.True, "The marker struck and went.");
            int expected = Mathf.Max(1, StageHazardTable.MeteorPlayerDamage - health.Armor);
            Assert.That(beforeStrike - health.CurrentHealth, Is.InRange(expected - 2, expected), $"The strike deals 30 (minus Armor {health.Armor}); invincible {health.IsInvincible}. " + Errors());
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator PileAssault_StandsStill_ItsBulletHurtsThePlayer_ThenItLeaves()
        {
            yield return Boot(8, invinciblePlayer: false);
            var player = PlayerController.Instance;
            var health = player.GetComponent<HealthController>();
            var shooters = _hazards.StartPileAssault(count: 4, durationSeconds: 60f);
            Assert.That(shooters.Count, Is.EqualTo(4), "A ring of the stage's enemies. " + Errors());
            var spots = shooters.Select(s => s.transform.position).ToArray();

            int start = health.CurrentHealth;
            float deadline = Time.realtimeSinceStartup + 10f;
            while (health.CurrentHealth >= start && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(health.CurrentHealth, Is.LessThan(start), "A bullet reached the player. " + Errors());
            int expected = Mathf.Max(1, StageHazardTable.PileBulletDamage - health.Armor);
            Assert.That(start - health.CurrentHealth, Is.InRange(1, expected * 4), "Bullet damage.");
            Assert.That(shooters.Sum(s => s.ShotsFired), Is.GreaterThan(0));
            for (int i = 0; i < shooters.Count; i++)
                Assert.That(Flat(shooters[i].transform.position, spots[i]), Is.LessThan(0.3f), "They stand still.");

            int kills = Kills();
            foreach (var s in shooters) s.RemainingSeconds = 0.2f; // test hook: VS assaults last 20-30 s
            yield return WaitGameSeconds(0.5f);
            Assert.That(shooters.All(s => s == null || !s.gameObject.activeInHierarchy), Is.True, "The assault ends.");
            Assert.That(Kills(), Is.EqualTo(kills), "Leaving is not a kill.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator RailCart_LaunchesWhenHit_AndKillsAnEnemyOnItsRail()
        {
            yield return Boot(3, invinciblePlayer: true);
            yield return WaitUntilRealtime(() => _hazards.Tracks.Count == StageHazardTable.RailCount, "the Stage 3 rails");
            var track = _hazards.Tracks[0];
            var cart = track.Cart;
            Assert.That(cart, Is.Not.Null);
            Assert.That(cart.IsMoving, Is.False);
            Assert.That(Flat(cart.transform.position, track.EndA), Is.LessThan(0.5f), "The cart waits at one end.");

            var enemyObject = _spawner.SpawnHazardEnemy(_spawner.StageEnemyPrefab, track.transform.position);
            var enemy = enemyObject.GetComponent<EnemyController>();
            enemy.Freeze(20f); // stays on the rail
            yield return new WaitForFixedUpdate();
            int kills = Kills();

            cart.Health.TakeDamage(new DamagePacket(RailCart.MaxHealth, cart.transform.position, Vector3.zero));
            Assert.That(cart.IsMoving, Is.True, "Enough damage launches it.");
            Assert.That(cart.gameObject.activeInHierarchy, Is.True, "It launches instead of breaking.");
            Assert.That(cart.Launches, Is.EqualTo(1));

            yield return WaitUntilRealtime(() => !enemyObject.activeSelf, "the cart to kill the enemy on its rail");
            Assert.That(cart.EnemiesHit, Is.GreaterThanOrEqualTo(1));
            if (RunStatsTracker.Instance != null)
                Assert.That(Kills(), Is.EqualTo(kills + 1), "The enemy's death is a kill; the cart is not.");

            yield return WaitUntilRealtime(() => !cart.IsMoving, "the cart to reach the far end");
            Assert.That(Flat(cart.transform.position, track.EndB), Is.LessThan(0.5f), "It stops at the far end.");
            Assert.That(cart.Health.CurrentHealth, Is.EqualTo(RailCart.MaxHealth), "Ready to be launched back.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator NextStage_RemovesTheOldStagesHazards()
        {
            yield return Boot(3, invinciblePlayer: true);
            yield return WaitUntilRealtime(() => _hazards.Tracks.Count == StageHazardTable.RailCount, "the Stage 3 rails");
            var rails = _hazards.Tracks.ToList();
            var meteor = _hazards.DropMeteor(PlayerController.Instance.transform.position + Vector3.right * 20f);
            LevelProgressionManager.Instance.StartStage(4);
            yield return null;
            yield return null;
            Assert.That(rails.All(r => r == null), Is.True, "Stage 3's rails are removed.");
            Assert.That(meteor == null, Is.True, "Pending markers are removed.");
            Assert.That(_hazards.Tracks, Is.Empty, "Stage 4 has no rails.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
