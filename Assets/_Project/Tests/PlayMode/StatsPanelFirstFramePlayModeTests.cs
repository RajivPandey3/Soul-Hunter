using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Owner, 2026-10-01 ("level up stats panel bhi theek karo"): on the first frames of a level-up the stats panel was
    /// fitted to the cards' old positions and showed only 2 of its 5 rows. It must fit all its rows as soon as the
    /// screen opens.
    /// </summary>
    public class StatsPanelFirstFramePlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator TheStatsPanel_FitsEveryRow_TheMomentTheLevelUpOpens()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var player = PlayerController.Instance;
            foreach (var weapon in player.GetComponentsInChildren<AutoAttackWeapon>()) weapon.enabled = false;
            var levelUp = Object.FindFirstObjectByType<LevelUpManager>();
            var experience = player.GetComponent<PlayerExperience>();

            for (int round = 0; round < 2; round++)
            {
                // Opens synchronously: measured in the same frame, before any LateUpdate could correct it.
                experience.AddXP(experience.XPToNextLevel - experience.CurrentXP);
                Assert.That(levelUp.IsLevelUpActive, Is.True, "The level-up opened.");
                var panel = Object.FindFirstObjectByType<StatsPanel>();
                Assert.That(panel, Is.Not.Null, "The level-up screen has its stats panel.");
                int perColumn = Mathf.CeilToInt(panel.Rows.Length / 4f);
                float need = perColumn * panel.FontSize * 1.25f; // the rows' height inside the columns (92% of the panel)
                float have = ((RectTransform)panel.transform).rect.height * 0.92f;
                Assert.That(have, Is.GreaterThanOrEqualTo(need - 1f),
                    $"Round {round + 1}: the panel ({have:0} high for its rows) holds all {perColumn} rows of {panel.FontSize:0} pt ({need:0}).");
                levelUp.SelectUpgrade(levelUp.GetRandomValidUpgrades(1)[0]);
                yield return null;
                yield return null;
            }
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
