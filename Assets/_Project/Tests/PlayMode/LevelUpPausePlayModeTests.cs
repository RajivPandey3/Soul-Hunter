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

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// VS wiki (Level_up): "Upon leveling up, the game is paused ... After selecting an item, the game resumes". Owner
    /// report (2026-09-30): behind the level-up screen the game kept running. While a level-up is open nothing may move:
    /// the stage clock, enemies, spawns, the player, gems and projectiles.
    /// </summary>
    public class LevelUpPausePlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator BehindTheLevelUpScreen_NothingMoves()
        {
            yield return BootIntoGameplay();
            // Let the run get going: enemies arrive and the weapons fire.
            yield return new WaitForSeconds(3f);
            var player = PlayerController.Instance;
            var levelUp = Object.FindFirstObjectByType<LevelUpManager>();
            var experience = player.GetComponent<PlayerExperience>();
            List<UpgradeData> choices = null;
            levelUp.OnLevelUpUIRound += round => choices = round;
            experience.AddXP(experience.XPToNextLevel - experience.CurrentXP);
            yield return WaitUntilRealtime(() => levelUp.IsLevelUpActive, "the level-up screen");
            yield return null;

            var progression = LevelProgressionManager.Instance;
            float clock = progression.StageElapsedTime;
            int enemies = EnemyController.ActiveEnemies.Count;
            var enemyPositions = EnemyController.ActiveEnemies.Where(e => e != null).ToDictionary(e => e, e => e.transform.position);
            var gemPositions = XPGem.ActiveGems.Where(g => g != null).ToDictionary(g => g, g => g.transform.position);
            Vector3 playerAt = player.transform.position;
            var projectiles = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Where(p => p.isActiveAndEnabled).ToDictionary(p => p, p => p.transform.position);

            // Everything else in the world too: any moving object, playing animator or particle system is reported by name.
            var world = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Where(t => t.GetComponentInParent<Canvas>() == null && t.GetComponentInParent<Camera>() == null)
                .ToDictionary(t => t, t => (t.position, t.rotation, t.localScale));
            var animators = Object.FindObjectsByType<Animator>(FindObjectsSortMode.None).Where(a => a.isActiveAndEnabled && a.runtimeAnimatorController != null)
                .ToDictionary(a => a, a => a.GetCurrentAnimatorStateInfo(0).normalizedTime);
            var particles = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Where(p => p.isPlaying).ToDictionary(p => p, p => p.time);

            float end = Time.realtimeSinceStartup + 1.5f;
            var notes = new List<string>();
            while (Time.realtimeSinceStartup < end)
            {
                if (Time.timeScale != 0f) notes.Add($"timeScale {Time.timeScale}");
                yield return null;
            }

            var movers = world.Where(p => p.Key != null && p.Key.gameObject.activeInHierarchy &&
                                          ((p.Key.position - p.Value.position).sqrMagnitude > 1e-6f || Quaternion.Angle(p.Key.rotation, p.Value.rotation) > 0.01f ||
                                           (p.Key.localScale - p.Value.localScale).sqrMagnitude > 1e-6f))
                .Select(p => PathOf(p.Key)).Take(25).ToList();
            var playing = animators.Where(p => p.Key != null && p.Key.isActiveAndEnabled && Mathf.Abs(p.Key.GetCurrentAnimatorStateInfo(0).normalizedTime - p.Value) > 1e-4f)
                .Select(p => PathOf(p.Key.transform)).Take(15).ToList();
            var emitting = particles.Where(p => p.Key != null && p.Key.isPlaying && !p.Key.main.useUnscaledTime && Mathf.Abs(p.Key.time - p.Value) > 1e-4f)
                .Select(p => PathOf(p.Key.transform)).Take(15).ToList();
            Assert.That(movers, Is.Empty, "Nothing in the world moves while the level-up is open: " + string.Join(" | ", movers));
            Assert.That(playing, Is.Empty, "No animator plays: " + string.Join(" | ", playing));
            Assert.That(emitting, Is.Empty, "No game-time particles play: " + string.Join(" | ", emitting));

            Assert.That(levelUp.IsLevelUpActive, Is.True, "The level-up screen is still open.");
            Assert.That(Time.timeScale, Is.EqualTo(0f), "VS: the game is paused. " + string.Join("; ", notes.Distinct()));
            Assert.That(progression.StageElapsedTime, Is.EqualTo(clock), "The stage clock stops.");
            Assert.That(EnemyController.ActiveEnemies.Count, Is.EqualTo(enemies), "No enemies spawn.");
            var movedEnemies = enemyPositions.Where(p => p.Key != null && (p.Key.transform.position - p.Value).sqrMagnitude > 1e-6f).Select(p => p.Key.name).ToList();
            Assert.That(movedEnemies, Is.Empty, $"No enemy moves ({movedEnemies.Count} did).");
            var movedGems = gemPositions.Where(p => p.Key != null && (p.Key.transform.position - p.Value).sqrMagnitude > 1e-6f).ToList();
            Assert.That(movedGems, Is.Empty, $"No gem flies ({movedGems.Count} did).");
            Assert.That((player.transform.position - playerAt).sqrMagnitude, Is.LessThan(1e-6f), "The player does not move.");
            var movedShots = projectiles.Where(p => p.Key != null && p.Key.isActiveAndEnabled && (p.Key.transform.position - p.Value).sqrMagnitude > 1e-6f).Select(p => p.Key.name).ToList();
            Assert.That(movedShots, Is.Empty, $"No projectile flies ({string.Join(", ", movedShots.Distinct())}).");

            // VS: "After selecting an item, the game resumes and the character gains a brief moment of invulnerability."
            Assert.That(choices, Is.Not.Null.And.Not.Empty);
            levelUp.SelectUpgrade(choices[0]);
            yield return null;
            Assert.That(levelUp.IsLevelUpActive, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f), "The game resumes after the pick.");
            Assert.That(player.GetComponent<HealthController>().IsHitInvulnerable, Is.True, "A brief moment of invulnerability after the pick.");

            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static string PathOf(Transform t)
        {
            var parts = new List<string>();
            for (var p = t; p != null && parts.Count < 4; p = p.parent) parts.Insert(0, p.name);
            return string.Join("/", parts);
        }
    }
}
