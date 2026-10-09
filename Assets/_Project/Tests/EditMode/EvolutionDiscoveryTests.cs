using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using SoulHunter.Core.Persistence;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Release items P3-01 / P3-06 (2026-10-09): evolutions discovered on the save (wiki Grim_Grimoire: "discovered on their
    /// save file"), the level-up hint for them (wiki Level_up, 0.3.2 and 1.4.200: weapon and passive cards), and the rows
    /// of the pause screen's evolution list (wiki Grim_Grimoire: held, made this run, faded when no longer possible).
    /// </summary>
    public class EvolutionDiscoveryTests
    {
        private readonly List<Object> _made = new List<Object>();

        [TearDown]
        public void Clean()
        {
            foreach (var o in _made) if (o != null) Object.DestroyImmediate(o);
            _made.Clear();
        }

        private EvolutionRecipe Evolution(string id, UpgradeData.UpgradeType weapon, UpgradeData.UpgradeType passive,
            params UpgradeData.UpgradeType[] more)
        {
            var data = ScriptableObject.CreateInstance<WeaponEvolutionData>();
            _made.Add(data);
            data.name = id;
            data.EvolvedName = id;
            data.BaseWeapon = weapon;
            data.RequiredPassive = passive;
            var extras = new List<PassiveEvolutionRequirement>();
            foreach (var type in more) extras.Add(new PassiveEvolutionRequirement { Type = type, MinimumLevel = 1 });
            data.AdditionalPassives = extras.ToArray();
            return EvolutionRecipe.From(data);
        }

        [Test]
        public void MarkDiscovered_AddsOnce_AndIsDiscoveredReadsIt()
        {
            var data = new GameData();
            Assert.That(EvolutionDiscovery.IsDiscovered(data, "Evo_BloodyTear"), Is.False);
            Assert.That(EvolutionDiscovery.MarkDiscovered(data, "Evo_BloodyTear"), Is.True, "New: the caller saves.");
            Assert.That(EvolutionDiscovery.MarkDiscovered(data, "Evo_BloodyTear"), Is.False, "Already known: nothing to save.");
            Assert.That(data.DiscoveredEvolutions, Is.EqualTo(new[] { "Evo_BloodyTear" }));
            Assert.That(EvolutionDiscovery.IsDiscovered(data, "Evo_BloodyTear"), Is.True);
            Assert.That(EvolutionDiscovery.MarkDiscovered(data, ""), Is.False);
            Assert.That(EvolutionDiscovery.MarkDiscovered(null, "Evo_X"), Is.False);
        }

        [Test]
        public void SaveRepair_GivesAnOlderSaveAnEmptyList_AndDropsRepeats()
        {
            var old = new GameData { SaveVersion = GameData.CurrentVersion, DiscoveredEvolutions = null };
            SaveService.Migrate(old);
            Assert.That(old.DiscoveredEvolutions, Is.Not.Null.And.Empty, "A save written before the list loads it empty.");

            var messy = new GameData { SaveVersion = GameData.CurrentVersion };
            messy.DiscoveredEvolutions.AddRange(new[] { "Evo_SoulEater", "", null, "Evo_SoulEater", "Evo_HolyWand" });
            SaveService.Migrate(messy);
            Assert.That(messy.DiscoveredEvolutions, Is.EqualTo(new[] { "Evo_SoulEater", "Evo_HolyWand" }));

            // A save file without the field at all (JsonUtility, as SaveService loads).
            var loaded = JsonUtility.FromJson<GameData>("{\"SaveVersion\":2,\"Currency\":5}");
            SaveService.Migrate(loaded);
            Assert.That(loaded.DiscoveredEvolutions, Is.Not.Null.And.Empty);
            Assert.That(loaded.Currency, Is.EqualTo(5), "Nothing else changes.");
        }

        [Test]
        public void Hints_OnlyForDiscoveredRecipes_OnWeaponAndPassiveCards_NotOnceMade()
        {
            var bloodyTear = Evolution("Evo_BloodyTear", UpgradeData.UpgradeType.Whip, UpgradeData.UpgradeType.MaxHealth);
            var soulEater = Evolution("Evo_SoulEater", UpgradeData.UpgradeType.Garlic, UpgradeData.UpgradeType.Pummarola);
            var recipes = new[] { bloodyTear, soulEater };
            var discovered = new HashSet<string>();
            var made = new HashSet<string>();

            Assert.That(EvolutionRecipes.HintsFor(UpgradeData.UpgradeType.Whip, recipes, discovered.Contains, made.Contains), Is.Empty,
                "Nothing is hinted before it is discovered.");
            discovered.Add("Evo_BloodyTear");
            Assert.That(EvolutionRecipes.HintsFor(UpgradeData.UpgradeType.Whip, recipes, discovered.Contains, made.Contains), Is.EqualTo(new[] { bloodyTear }),
                "The weapon card hints it.");
            Assert.That(EvolutionRecipes.HintsFor(UpgradeData.UpgradeType.MaxHealth, recipes, discovered.Contains, made.Contains), Is.EqualTo(new[] { bloodyTear }),
                "The passive card hints it too (wiki Level_up 1.4.200).");
            Assert.That(EvolutionRecipes.HintsFor(UpgradeData.UpgradeType.Garlic, recipes, discovered.Contains, made.Contains), Is.Empty,
                "An undiscovered recipe stays hidden.");
            made.Add("Evo_BloodyTear");
            Assert.That(EvolutionRecipes.HintsFor(UpgradeData.UpgradeType.MaxHealth, recipes, discovered.Contains, made.Contains), Is.Empty,
                "Once made in the run there is nothing left to plan.");
        }

        [Test]
        public void HintText_NamesTheOtherItems_AndTheResult()
        {
            var bloodyTear = Evolution("Evo_BloodyTear", UpgradeData.UpgradeType.Whip, UpgradeData.UpgradeType.MaxHealth);
            string weaponCard = EvolutionRecipes.HintText(UpgradeData.UpgradeType.Whip, new[] { bloodyTear });
            Assert.That(weaponCard, Does.StartWith("Evolves with ").And.EndWith(" into Bloody Tear"));
            Assert.That(weaponCard, Does.Contain(UpgradeText.NameOf(UpgradeData.UpgradeType.MaxHealth)));
            string passiveCard = EvolutionRecipes.HintText(UpgradeData.UpgradeType.MaxHealth, new[] { bloodyTear, bloodyTear });
            Assert.That(passiveCard, Does.Contain(UpgradeText.NameOf(UpgradeData.UpgradeType.Whip)).And.EndWith("(+1 more)"));
            Assert.That(EvolutionRecipes.JoinNames(new[] { "A", "B", "C" }), Is.EqualTo("A, B and C"));
        }

        [Test]
        public void CrimsonShroud_ListsAllThreeItems()
        {
            var asset = AssetDatabase.LoadAssetAtPath<WeaponEvolutionData>("Assets/Resources/Metaglio/CrimsonShroudEvolution.asset");
            Assert.That(asset, Is.Not.Null);
            var recipe = EvolutionRecipe.From(asset);
            Assert.That(recipe.Items, Is.EqualTo(new[] { UpgradeData.UpgradeType.Laurel, UpgradeData.UpgradeType.MetaglioLeft, UpgradeData.UpgradeType.MetaglioRight }));
            Assert.That(EvolutionRecipes.Counterparts(recipe, UpgradeData.UpgradeType.Laurel),
                Is.EqualTo(new[] { UpgradeData.UpgradeType.MetaglioLeft, UpgradeData.UpgradeType.MetaglioRight }));
            Assert.That(recipe.Name, Is.EqualTo("Crimson Shroud"));
        }

        [Test]
        public void RowStates_FollowTheGrimoireRules()
        {
            var recipe = Evolution("Evo_BloodyTear", UpgradeData.UpgradeType.Whip, UpgradeData.UpgradeType.MaxHealth);
            bool Holds(UpgradeData.UpgradeType t) => t == UpgradeData.UpgradeType.Whip;
            Assert.That(EvolutionRecipes.StateOf(recipe, false, false, Holds, _ => true, false), Is.EqualTo(EvolutionRecipes.RowState.Unknown));
            Assert.That(EvolutionRecipes.StateOf(recipe, true, true, Holds, _ => true, true), Is.EqualTo(EvolutionRecipes.RowState.Made));
            Assert.That(EvolutionRecipes.StateOf(recipe, true, false, Holds, _ => false, false), Is.EqualTo(EvolutionRecipes.RowState.Open),
                "Slots not full: every recipe stays visible.");
            Assert.That(EvolutionRecipes.StateOf(recipe, true, false, Holds, _ => true, true), Is.EqualTo(EvolutionRecipes.RowState.Open),
                "Full slots, one item held, the rest still gainable: visible.");
            Assert.That(EvolutionRecipes.StateOf(recipe, true, false, Holds, _ => false, true), Is.EqualTo(EvolutionRecipes.RowState.Faded),
                "Full slots and the rest can no longer be gained: faded.");
            Assert.That(EvolutionRecipes.StateOf(recipe, true, false, _ => false, _ => true, true), Is.EqualTo(EvolutionRecipes.RowState.Faded),
                "Full slots and nothing held: faded.");
        }

        [Test]
        public void OnlyTheChestRecordsDiscoveries_NotTheBotsDebugStart()
        {
            // The -sh-autoplay-evolve start calls WeaponManager.TryEvolveWeapon directly; discovery is written by the chest.
            string chest = File.ReadAllText("Assets/_Project/Core/Game/ChestLogicController.cs");
            string manager = File.ReadAllText("Assets/_Project/Gameplay/Combat/WeaponManager.cs");
            string bot = File.ReadAllText("Assets/_Project/Core/Game/AutoplayBot.cs");
            Assert.That(chest, Does.Contain("EvolutionDiscovery.MarkDiscovered"));
            Assert.That(manager, Does.Not.Contain("EvolutionDiscovery"), "TryEvolveWeapon itself never writes the save.");
            Assert.That(bot, Does.Not.Contain("EvolutionDiscovery"));
        }
    }
}
