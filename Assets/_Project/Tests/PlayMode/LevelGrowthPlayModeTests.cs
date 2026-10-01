using System.Collections;
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
    /// VS DOT Phase 1 row 2 (2026-10-01). vampire.survivors.wiki/w/Level_up: "at levels 20 and 40 ... the player also
    /// gains +100% Growth ... until they reach the next level"; wiki Growth: "This bonus is reverted at level 21 and 41",
    /// and Growth bonuses stack additively.
    /// </summary>
    public class LevelGrowthPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator Levels20And40_DoubleTheXpGained_UntilTheNextLevel()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var player = PlayerController.Instance;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.enabled = false;
            var levelUp = Object.FindFirstObjectByType<LevelUpManager>();
            levelUp.enabled = false; // levels only; no level-up screens in this test
            var experience = player.GetComponent<PlayerExperience>();
            var stats = player.Stats;
            float growth = stats.ExpBonus;

            void LevelTo(int target)
            {
                while (experience.CurrentLevel < target) experience.AddXP(Mathf.Max(1, Mathf.CeilToInt((experience.XPToNextLevel - experience.CurrentXP) / stats.ExpBonus)));
            }

            LevelTo(19);
            Assert.That(stats.ExpBonus, Is.EqualTo(growth).Within(0.0001f), "No bonus before level 20.");
            LevelTo(20);
            Assert.That(stats.ExpBonus, Is.EqualTo(growth + 1f).Within(0.0001f), "Level 20: +100% Growth.");
            int before = experience.CurrentXP;
            experience.AddXP(10);
            Assert.That(experience.CurrentXP - before, Is.EqualTo(Mathf.RoundToInt(10 * (growth + 1f))), "A 10 XP gem gives twice as much (with the base Growth).");
            LevelTo(21);
            Assert.That(stats.ExpBonus, Is.EqualTo(growth).Within(0.0001f), "Level 21: the bonus is gone.");
            LevelTo(40);
            Assert.That(stats.ExpBonus, Is.EqualTo(growth + 1f).Within(0.0001f), "Level 40: +100% Growth again.");
            LevelTo(41);
            Assert.That(stats.ExpBonus, Is.EqualTo(growth).Within(0.0001f), "Level 41: gone again.");
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
