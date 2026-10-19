using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SoulHunter.Core.Persistence;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.Weapons;
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
            Assert.That(RunModeOptions.All.Length, Is.EqualTo(8)); // six modes and the two Randomizers (P3-05)
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
            typeof(LevelUpManager).GetMethod("Awake", Private).Invoke(manager, null);
            manager.AddCharges(2, 2, 2); // VS: charges come from the PowerUps (rank 1 each here)
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
        public void MaxedRewardCards_AreMadeOnce_AndReused_NotLeakedPerLevelUp()
        {
            // Bug MIS CORE-10 (2026-10-19): every maxed level-up, reroll and banish created 2-4 UpgradeData objects that
            // were never destroyed. The cards are now made once per kind and reused.
            var first = MaxedRewards.BuildCards(true);
            int objects = Resources.FindObjectsOfTypeAll<UpgradeData>().Length;
            for (int i = 0; i < 20; i++) MaxedRewards.BuildCards(i % 2 == 0);
            var again = MaxedRewards.BuildCards(true);
            Assert.That(Resources.FindObjectsOfTypeAll<UpgradeData>().Length, Is.EqualTo(objects), "No new card objects per level-up.");
            for (int i = 0; i < first.Count; i++) Assert.That(again[i], Is.SameAs(first[i]));
            Assert.That(again.TrueForAll(MaxedRewards.IsCard), Is.True);
            Assert.That(MaxedRewards.TryGetKind(again[3], out var kind) && kind == MaxedRewards.Kind.AlwaysChicken, Is.True);

            // A card destroyed elsewhere is made again, and the new one is a card.
            Object.DestroyImmediate(again[0]);
            var remade = MaxedRewards.BuildCards();
            Assert.That(remade[0] != null, Is.True);
            Assert.That(MaxedRewards.IsGoldCard(remade[0]), Is.True);
            Assert.That(remade[0].UpgradeName, Is.EqualTo(MaxedRewards.GoldCardName));
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
                Assert.That(card.Description, Is.EqualTo(LimitBreak.Describe(LimitBreak.WeaponOf(card), LimitBreak.StatOf(card)) + " beyond max level."), "Each card names its stat (VS Limit Break table).");
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
            Assert.That(LimitBreak.CardsFor(wand), Is.EqualTo(0));

            var first = manager.GetLevelUpChoices(3)[0];
            var stat = LimitBreak.StatOf(first);
            manager.SelectUpgrade(first);
            Assert.That(LimitBreak.CardsFor(wand), Is.EqualTo(1));
            Assert.That(LimitBreak.Total(wand, stat), Is.EqualTo(LimitBreak.StepFor(wand, stat)).Within(1e-4f), $"{stat} rose by its step.");

            manager.SelectUpgrade(manager.GetLevelUpChoices(3)[0]);
            Assert.That(LimitBreak.CardsFor(wand), Is.EqualTo(2), "Cards stack.");
            Assert.That(weapons.GetWeaponLevel(UpgradeData.UpgradeType.MagicWand), Is.EqualTo(1), "The weapon's level is not changed.");

            LimitBreak.Reset();
            Assert.That(LimitBreak.CardsFor(wand), Is.EqualTo(0), "A new run starts without bonuses.");
            foreach (LimitBreak.Stat s in System.Enum.GetValues(typeof(LimitBreak.Stat))) Assert.That(LimitBreak.Total(wand, s), Is.EqualTo(0f));
        }

        /// <summary>
        /// VS Limit Break "List of upgrades" (2026-10-01): stats drawn by rarity among those the weapon uses, each with its
        /// step and cap; Base Damage has no cap.
        /// </summary>
        [Test]
        public void LimitBreakStats_FollowTheWikiTable_RarityAndCaps()
        {
            RunModes.LimitBreak = true;
            var (_, weapons) = NewLevelUp(UpgradeData.UpgradeType.MagicWand);
            var wand = weapons.GetEquippedWeapons()[0].Value;
            // The helper's weapon is a plain AutoAttackWeapon: no pierce, duration, crit or chance effect.
            Assert.That(LimitBreak.CanRaise(wand, LimitBreak.Stat.Pierce), Is.False, "No pierce.");
            Assert.That(LimitBreak.CanRaise(wand, LimitBreak.Stat.Duration), Is.False, "No duration.");
            Assert.That(LimitBreak.CanRaise(wand, LimitBreak.Stat.CritChance), Is.False, "Cannot crit.");
            Assert.That(LimitBreak.CanRaise(wand, LimitBreak.Stat.Chance), Is.False, "No chance effect.");

            // Rarities: BaseDamage 10, Area 10, Speed 10, Amount 1 -> pool 31.
            Assert.That(LimitBreak.DrawStat(wand, 0f, out var s0) && s0 == LimitBreak.Stat.BaseDamage, Is.True);
            Assert.That(LimitBreak.DrawStat(wand, 10.5f / 31f, out var s1) && s1 == LimitBreak.Stat.Area, Is.True);
            Assert.That(LimitBreak.DrawStat(wand, 20.5f / 31f, out var s2) && s2 == LimitBreak.Stat.Speed, Is.True);
            Assert.That(LimitBreak.DrawStat(wand, 30.5f / 31f, out var s3) && s3 == LimitBreak.Stat.Amount, Is.True);

            for (int i = 0; i < 30; i++) LimitBreak.Apply(wand, LimitBreak.Stat.Amount);
            Assert.That(LimitBreak.AmountBonus(wand), Is.EqualTo(20), "Amount stops at 20.");
            Assert.That(LimitBreak.CanRaise(wand, LimitBreak.Stat.Amount), Is.False);
            for (int i = 0; i < 400; i++) LimitBreak.Apply(wand, LimitBreak.Stat.Area);
            Assert.That(LimitBreak.AreaBonus(wand), Is.EqualTo(9f).Within(1e-3f), "Area stops at 1000% (its 100% plus 900%).");
            for (int i = 0; i < 50; i++) LimitBreak.Apply(wand, LimitBreak.Stat.BaseDamage);
            Assert.That(LimitBreak.BaseDamageBonus(wand), Is.EqualTo(25f), "Base Damage +0.5 per card, no cap.");
        }

        /// <summary>
        /// Each weapon's own wiki "Limit Break" table (raw pages, 2026-10-12): which stats it is offered, the Base Damage
        /// step and the Duration cap. "Unlike most weapons, Laurel does not benefit from Limit Break" (wiki Laurel).
        /// </summary>
        [Test]
        public void LimitBreak_OffersEachWeaponItsOwnWikiRows()
        {
            T Weapon<T>() where T : AutoAttackWeapon => Track(new GameObject(typeof(T).Name)).AddComponent<T>();
            bool Any(AutoAttackWeapon w)
            {
                foreach (LimitBreak.Stat s in System.Enum.GetValues(typeof(LimitBreak.Stat))) if (LimitBreak.CanRaise(w, s)) return true;
                return false;
            }

            var laurel = Weapon<LaurelWeapon>();
            Assert.That(Any(laurel), Is.False, "Laurel takes no Limit Break card.");
            Assert.That(LimitBreak.DrawStat(laurel, 0.5f, out _), Is.False);

            var garlic = Weapon<GarlicWeapon>();
            Assert.That(LimitBreak.CanRaise(garlic, LimitBreak.Stat.BaseDamage) && LimitBreak.CanRaise(garlic, LimitBreak.Stat.Area), Is.True);
            Assert.That(LimitBreak.CanRaise(garlic, LimitBreak.Stat.Amount) || LimitBreak.CanRaise(garlic, LimitBreak.Stat.Speed), Is.False,
                "Garlic's table has only Base Damage and Area.");

            var whip = Weapon<WhipWeapon>();
            Assert.That(LimitBreak.CanRaise(whip, LimitBreak.Stat.Amount), Is.True);
            Assert.That(LimitBreak.CanRaise(whip, LimitBreak.Stat.Speed), Is.False, "The Whip's table has no Speed.");

            var song = Weapon<SongOfManaWeapon>();
            Assert.That(LimitBreak.CanRaise(song, LimitBreak.Stat.Duration), Is.True);
            Assert.That(LimitBreak.CanRaise(song, LimitBreak.Stat.Amount) || LimitBreak.CanRaise(song, LimitBreak.Stat.Speed), Is.False,
                "Song of Mana ignores Amount and Speed.");

            var pentagram = Weapon<PentagramWeapon>();
            Assert.That(LimitBreak.CanRaise(pentagram, LimitBreak.Stat.Chance), Is.True);
            Assert.That(LimitBreak.CanRaise(pentagram, LimitBreak.Stat.BaseDamage), Is.False, "Pentagram's table has only Chance.");

            var lancet = Weapon<ClockLancetWeapon>();
            Assert.That(LimitBreak.CanRaise(lancet, LimitBreak.Stat.Duration), Is.True);
            Assert.That(LimitBreak.CanRaise(lancet, LimitBreak.Stat.BaseDamage) || LimitBreak.CanRaise(lancet, LimitBreak.Stat.Area), Is.False,
                "Clock Lancet's table has only Duration.");
            for (int i = 0; i < 200; i++) LimitBreak.Apply(lancet, LimitBreak.Stat.Duration);
            Assert.That(lancet.LimitBreakBaseDuration + LimitBreak.DurationSeconds(lancet), Is.EqualTo(10f).Within(1e-3f), "Clock Lancet's Duration stops at 10 seconds in total.");
            Assert.That(LimitBreak.CanRaise(lancet, LimitBreak.Stat.Duration), Is.False);

            Assert.That(LimitBreak.StepFor(Weapon<FireWandWeapon>(), LimitBreak.Stat.BaseDamage), Is.EqualTo(1f), "Fire Wand: +1.");
            Assert.That(LimitBreak.StepFor(Weapon<KnifeWeapon>(), LimitBreak.Stat.BaseDamage), Is.EqualTo(0.5f), "Knife: +0.5.");
            Assert.That(LimitBreak.RowFor(LimitBreak.Stat.Area).Step, Is.EqualTo(0.025f));
            Assert.That(LimitBreak.RowFor(LimitBreak.Stat.Speed).MaxTotal, Is.EqualTo(2f), "Speed up to 300% in total.");
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
                // vampire.survivors.wiki/w/Treasure_Chest (Arcana): past the Arcana limit "they will turn into bronze chests".
                Assert.That(chest.LastResult.Kind, Is.EqualTo(SoulHunter.Gameplay.Pickups.ChestKind.Bronze));
                Assert.That(chest.LastResult.IsArcana, Is.False);
            }
            finally
            {
                typeof(ArcanaManager).GetMethod("OnDestroy", Private).Invoke(arcanas, null);
            }
        }

        [Test]
        public void ArcanaChest_WithEveryArcanaInUse_ActsLikeASilverChest()
        {
            // vampire.survivors.wiki/w/Treasure_Chest (Arcana): "If a newer player has all of their unlocked arcanas in use
            // ... any extra Arcana chests will act like Silver chests instead."
            var arcanas = NewArcanaManager(1);
            var chest = Track(new GameObject("Chest")).AddComponent<ChestLogicController>();
            try
            {
                Assert.That(chest.ProcessArcanaChest(), Does.StartWith("ARCANA CHEST!"));
                Assert.That(chest.ProcessArcanaChest(), Does.Not.StartWith("ARCANA CHEST!"), "No unlocked Arcana is left to give.");
                Assert.That(chest.LastResult.Kind, Is.EqualTo(SoulHunter.Gameplay.Pickups.ChestKind.Silver));
            }
            finally
            {
                typeof(ArcanaManager).GetMethod("OnDestroy", Private).Invoke(arcanas, null);
            }
        }

        /// <summary>
        /// CMB-04 (2026-10-19): Limit Break cards that changed nothing. The Magic Wand read the player's stats directly
        /// (only Pierce worked), Crimson Shroud ignored its Base Damage, Area and Amount cards, and Peachone and Dusk Raven
        /// ignored Speed. Wiki tables: Magic_Wand (Might +0.5%, Area +2.5%, Speed +5%, Amount +1, Pierce +1);
        /// Crimson_Shroud (Might +0.5%, Area +2.5%, Amount +1; "Area increases the size of the explosion", "Amount creates
        /// more explosions"); Peachone and Ebony_Wings ("Speed increases how fast the circle moves", Speed +5%).
        /// </summary>
        [Test]
        public void LimitBreakCards_ReachTheMagicWand_CrimsonShroud_AndThePeachoneOrbit()
        {
            // Magic Wand: one real shot, with an enemy to aim at.
            var wand = Track(new GameObject("TestMagicWand")).AddComponent<MagicWandWeapon>();
            typeof(MagicWandWeapon).GetMethod("Awake", Private).Invoke(wand, null);
            var prefab = Track(new GameObject("TestWandShot"));
            prefab.AddComponent<BoxCollider>();
            prefab.AddComponent<Rigidbody>();
            prefab.AddComponent<Projectile>();
            prefab.SetActive(false);
            typeof(MagicWandWeapon).GetField("_projectilePrefab", Private).SetValue(wand, prefab);
            var enemyObject = Track(new GameObject("TestWandTarget"));
            enemyObject.transform.position = Vector3.forward * 5f;
            var enemy = enemyObject.AddComponent<SoulHunter.Gameplay.AI.EnemyController>();
            if (!SoulHunter.Gameplay.AI.EnemyController.ActiveEnemies.Contains(enemy)) SoulHunter.Gameplay.AI.EnemyController.ActiveEnemies.Add(enemy);
            try
            {
                Assert.That(wand.ShotCount, Is.EqualTo(1), "Precondition: one shot at level 1.");
                for (int i = 0; i < 4; i++) LimitBreak.Apply(wand, LimitBreak.Stat.BaseDamage); // +2
                for (int i = 0; i < 2; i++) LimitBreak.Apply(wand, LimitBreak.Stat.Speed);      // +10%
                for (int i = 0; i < 4; i++) LimitBreak.Apply(wand, LimitBreak.Stat.Area);       // +10%
                LimitBreak.Apply(wand, LimitBreak.Stat.Amount);
                Assert.That(wand.ShotCount, Is.EqualTo(2), "An Amount card adds a shot.");

                Projectile fired = null;
                void Fired(Projectile p) { fired = p; if (p != null) Track(p.gameObject); }
                wand.OnProjectileFired += Fired;
                typeof(MagicWandWeapon).GetMethod("FireWand", Private).Invoke(wand, null);
                wand.OnProjectileFired -= Fired;
                Assert.That(fired, Is.Not.Null, "The wand fired.");
                Assert.That(typeof(Projectile).GetField("_damage", Private).GetValue(fired), Is.EqualTo(12), "Base Damage cards: 10 + 2.");
                Assert.That((float)typeof(Projectile).GetField("_speed", Private).GetValue(fired), Is.EqualTo(22f).Within(1e-3f), "Speed cards: 20 x 1.1.");
                Assert.That(fired.transform.localScale.x, Is.EqualTo(prefab.transform.localScale.x * 1.1f).Within(1e-3f), "Area cards: x 1.1.");
            }
            finally { SoulHunter.Gameplay.AI.EnemyController.ActiveEnemies.Remove(enemy); }

            // Crimson Shroud: Area widens the explosion, Amount adds explosions, Base Damage adds to the hit.
            var shroud = Track(new GameObject("TestCrimsonShroud")).AddComponent<CrimsonShroudWeapon>();
            float radius = shroud.RetaliationRadius;
            int damage = shroud.CalculateRetaliationDamage(10);
            for (int i = 0; i < 4; i++) LimitBreak.Apply(shroud, LimitBreak.Stat.Area);
            for (int i = 0; i < 4; i++) LimitBreak.Apply(shroud, LimitBreak.Stat.BaseDamage);
            Assert.That(shroud.RetaliationRadius, Is.EqualTo(radius * 1.1f).Within(1e-3f), "Area cards widen the explosion.");
            Assert.That(shroud.CalculateRetaliationDamage(10), Is.EqualTo(damage + 2), "Base Damage cards add to the retaliation.");
            LimitBreak.Apply(shroud, LimitBreak.Stat.Amount);
            typeof(CrimsonShroudWeapon).GetMethod("QueueRetaliation", Private).Invoke(shroud, new object[] { new DamagePacket(10, Vector3.zero, Vector3.zero) });
            Assert.That(shroud.PendingPulses, Is.EqualTo(2), "An Amount card adds an explosion.");

            // Peachone and Dusk Raven: Speed turns the circle faster.
            foreach (var bird in new PeachoneWeapon[] { Track(new GameObject("TestPeachone")).AddComponent<PeachoneWeapon>(), Track(new GameObject("TestDuskRaven")).AddComponent<DuskRavenWeapon>() })
            {
                float rate = bird.OrbitRate;
                Assert.That(Mathf.Abs(rate), Is.GreaterThan(0f));
                for (int i = 0; i < 4; i++) LimitBreak.Apply(bird, LimitBreak.Stat.Speed); // +20%
                Assert.That(bird.OrbitRate, Is.EqualTo(rate * 1.2f).Within(1e-3f), $"{bird.GetType().Name}: Speed cards turn the circle faster.");
            }
        }
    }
}
