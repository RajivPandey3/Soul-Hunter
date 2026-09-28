using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;
using UpgradeType = SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Found 2026-09-28: Main_Gameplay keeps Weapon_Manager under MANAGERS, so passives looked for PlayerStats in their
    /// parents, found none and changed nothing in the real game. EditMode tests missed it because they put the manager
    /// on the player. Values per each item's vampire.survivors.wiki page.
    /// </summary>
    public class PassivePlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator Passives_ChangeThePlayersStats_InTheRealGame()
        {
            yield return BootIntoGameplay();
            Object.FindFirstObjectByType<EnemySpawner>().enabled = false;
            var player = PlayerController.Instance;
            var stats = player.GetComponent<PlayerStats>();
            var health = player.GetComponent<HealthController>();
            var weapons = WeaponManager.Instance;
            Assert.That(weapons, Is.Not.Null);

            float might = stats.Might;
            int armor = stats.Armor;
            int maxHealth = health.MaxHealth;
            var created = new[] { Upgrade(UpgradeType.Spinach), Upgrade(UpgradeType.Armor), Upgrade(UpgradeType.MaxHealth) };
            try
            {
                foreach (var upgrade in created) weapons.ApplyUpgrade(upgrade);
                Assert.That(stats.Might, Is.EqualTo(might + 0.1f).Within(1e-4f), "Spinach: +10% Might.");
                Assert.That(stats.Armor, Is.EqualTo(armor + 1), "Armor: +1 Armor.");
                Assert.That(health.MaxHealth, Is.EqualTo(Mathf.RoundToInt(maxHealth * 1.2f)).Within(1), "Hollow Heart: Max Health x1.2.");
            }
            finally
            {
                foreach (var upgrade in created) Object.Destroy(upgrade);
            }
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        private static UpgradeData Upgrade(UpgradeType type)
        {
            var upgrade = ScriptableObject.CreateInstance<UpgradeData>();
            upgrade.Type = type;
            upgrade.Level = 1;
            return upgrade;
        }
    }
}
