using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SoulHunter.Core.Persistence;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Learning Comment: RunModeTests verifies the VS run modes (vampire.survivors.wiki/w/Stages "List of Modes",
    /// /w/Arcanas, /w/Limit_Break; owner 2026-09-27): the six toggles survive a save and load, a run copies them into
    /// RunModes, Hurry runs the stage clock twice as fast, Endless starts Level 1 again after Level 10, Limit Break
    /// offers stacking weapon cards once nothing is left, and an Arcana chest gives an Arcana (until three are held).
    /// </summary>
    public class RunModeTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly List<Object> _cleanup = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            RunModes.Reset();
            LimitBreak.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            RunModes.Reset();
            LimitBreak.Reset();
            SoulHunter.Core.Services.GameTime.ResetAll();
            for (int i = _cleanup.Count - 1; i >= 0; i--)
                if (_cleanup[i] != null) Object.DestroyImmediate(_cleanup[i]);
            _cleanup.Clear();
        }

        private T Track<T>(T obj) where T : Object { _cleanup.Add(obj); return obj; }

        // ---------------------------------------------------------------- persistence

        [Test]
        public void ModeToggles_SurviveSaveAndLoad()
        {
            string directory = Path.Combine(Application.temporaryCachePath, nameof(RunModeTests), System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                string path = Path.Combine(directory, "save.json");
                var service = new SaveService(path);
                service.Initialize();
                foreach (var mode in RunModeOptions.All) Assert.That(RunModeOptions.Get(service.CurrentData, mode), Is.False, $"{mode} starts off.");

                RunModeOptions.Set(service.CurrentData, RunMode.Hurry, true);
                RunModeOptions.Set(service.CurrentData, RunMode.Inverse, true);
                RunModeOptions.Set(service.CurrentData, RunMode.LimitBreak, true);
                Assert.That(service.SaveGame(), Is.True);

                var loaded = new SaveService(path);
                loaded.Initialize();
                Assert.That(loaded.CurrentData.ModeHurry, Is.True);
                Assert.That(loaded.CurrentData.ModeHyper, Is.False);
                Assert.That(loaded.CurrentData.ModeInverse, Is.True);
                Assert.That(loaded.CurrentData.ModeEndless, Is.False);
                Assert.That(loaded.CurrentData.ModeArcana, Is.False);
                Assert.That(loaded.CurrentData.ModeLimitBreak, Is.True);
                Assert.That(loaded.CurrentData.SaveVersion, Is.EqualTo(GameData.CurrentVersion), "The schema version is unchanged.");
            }
            finally { Directory.Delete(directory, true); }
        }

        [Test]
        public void OldSave_WithoutModeFields_LoadsEveryModeOff()
        {
            var data = new GameData();
            JsonUtility.FromJsonOverwrite("{\"SaveVersion\": 1, \"Currency\": 50, \"ModeHurry\": true}", data);
            SaveService.Migrate(data);
            Assert.That(data.ModeHurry, Is.True);
            Assert.That(data.ModeHyper || data.ModeInverse || data.ModeEndless || data.ModeArcana || data.ModeLimitBreak, Is.False);
            Assert.That(data.Currency, Is.EqualTo(50));
        }

        [Test]
        public void BeginRun_CopiesTheSavedToggles_AndClearsTheLastRun()
        {
            RunModes.EndlessCycle = 3;
            RunModes.Hyper = true;
            var data = new GameData { ModeHurry = true, ModeArcana = true };

            RunModeOptions.BeginRun(data);

            Assert.That(RunModes.Hurry, Is.True);
            Assert.That(RunModes.Arcana, Is.True);
            Assert.That(RunModes.Hyper, Is.False, "A mode that is off in the save is off for the run.");
            Assert.That(RunModes.EndlessCycle, Is.EqualTo(0), "Every run starts on the first cycle.");

            RunModeOptions.BeginRun(null);
            foreach (var mode in RunModeOptions.All) Assert.That(RunModeOptions.IsActive(mode), Is.False);
        }

        [Test]
        public void EveryMode_HasANameAndADescription()
        {
            Assert.That(RunModeOptions.All.Length, Is.EqualTo(6));
            foreach (var mode in RunModeOptions.All)
            {
                Assert.That(RunModeOptions.Name(mode), Is.Not.Empty);
                Assert.That(RunModeOptions.Description(mode), Does.StartWith(RunModeOptions.Name(mode)));
            }
        }

        // ---------------------------------------------------------------- Hurry / Endless

        private LevelProgressionManager NewProgression() =>
            Track(new GameObject("Progression")).AddComponent<LevelProgressionManager>();

        [Test]
        public void Hurry_RunsTheStageClockTwiceAsFast()
        {
            var progression = NewProgression();
            progression.AdvanceClock(1f);
            Assert.That(progression.StageElapsedTime, Is.EqualTo(1f).Within(1e-4f));

            RunModes.Hurry = true;
            progression.AdvanceClock(1f);
            Assert.That(progression.StageElapsedTime, Is.EqualTo(3f).Within(1e-4f), "One second of game time is two on the stage clock.");
        }

        private static void SurviveThirtyMinutes(LevelProgressionManager progression) =>
            typeof(LevelProgressionManager).GetMethod("CompleteStageAfterSurvival", Private).Invoke(progression, null);

        [Test]
        public void WithoutEndless_ClearingLevel10_WinsTheRun()
        {
            var progression = NewProgression();
            bool won = false;
            progression.OnGameWon += () => won = true;
            progression.StartStage(10);

            // The owner's story: the Soul Core is not survived; only destroying Shadow Kael ends it.
            SurviveThirtyMinutes(progression);
            Assert.That(won, Is.False, "30:00 on Level 10 does not win.");
            Assert.That(progression.IsStageTransitioning, Is.False);

            progression.ReportBossDefeated();
            Assert.That(won, Is.True);
            Assert.That(LevelProgressionManager.StageAfter(10), Is.EqualTo(0));
        }

        [Test]
        public void Endless_ClearingLevel10_StartsLevel1Again_AsANewCycle()
        {
            RunModes.Endless = true;
            var progression = NewProgression();
            bool won = false;
            progression.OnGameWon += () => won = true;
            progression.StartStage(10);

            SurviveThirtyMinutes(progression);
            Assert.That(RunModes.EndlessCycle, Is.EqualTo(0), "30:00 on Level 10 never ends it; Shadow Kael must die.");
            progression.ReportBossDefeated();

            Assert.That(won, Is.False, "Endless: Level 10 does not end the run.");
            Assert.That(RunModes.EndlessCycle, Is.EqualTo(1));
            Assert.That(progression.NextStage, Is.EqualTo(1), "The next stage is Level 1.");
            Assert.That(LevelProgressionManager.StageAfter(10), Is.EqualTo(1));
            Assert.That(LevelProgressionManager.StageAfter(4), Is.EqualTo(5), "Other stages still go on in order.");
            Assert.That(RunModes.EnemyHealthMultiplier(0f), Is.EqualTo(2f).Within(1e-4f), "Cycle 1: enemy health +100%.");

            // The next cycle's Level 10 boss (Shadow Kael) starts cycle 2 instead of winning.
            progression.StartStage(10);
            progression.ReportBossDefeated();
            Assert.That(won, Is.False);
            Assert.That(RunModes.EndlessCycle, Is.EqualTo(2));
        }

        // ---------------------------------------------------------------- Limit Break

        private (LevelUpManager manager, WeaponManager weapons) NewLevelUp(params UpgradeData.UpgradeType[] held)
        {
            var manager = Track(new GameObject("LimitBreakLevelUp")).AddComponent<LevelUpManager>();
            typeof(LevelUpManager).GetMethod("Awake", Private).Invoke(manager, null); // loads the Reroll/Skip/Banish charges
            // WeaponManager spawns weapons under the scene's PlayerController.
            var player = Track(new GameObject("LimitBreakPlayer"));
            player.AddComponent<PlayerController>();
            var weapons = player.AddComponent<WeaponManager>();
            typeof(LevelUpManager).GetField("_weaponManager", Private).SetValue(manager, weapons);
            // No player controller for the level-up manager: its Awake never ran here, so resuming movement would fail.
            typeof(LevelUpManager).GetField("_playerController", Private).SetValue(manager, null);

            var pool = new List<UpgradeData>();
            foreach (var type in held)
            {
                var prefab = Track(new GameObject(type + "_Weapon"));
                prefab.AddComponent<AutoAttackWeapon>();
                string field = type == UpgradeData.UpgradeType.MagicWand ? "_magicWandObject" : "_garlicWeaponObject";
                typeof(WeaponManager).GetField(field, Private).SetValue(weapons, prefab);
                weapons.DefinitionLookup = _ => null; // the mock prefab above, not the catalog's WeaponDefinition
                weapons.GiveWeapon(type, 1);

                // The only upgrade in the pool is level 1 of a weapon already held: nothing is left to offer.
                var upgrade = Track(ScriptableObject.CreateInstance<UpgradeData>());
                upgrade.Type = type;
                upgrade.Level = 1;
                upgrade.UpgradeName = type.ToString();
                pool.Add(upgrade);
            }
            typeof(LevelUpManager).GetField("_allUpgrades", Private).SetValue(manager, pool);
            return (manager, weapons);
        }

        [Test]
        public void LimitBreakOff_NothingLeft_OffersGoldOrChicken()
        {
            // VS wiki (Level_up): with nothing left to upgrade, the level-up offers gold or a Floor Chicken to choose from.
            var (manager, _) = NewLevelUp(UpgradeData.UpgradeType.MagicWand);
            var cards = manager.GetLevelUpChoices(3);
            Assert.That(cards.Count, Is.EqualTo(2));
            Assert.That(cards.TrueForAll(MaxedRewards.IsCard), Is.True);
            Assert.That(cards.Exists(MaxedRewards.IsGoldCard) && cards.Exists(c => !MaxedRewards.IsGoldCard(c)), Is.True, "One gold, one chicken.");
            Assert.That(UpgradeText.Tag(cards[0], 0), Is.EqualTo("REWARD"));
        }

        [Test]
        public void MaxedChickenCard_HealsThirty_AndGoldCard_Pays25()
        {
            var player = Track(new GameObject("MaxedPlayer"));
            var controller = player.AddComponent<PlayerController>();
            var health = player.GetComponent<HealthController>() ?? player.AddComponent<HealthController>();
            health.Initialize(100);
            health.TakeDamage(new DamagePacket(50, Vector3.zero, Vector3.zero, DamageType.Normal));
            int hurt = health.CurrentHealth;
            var cards = MaxedRewards.BuildCards();
            Assert.That(MaxedRewards.TryApply(cards.Find(c => !MaxedRewards.IsGoldCard(c)), controller), Is.True);
            Assert.That(health.CurrentHealth, Is.EqualTo(Mathf.Min(100, hurt + MaxedRewards.HealAmount)));
            Assert.That(MaxedRewards.GoldAmount, Is.EqualTo(25), "VS Big Coin Bag.");
            Assert.That(MaxedRewards.TryApply(Track(ScriptableObject.CreateInstance<UpgradeData>()), controller), Is.False, "Not a reward card.");
        }

        [Test]
        public void LimitBreakOn_NothingLeft_OffersOneCardPerHeldWeapon()
        {
            RunModes.LimitBreak = true;
            var (manager, weapons) = NewLevelUp(UpgradeData.UpgradeType.MagicWand, UpgradeData.UpgradeType.Garlic);

            var cards = manager.GetLevelUpChoices(3);

            Assert.That(cards.Count, Is.EqualTo(2), "One card per held weapon, never two for the same one.");
            var targets = new HashSet<AutoAttackWeapon>();
            foreach (var card in cards)
            {
                Assert.That(LimitBreak.IsCard(card), Is.True);
                Assert.That(UpgradeText.Tag(card, 1), Is.EqualTo("LIMIT BREAK"));
                Assert.That(card.UpgradeName, Does.StartWith("Limit Break: "));
                Assert.That(card.Description, Does.Contain("+10% damage"));
                targets.Add(LimitBreak.WeaponOf(card));
            }
            Assert.That(targets.Count, Is.EqualTo(2));
            foreach (var pair in weapons.GetEquippedWeapons()) Assert.That(targets.Contains(pair.Value), Is.True);
        }

        [Test]
        public void LimitBreakCards_StackOnTheirWeapon_AndDoNotChangeItsLevel()
        {
            RunModes.LimitBreak = true;
            var (manager, weapons) = NewLevelUp(UpgradeData.UpgradeType.MagicWand);
            var wand = weapons.GetEquippedWeapons()[0].Value;
            Assert.That(LimitBreak.DamageMultiplier(wand), Is.EqualTo(1f));

            manager.SelectUpgrade(manager.GetLevelUpChoices(3)[0]);
            Assert.That(LimitBreak.CardsFor(wand), Is.EqualTo(1));
            Assert.That(LimitBreak.DamageMultiplier(wand), Is.EqualTo(1.1f).Within(1e-4f));

            manager.SelectUpgrade(manager.GetLevelUpChoices(3)[0]);
            Assert.That(LimitBreak.DamageMultiplier(wand), Is.EqualTo(1.2f).Within(1e-4f), "Cards stack without end.");
            Assert.That(weapons.GetWeaponLevel(UpgradeData.UpgradeType.MagicWand), Is.EqualTo(1), "The weapon's level is not changed.");

            LimitBreak.Reset();
            Assert.That(LimitBreak.DamageMultiplier(wand), Is.EqualTo(1f), "A new run starts without bonuses.");
        }

        [Test]
        public void LimitBreak_LevelUpShowsCards_AndBanishingOneKeepsTheWeaponInThePool()
        {
            RunModes.LimitBreak = true;
            var (manager, _) = NewLevelUp(UpgradeData.UpgradeType.MagicWand);
            List<UpgradeData> round = null;
            manager.OnLevelUpUIRound += choices => round = choices;

            typeof(LevelUpManager).GetMethod("HandleLevelUp", Private).Invoke(manager, new object[] { 2 });
            Assert.That(round, Is.Not.Null, "A maxed level-up shows Limit Break cards instead of paying out.");
            Assert.That(round.Count, Is.EqualTo(1));
            Assert.That(LimitBreak.IsCard(round[0]), Is.True);

            var banished = round[0];
            Assert.That(manager.Banish(banished), Is.True);
            Assert.That(manager.IsBanished(UpgradeData.UpgradeType.MagicWand), Is.False, "Banishing a Limit Break card banishes no item.");
            Assert.That(LimitBreak.IsCard(round[0]), Is.True, "New cards are dealt.");
        }

        // ---------------------------------------------------------------- Arcana chest

        private ArcanaManager NewArcanaManager(int arcanaCount)
        {
            var manager = Track(new GameObject("Arcana")).AddComponent<ArcanaManager>();
            typeof(ArcanaManager).GetMethod("Awake", Private).Invoke(manager, null);
            var arcanas = new List<ArcanaData>();
            for (int i = 0; i < arcanaCount; i++)
            {
                var arcana = Track(ScriptableObject.CreateInstance<ArcanaData>());
                arcana.CardNumber = (i + 1).ToString();
                arcana.CardName = "Test Arcana " + i;
                arcana.Type = ArcanaData.ArcanaType.IronBlueWill;
                arcanas.Add(arcana);
            }
            manager.SetAvailableArcanas(arcanas);
            return manager;
        }

        [Test]
        public void ArcanaChest_GivesAnUnownedArcana_UntilThreeAreHeld()
        {
            var arcanas = NewArcanaManager(4);
            var chest = Track(new GameObject("Chest")).AddComponent<ChestLogicController>();
            try
            {
                for (int i = 0; i < ArcanaManager.MaxArcanasFromChests; i++)
                {
                    string message = chest.ProcessArcanaChest();
                    Assert.That(message, Does.StartWith("ARCANA CHEST!"));
                    Assert.That(arcanas.ActiveArcanas.Count, Is.EqualTo(i + 1));
                    Assert.That(message, Does.Contain(arcanas.ActiveArcanas[i].CardName));
                }
                Assert.That(new HashSet<ArcanaData>(arcanas.ActiveArcanas).Count, Is.EqualTo(3), "Never the same Arcana twice.");

                // VS: with three Arcanas the Arcana elites drop regular chests.
                string regular = chest.ProcessArcanaChest();
                Assert.That(regular, Does.Not.StartWith("ARCANA CHEST!"));
                Assert.That(arcanas.ActiveArcanas.Count, Is.EqualTo(3));
            }
            finally
            {
                typeof(ArcanaManager).GetMethod("OnDestroy", Private).Invoke(arcanas, null);
            }
        }
    }
}
