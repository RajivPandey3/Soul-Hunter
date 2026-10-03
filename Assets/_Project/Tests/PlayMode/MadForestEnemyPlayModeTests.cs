using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Level 1 spawns the VS Mad Forest enemy of each minute with its wiki stats (owner, 2026-10-03), on the Level 1 models
    /// tinted per type; "HP x Level" types scale with the player's level.
    /// </summary>
    public class MadForestEnemyPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator At12Minutes_GhoulHoundsSpawn_WithWerewolfStats_AndTheirTint()
        {
            yield return BootIntoGameplay();
            PlayerController.Instance.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
            LevelProgressionManager.Instance.SkipTo(12f * 60f + 1f);
            EnemyController hound = null;
            yield return WaitUntilRealtime(() => (hound = EnemyController.ActiveEnemies.FirstOrDefault(e => e != null && e.Data != null && e.Data.EnemyName == "Ghoul Hound")) != null,
                "a Ghoul Hound (VS Werewolf) at 12:00",
                () => string.Join(", ", EnemyController.ActiveEnemies.Where(e => e != null && e.Data != null).Select(e => e.Data.EnemyName).Distinct()));
            yield return null;
            var health = hound.GetComponent<HealthController>();
            // The shared boot may take a level-up card (Skull O'Maniac raises Curse), so expect the wiki value x Curse.
            float curse = PlayerController.Instance.GetComponent<PlayerStats>().Curse;
            Assert.AreEqual(EnemySpawner.ScaleHealthByCurse(180, curse), health.MaxHealth, $"Werewolf health 180 x Curse {curse}.");
            Assert.AreEqual(14f, hound.GetComponent<TouchDamage>().DamageAmount, "Werewolf damage 14.");
            Assert.AreEqual(130f * 0.03f, hound.Data.MoveSpeed, 1e-4, "Werewolf speed 130 (its data; Curse speeds the live enemy).");
            Assert.AreEqual(0.8f, hound.Data.Knockback, 1e-5);
            Assert.AreEqual(MadForestEnemies.Find("GhoulHound").Tint, hound.TypeTint, "Drawn in its own tint.");
        }

        [UnityTest]
        public IEnumerator HpTimesLevel_EmberBatsAt28Minutes_HaveFiftyTimesThePlayersLevel()
        {
            yield return BootIntoGameplay();
            PlayerController.Instance.GetComponent<HealthController>().IsInvincible = true;
            foreach (var weapon in PlayerController.Instance.GetComponentsInChildren<AutoAttackWeapon>(true)) weapon.enabled = false;
            var experience = PlayerController.Instance.GetComponent<PlayerExperience>();
            LevelProgressionManager.Instance.SkipTo(28f * 60f + 1f);
            EnemyController bat = null;
            yield return WaitUntilRealtime(() => (bat = EnemyController.ActiveEnemies.FirstOrDefault(e => e != null && e.Data != null && e.Data.EnemyName == "Ember Bat")) != null,
                "an Ember Bat (VS Pipeestrello 4) at 28:00");
            yield return null;
            float curse = PlayerController.Instance.GetComponent<PlayerStats>().Curse;
            Assert.AreEqual(EnemySpawner.ScaleHealthByCurse(50 * experience.CurrentLevel, curse), bat.GetComponent<HealthController>().MaxHealth,
                $"HP x Level: 50 x the player's level x Curse {curse}.");
            Assert.AreEqual(10f, bat.GetComponent<TouchDamage>().DamageAmount);
        }
    }
}
