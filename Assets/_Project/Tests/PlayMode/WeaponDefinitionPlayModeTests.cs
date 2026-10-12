using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.PlayMode
{
    /// <summary>
    /// Learning Comment: WeaponDefinitionPlayModeTests (2026-09-28) boots the real game and checks that the
    /// WeaponDefinitions match the scene's old WeaponManager wiring (so behaviour is unchanged), and that a real level-up,
    /// drawn with the VS rarity weights and owned-item checks, offers 3-4 valid, distinct items without errors.
    /// </summary>
    public class WeaponDefinitionPlayModeTests : GameplayPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator Definitions_MatchTheSceneWiring()
        {
            yield return BootIntoGameplay();
            var weapons = WeaponManager.Instance;
            Assert.IsNotNull(weapons, "No WeaponManager in Main_Gameplay. " + Errors());
            var catalog = GameContentCatalog.Load();
            Assert.IsNotNull(catalog.WeaponDefinitions, "Run Soul Hunter/Content/Build Weapon Definitions first.");

            foreach (var definition in catalog.WeaponDefinitions)
            {
                if (definition.IsPassive) continue;
                // Weapons added by NewVsItemsBuilder (2026-10-12) have no scene binding: the definition is their wiring.
                var legacy = weapons.LegacyWeaponObject(definition.Type);
                if (legacy != null)
                    Assert.That(definition.WeaponPrefab, Is.SameAs(legacy),
                        $"{definition.Type}: the definition's prefab differs from the scene's WeaponManager wiring.");
                else
                    Assert.That(definition.WeaponPrefab, Is.Not.Null, $"{definition.Type} has neither a scene binding nor a prefab in its definition.");
                Assert.That(weapons.GetWeaponObject(definition.Type), Is.SameAs(definition.WeaponPrefab));
            }
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }

        [UnityTest]
        public IEnumerator RealLevelUp_OffersValidDistinctChoices()
        {
            yield return BootIntoGameplay();
            var spawner = Object.FindFirstObjectByType<SoulHunter.Gameplay.AI.EnemySpawner>();
            if (spawner != null) spawner.enabled = false;
            var player = PlayerController.Instance;
            var weapons = WeaponManager.Instance;
            var levelUps = Object.FindFirstObjectByType<LevelUpManager>();
            Assert.IsNotNull(levelUps, "No LevelUpManager. " + Errors());

            List<UpgradeData> offered = null;
            System.Action<List<UpgradeData>> capture = choices => offered = choices;
            levelUps.OnLevelUpUIRound += capture;
            var experience = player.GetComponent<PlayerExperience>();
            for (int round = 0; round < 5; round++)
            {
                if (!levelUps.IsLevelUpActive)
                {
                    offered = null;
                    // Just enough XP for one level (a Growth bonus can only add a little more).
                    experience.AddXP(Mathf.Max(1, experience.XPToNextLevel - experience.CurrentXP));
                    yield return null;
                }

                Assert.IsNotNull(offered, $"Round {round}: the level-up offered nothing. " + Errors());
                Assert.That(offered.Count, Is.InRange(LevelUpManager.BaseChoiceCount, LevelUpManager.BaseChoiceCount + 1), $"Round {round}: 3 or 4 choices.");
                Assert.That(offered.Select(u => u.Type), Is.Unique, $"Round {round}: an item is offered once.");
                foreach (var choice in offered)
                {
                    int owned = weapons.GetWeaponLevel(choice.Type);
                    Assert.That(choice.Level, Is.EqualTo(owned + 1), $"{choice.Type}: the next level.");
                    Assert.That(choice.Level, Is.LessThanOrEqualTo(weapons.GetMaxWeaponLevel(choice.Type)), $"{choice.Type}: within max level.");
                    Assert.That(weapons.HasSlotFor(choice.Type), Is.True, $"{choice.Type}: a slot is free.");
                    Assert.That(GameContentCatalog.FindWeaponDefinition(choice.Type), Is.Not.Null, $"{choice.Type} has a definition.");
                    Assert.That(UpgradeText.DisplayName(choice), Is.Not.Empty);
                    Assert.That(UpgradeText.Describe(choice, owned), Is.Not.Empty);
                }

                var picked = offered[0];
                int before = weapons.GetWeaponLevel(picked.Type);
                levelUps.SelectUpgrade(picked);
                yield return null;
                Assert.That(weapons.GetWeaponLevel(picked.Type), Is.EqualTo(before + 1), $"Round {round}: picking {picked.Type} levels it.");
            }
            // Close any level-up still queued.
            for (int guard = 0; levelUps.IsLevelUpActive && offered != null && guard < 10; guard++)
            {
                levelUps.SelectUpgrade(offered[0]);
                yield return null;
            }
            levelUps.OnLevelUpUIRound -= capture;
            Assert.That(levelUps.IsLevelUpActive, Is.False);
            Assert.That(Errors(), Is.EqualTo("(no errors logged)"));
        }
    }
}
