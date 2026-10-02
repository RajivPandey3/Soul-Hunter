using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: WeaponDefinitionTests (2026-09-28) checks the one-asset-per-item WeaponDefinitions built by
    /// WeaponDefinitionBuilder (menu Soul Hunter/Content/Build Weapon Definitions; these catalog checks fail until it has
    /// run), that WeaponManager, UpgradeText and UpgradeRarity prefer a definition over the older tables, and the VS
    /// level-up draw (vampire.survivors.wiki/w/Level_up): P(item) = itemRarity / itemPoolWeight, no repeats, and the two
    /// owned-item checks with ownedChance = 1 + 0.3x - 1/totalLuck.
    /// </summary>
    public class WeaponDefinitionTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            WeaponDefinitionLookup.ClearOverride();
            foreach (var created in _created) if (created != null) Object.DestroyImmediate(created);
            _created.Clear();
        }

        private T Track<T>(T created) where T : Object { _created.Add(created); return created; }

        private WeaponDefinition Definition(UpgradeData.UpgradeType type, int rarity = 100, bool passive = false)
        {
            var definition = Track(ScriptableObject.CreateInstance<WeaponDefinition>());
            definition.Type = type;
            definition.Rarity = rarity;
            definition.IsPassive = passive;
            return definition;
        }

        private UpgradeData Upgrade(UpgradeData.UpgradeType type, int level = 1, string name = null, string description = null)
        {
            var upgrade = Track(ScriptableObject.CreateInstance<UpgradeData>());
            upgrade.Type = type;
            upgrade.Level = level;
            upgrade.UpgradeName = name ?? $"{type} Lv.{level}";
            upgrade.Description = description ?? $"{type} upgrade level {level}.";
            return upgrade;
        }

        private static IEnumerable<UpgradeData.UpgradeType> AllTypes => Enum.GetValues(typeof(UpgradeData.UpgradeType)).Cast<UpgradeData.UpgradeType>();

        private static WeaponDefinition[] CatalogDefinitions()
        {
            var catalog = GameContentCatalog.Load();
            Assert.IsNotNull(catalog, "Resources/GameContent is missing.");
            Assert.IsNotNull(catalog.WeaponDefinitions, "GameContent has no WeaponDefinitions: run Soul Hunter/Content/Build Weapon Definitions.");
            return catalog.WeaponDefinitions;
        }

        // ---------- The built definitions ----------

        [Test]
        public void Catalog_HasExactlyOneDefinition_PerUpgradeType()
        {
            var definitions = CatalogDefinitions();
            Assert.That(definitions, Has.None.Null);
            foreach (var type in AllTypes)
            {
                Assert.That(definitions.Count(d => d.Type == type), Is.EqualTo(1), $"{type} needs exactly one WeaponDefinition.");
                Assert.That(WeaponDefinitionLookup.Find(type), Is.SameAs(definitions.First(d => d.Type == type)));
                Assert.That(GameContentCatalog.FindWeaponDefinition(type), Is.SameAs(WeaponDefinitionLookup.Find(type)));
            }
        }

        [Test]
        public void Weapons_HaveTheirPrefab_AndPassivesNone()
        {
            foreach (var definition in CatalogDefinitions())
            {
                Assert.That(definition.IsPassive, Is.EqualTo(!WeaponManager.LegacyIsWeaponType(definition.Type)),
                    $"{definition.Type}: weapon/passive split must match the built-in list.");
                if (definition.IsPassive)
                {
                    Assert.That(definition.WeaponPrefab, Is.Null, $"{definition.Type} is a passive and spawns no prefab.");
                    Assert.That(definition.MaxLevel, Is.GreaterThan(0), $"{definition.Type} max level.");
                }
                else
                {
                    Assert.That(definition.WeaponPrefab, Is.Not.Null, $"{definition.Type} has no weapon prefab.");
                    var weapon = definition.WeaponPrefab.GetComponent<AutoAttackWeapon>();
                    Assert.That(weapon, Is.Not.Null, $"{definition.Type}'s prefab has no AutoAttackWeapon.");
                    Assert.That(definition.MaxLevel, Is.EqualTo(weapon.MaxLevel), $"{definition.Type}: max level comes from the prefab.");
                }
            }
        }

        [Test]
        public void Definitions_HaveVsRarity_TextAndLevels()
        {
            foreach (var definition in CatalogDefinitions())
            {
                Assert.That(definition.Rarity, Is.GreaterThan(0), $"{definition.Type} rarity.");
                Assert.That(UpgradeRarity.VsRarity.ContainsKey(definition.Type), Is.True, $"{definition.Type} has no VS rarity in the table.");
                Assert.That(definition.Rarity, Is.EqualTo(UpgradeRarity.VsRarity[definition.Type].Rarity), $"{definition.Type}: rarity is the VS wiki value.");
                Assert.That(definition.DisplayName, Is.Not.Null.And.Not.Empty, $"{definition.Type} display name.");
                Assert.That(definition.DisplayName, Does.Not.Contain("Lv"), $"{definition.Type} display name has no level suffix.");
                Assert.That(definition.Description, Is.Not.Null.And.Not.Empty, $"{definition.Type} description.");

                Assert.That(definition.Levels, Is.Not.Null.And.Not.Empty, $"{definition.Type} lists its UpgradeData levels.");
                Assert.That(definition.Levels.All(u => u != null && u.Type == definition.Type), Is.True, $"{definition.Type} levels are its own.");
                Assert.That(definition.Levels.Select(u => u.Level), Is.Ordered.Ascending, $"{definition.Type} levels in order.");
                Assert.That(definition.Levels.Select(u => u.Level), Is.Unique, $"{definition.Type} one asset per level.");
                Assert.That(definition.GetLevel(1), Is.Not.Null, $"{definition.Type} has a level 1.");
                if (definition.Levels.Any(u => u.Icon != null))
                    Assert.That(definition.Icon, Is.Not.Null, $"{definition.Type} icon.");
            }
        }

        [Test]
        public void VsRarityTable_MatchesTheWikiValues()
        {
            // Spot checks of values fetched from the VS wiki item infoboxes on 2026-09-28.
            Assert.That(UpgradeRarity.VsWeight(UpgradeData.UpgradeType.MagicWand), Is.EqualTo(100));
            Assert.That(UpgradeRarity.VsWeight(UpgradeData.UpgradeType.Garlic), Is.EqualTo(70));
            Assert.That(UpgradeRarity.VsWeight(UpgradeData.UpgradeType.Bible), Is.EqualTo(80));
            Assert.That(UpgradeRarity.VsWeight(UpgradeData.UpgradeType.Bone), Is.EqualTo(1));
            Assert.That(UpgradeRarity.VsWeight(UpgradeData.UpgradeType.Gun), Is.EqualTo(50));
            Assert.That(UpgradeRarity.VsWeight(UpgradeData.UpgradeType.PlayerSpeed), Is.EqualTo(50));
            Assert.That(UpgradeRarity.VsWeight(UpgradeData.UpgradeType.MaxHealth), Is.EqualTo(90));
            Assert.That(UpgradeRarity.VsWeight(UpgradeData.UpgradeType.MetaglioLeft), Is.EqualTo(10));
            foreach (var type in AllTypes)
                Assert.That(UpgradeRarity.VsRarity.ContainsKey(type), Is.True, $"{type} is missing from the VS rarity table.");
        }

        // ---------- Definitions win over the old tables ----------

        [Test]
        public void WeaponManager_PrefersTheDefinition_ThenTheOldFields()
        {
            var go = Track(new GameObject("WeaponManager"));
            var manager = go.AddComponent<WeaponManager>();
            var oldPrefab = Track(new GameObject("OldWand"));
            var newPrefab = Track(new GameObject("NewWand"));
            typeof(WeaponManager).GetField("_magicWandObject", Private).SetValue(manager, oldPrefab);

            var definition = Definition(UpgradeData.UpgradeType.MagicWand);
            definition.WeaponPrefab = newPrefab;
            definition.MaxLevel = 3;
            manager.DefinitionLookup = type => type == UpgradeData.UpgradeType.MagicWand ? definition : null;

            Assert.That(manager.GetWeaponObject(UpgradeData.UpgradeType.MagicWand), Is.SameAs(newPrefab));
            Assert.That(manager.GetMaxWeaponLevel(UpgradeData.UpgradeType.MagicWand), Is.EqualTo(3));

            manager.DefinitionLookup = _ => null;
            Assert.That(manager.GetWeaponObject(UpgradeData.UpgradeType.MagicWand), Is.SameAs(oldPrefab), "Without a definition the old field is used.");
            Assert.That(manager.GetMaxWeaponLevel(UpgradeData.UpgradeType.MetaglioLeft), Is.EqualTo(9), "Old rule: stage passives cap at 9.");
        }

        [Test]
        public void IsWeaponType_PrefersTheDefinition_ThenTheBuiltInList()
        {
            WeaponDefinitionLookup.Override(new[] { Definition(UpgradeData.UpgradeType.Spinach, passive: false) });
            Assert.That(WeaponManager.IsWeaponType(UpgradeData.UpgradeType.Spinach), Is.True, "The definition says weapon.");

            WeaponDefinitionLookup.Override(new WeaponDefinition[0]);
            Assert.That(WeaponManager.IsWeaponType(UpgradeData.UpgradeType.Spinach), Is.False, "Built-in list: Spinach is a passive.");
            Assert.That(WeaponManager.IsWeaponType(UpgradeData.UpgradeType.Whip), Is.True);
        }

        [Test]
        public void UpgradeText_PrefersTheDefinition_ButKeepsHandWrittenAssetText()
        {
            var definition = Definition(UpgradeData.UpgradeType.Garlic);
            definition.DisplayName = "Soul Aura";
            definition.Description = "Hurts what comes close.";
            WeaponDefinitionLookup.Override(new[] { definition });
            var levelOne = Upgrade(UpgradeData.UpgradeType.Garlic, 1);
            var levelTwo = Upgrade(UpgradeData.UpgradeType.Garlic, 2);

            Assert.That(UpgradeText.DisplayName(levelOne), Is.EqualTo("Soul Aura"));
            Assert.That(UpgradeText.Describe(levelOne, 0), Is.EqualTo("Hurts what comes close."));
            Assert.That(UpgradeText.Describe(levelTwo, 1), Is.EqualTo("+2 damage, +40% area."), "What VS Garlic level 2 adds (Bug MIS v2 C14).");
            Assert.That(UpgradeText.Describe(Upgrade(UpgradeData.UpgradeType.Garlic, 3, description: "Written by hand."), 2),
                Is.EqualTo("Written by hand."), "A hand-written asset description is kept.");

            WeaponDefinitionLookup.Override(new WeaponDefinition[0]);
            Assert.That(UpgradeText.DisplayName(levelOne), Is.EqualTo("Garlic"), "Without a definition: the asset name.");
            Assert.That(UpgradeText.Describe(levelOne, 0), Is.EqualTo(UpgradeText.FallbackDescription(UpgradeData.UpgradeType.Garlic)));
        }

        [Test]
        public void Rarity_PrefersTheDefinition_ThenTheVsTable()
        {
            WeaponDefinitionLookup.Override(new[] { Definition(UpgradeData.UpgradeType.Whip, rarity: 7) });
            Assert.That(UpgradeRarity.GetWeight(UpgradeData.UpgradeType.Whip), Is.EqualTo(7));
            Assert.That(UpgradeRarity.GetWeight(UpgradeData.UpgradeType.Garlic), Is.EqualTo(70), "No definition: VS table.");
        }

        // ---------- VS level-up draw ----------

        private static Func<float> Seeded(int seed)
        {
            var random = new System.Random(seed);
            return () => (float)random.NextDouble();
        }

        private static Func<float> Sequence(params float[] rolls)
        {
            int i = 0;
            return () => i < rolls.Length ? rolls[i++] : 0.999f;
        }

        [Test]
        public void Draw_PicksEachItem_InProportionToItsRarity()
        {
            // VS wiki (Level_up): P(item) = itemRarity / itemPoolWeight.
            WeaponDefinitionLookup.Override(new[]
            {
                Definition(UpgradeData.UpgradeType.MagicWand, 100),
                Definition(UpgradeData.UpgradeType.Whip, 50),
                Definition(UpgradeData.UpgradeType.Bone, 10),
            });
            var pool = new List<UpgradeData>
            {
                Upgrade(UpgradeData.UpgradeType.MagicWand), Upgrade(UpgradeData.UpgradeType.Whip), Upgrade(UpgradeData.UpgradeType.Bone)
            };
            var roll = Seeded(12345);
            var counts = new Dictionary<UpgradeData.UpgradeType, int>();
            const int draws = 40000;
            for (int i = 0; i < draws; i++)
            {
                var type = LevelUpManager.DrawChoices(pool, 1, null, 0f, roll)[0].Type;
                counts[type] = counts.TryGetValue(type, out int n) ? n + 1 : 1;
            }

            Assert.That(counts[UpgradeData.UpgradeType.MagicWand] / (float)draws, Is.EqualTo(100f / 160f).Within(0.015f));
            Assert.That(counts[UpgradeData.UpgradeType.Whip] / (float)draws, Is.EqualTo(50f / 160f).Within(0.015f));
            Assert.That(counts[UpgradeData.UpgradeType.Bone] / (float)draws, Is.EqualTo(10f / 160f).Within(0.01f));
        }

        [Test]
        public void Draw_NeverRepeatsAnItem_AndStopsWhenThePoolRunsOut()
        {
            var pool = new List<UpgradeData>
            {
                Upgrade(UpgradeData.UpgradeType.MagicWand), Upgrade(UpgradeData.UpgradeType.Whip),
                Upgrade(UpgradeData.UpgradeType.Garlic), Upgrade(UpgradeData.UpgradeType.Axe), Upgrade(UpgradeData.UpgradeType.Spinach)
            };
            var roll = Seeded(7);
            for (int i = 0; i < 500; i++)
            {
                var choices = LevelUpManager.DrawChoices(pool, 4, t => t == UpgradeData.UpgradeType.Whip, 0.6f, roll);
                Assert.That(choices.Count, Is.EqualTo(4));
                Assert.That(choices.Select(c => c.Type), Is.Unique);
            }
            Assert.That(LevelUpManager.DrawChoices(pool.Take(2).ToList(), 4, null, 0f, roll).Count, Is.EqualTo(2));
        }

        [TestCase(1f, 1, 0.3f)]   // odd level: x = 1 -> 1 + 0.3 - 1 = 0.3
        [TestCase(1f, 2, 0.6f)]   // even level: x = 2 -> 0.6
        [TestCase(2f, 3, 0.8f)]   // Luck 2: 1 + 0.3 - 0.5
        [TestCase(2f, 4, 1f)]     // 1 + 0.6 - 0.5 = 1.1, a certainty
        [TestCase(0.5f, 1, 0f)]   // 1 + 0.3 - 2 < 0: never
        public void OwnedChance_FollowsTheWikiFormula(float luck, int level, float expected)
        {
            Assert.That(LevelUpManager.OwnedChance(luck, level), Is.EqualTo(expected).Within(1e-4f));
        }

        [Test]
        public void OwnedChecks_OfferAnOwnedItem_AndASecondSamePick_IsReplacedFromThePool()
        {
            var owned = Upgrade(UpgradeData.UpgradeType.Whip, 2);
            var pool = new List<UpgradeData> { Upgrade(UpgradeData.UpgradeType.MagicWand), owned, Upgrade(UpgradeData.UpgradeType.Garlic) };

            // Check 1 passes (0.0) and picks the only owned item; check 2 passes and picks it again, so a new item is
            // pulled from the pool (roll 0.0 -> the first remaining item, Magic Wand); the last choice fills from the pool.
            var choices = LevelUpManager.DrawChoices(pool, 3, t => t == UpgradeData.UpgradeType.Whip, 0.3f, Sequence(0f, 0f, 0f, 0f, 0f, 0f));
            Assert.That(choices.Select(c => c.Type), Is.EqualTo(new[] { UpgradeData.UpgradeType.Whip, UpgradeData.UpgradeType.MagicWand, UpgradeData.UpgradeType.Garlic }));

            // Both checks fail (roll 0.5 >= 0.3): only the weighted pool draw; roll 0 picks the first item.
            choices = LevelUpManager.DrawChoices(pool, 1, t => t == UpgradeData.UpgradeType.Whip, 0.3f, Sequence(0.5f, 0.5f, 0f));
            Assert.That(choices[0].Type, Is.EqualTo(UpgradeData.UpgradeType.MagicWand));
        }

        [Test]
        public void OwnedChecks_MakeOwnedItemsMuchLikelier()
        {
            // One owned item of rarity 1 among nine rarity-100 items. Without the checks it shows up ~1/901 per pick;
            // with ownedChance 0.6 (Luck 1, even level) at least one of the two checks succeeds 1 - 0.4^2 = 84% of the time.
            var types = new[]
            {
                UpgradeData.UpgradeType.MagicWand, UpgradeData.UpgradeType.Whip, UpgradeData.UpgradeType.Garlic, UpgradeData.UpgradeType.Axe,
                UpgradeData.UpgradeType.Cross, UpgradeData.UpgradeType.Knife, UpgradeData.UpgradeType.Spinach, UpgradeData.UpgradeType.Clover,
                UpgradeData.UpgradeType.Bracer
            };
            WeaponDefinitionLookup.Override(types.Select(t => Definition(t, 100)).Append(Definition(UpgradeData.UpgradeType.Bone, 1)).ToList());
            var pool = types.Select(t => Upgrade(t)).Append(Upgrade(UpgradeData.UpgradeType.Bone, 2)).ToList();
            var roll = Seeded(99);

            int offered = 0, offeredWithout = 0;
            const int rounds = 20000;
            for (int i = 0; i < rounds; i++)
            {
                if (LevelUpManager.DrawChoices(pool, 3, t => t == UpgradeData.UpgradeType.Bone, 0.6f, roll).Any(c => c.Type == UpgradeData.UpgradeType.Bone)) offered++;
                if (LevelUpManager.DrawChoices(pool, 3, t => t == UpgradeData.UpgradeType.Bone, 0f, roll).Any(c => c.Type == UpgradeData.UpgradeType.Bone)) offeredWithout++;
            }
            Assert.That(offered / (float)rounds, Is.EqualTo(0.84f).Within(0.02f));
            Assert.That(offeredWithout / (float)rounds, Is.LessThan(0.01f));
        }

        [Test]
        public void ValidUpgrades_OfferEachItemOnce_EvenWithDuplicateAssets()
        {
            var go = Track(new GameObject("LevelUp"));
            var weapons = go.AddComponent<WeaponManager>();
            var manager = go.AddComponent<LevelUpManager>();
            typeof(LevelUpManager).GetField("_weaponManager", Private).SetValue(manager, weapons);
            var pool = new List<UpgradeData>
            {
                Upgrade(UpgradeData.UpgradeType.Spinach), Upgrade(UpgradeData.UpgradeType.Spinach), Upgrade(UpgradeData.UpgradeType.Clover)
            };
            typeof(LevelUpManager).GetField("_allUpgrades", Private).SetValue(manager, pool);

            var valid = manager.GetValidUpgrades();
            Assert.That(valid.Select(u => u.Type), Is.EquivalentTo(new[] { UpgradeData.UpgradeType.Spinach, UpgradeData.UpgradeType.Clover }));
            Assert.That(manager.GetRandomValidUpgrades(3).Select(u => u.Type), Is.Unique);
        }
    }
}
