using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SoulHunter.Core.Persistence;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// The VS unlock system (2026-10-12): the item table (wiki Achievements and each item's page), the one-time rule for
    /// older saves, every condition kind, the relics with their lasting boons, the Soul Caskets and the Unlocks list.
    /// Everything runs on in-memory GameData; no save file is touched.
    /// </summary>
    public class ItemUnlockTests
    {
        private static GameData Fresh()
        {
            var data = new GameData { SaveVersion = GameData.CurrentVersion };
            SaveService.Migrate(data);
            return data;
        }

        private static bool IsType(string name) =>
            System.Enum.TryParse(name, out UpgradeData.UpgradeType type) && System.Enum.IsDefined(typeof(UpgradeData.UpgradeType), type);

        [Test]
        public void TheTable_ListsEachItemOnce_AndItsNamesAreRealTypes()
        {
            Assert.That(ItemUnlocks.All.Select(i => i.Item).Distinct().Count(), Is.EqualTo(ItemUnlocks.All.Length), "One row per item.");
            foreach (var info in ItemUnlocks.All)
            {
                Assert.That(info.VsCondition, Is.Not.Empty, info.Item);
                Assert.That(IsType(info.Item), Is.True, info.Item + " is an UpgradeType.");
                if (info.Kind == ItemUnlockKind.ItemLevel) Assert.That(IsType(info.Subject), Is.True, info.Subject);
                if (info.Kind == ItemUnlockKind.SurviveSecondsWith || info.Kind == ItemUnlockKind.CharacterUnlocked)
                    Assert.IsNotNull(GameContentCatalog.FindCharacterExact(info.Subject), info.Subject + " is a character.");
            }
            // The wiki's "unlocked by default" items.
            foreach (var open in new[] { "Whip", "MagicWand", "Knife", "Axe", "Bible", "SantaWater", "Laurel", "Spinach", "Armor" })
                Assert.That(ItemUnlocks.IsLockable(open), Is.False, open);
            // The wiki's numbers.
            Assert.That(ItemUnlocks.Find("FireWand").Value.Target, Is.EqualTo(20));
            Assert.That(ItemUnlocks.Find("Garlic").Value.Target, Is.EqualTo(5));
            Assert.That(ItemUnlocks.Find("LightningRing").Value.Target, Is.EqualTo(5000));
            Assert.That(ItemUnlocks.Find("PlayerSpeed").Value.Target, Is.EqualTo(5));
            Assert.That(ItemUnlocks.Find("Crown").Value.Target, Is.EqualTo(10));
            Assert.That(ItemUnlocks.Find("Pentagram").Value.Target, Is.EqualTo(1200));
            Assert.That(ItemUnlocks.Find("Duplicator").Value.Target, Is.EqualTo(7));
            // The pickup names the "Find a ..." rows use are the game's own.
            Assert.That(ItemUnlocks.LuckCharmPickup, Is.EqualTo(LuckCharmPickup.DisplayName));
            Assert.That(ItemUnlocks.SoulTollPickup, Is.EqualTo(ScreenWipePickup.DisplayName));
            Assert.That(ItemUnlocks.MagnetPickup, Is.EqualTo(PickupNames.Magnet));
            Assert.That(ItemUnlocks.TimeFreezePickup, Is.EqualTo(PickupNames.TimeFreeze));
            Assert.That(ItemUnlocks.ChickenPickup, Is.EqualTo(PickupNames.Chicken));
        }

        [Test]
        public void AFreshSave_StartsLocked_AndUnknownItemsAndNoSaveAreOpen()
        {
            var data = Fresh();
            Assert.That(ItemUnlocks.IsUnlocked(data, "Whip"), Is.True);
            Assert.That(ItemUnlocks.IsUnlocked(data, "FireWand"), Is.False);
            Assert.That(ItemUnlocks.IsUnlocked(data, "Garlic"), Is.False);
            Assert.That(ItemUnlocks.IsUnlocked(data, "MetaglioLeft"), Is.True, "The owner opened the Metaglios.");
            Assert.That(ItemUnlocks.IsUnlocked(data, "AWeaponAddedLater"), Is.True, "A type the table does not list is unlocked.");
            Assert.That(ItemUnlocks.IsUnlocked(null, "FireWand"), Is.True, "No save (EditMode tests, tools): open.");
            Assert.That(data.UnlockedAchievements, Does.Contain(ItemUnlocks.InitializedId));
        }

        [Test]
        public void ASaveWithPlay_KeepsEveryOlderItem_Once()
        {
            var data = new GameData { SaveVersion = 2, Currency = 21271 };
            SaveService.Migrate(data);
            foreach (var info in ItemUnlocks.All)
                Assert.That(ItemUnlocks.IsUnlocked(data, info.Item), Is.EqualTo(info.ExistedBefore || info.Kind == ItemUnlockKind.FromStart), info.Item);
            Assert.That(ItemUnlocks.IsUnlocked(data, "MisersIdol"), Is.False, "An item added with the unlock system is not given.");

            // The rule runs once: locking again and repairing again does not hand the items back.
            ItemUnlocks.LockEverything(data);
            SaveService.Migrate(data);
            Assert.That(ItemUnlocks.IsUnlocked(data, "FireWand"), Is.False);

            // Other signs of play count too; a save with none stays locked.
            var chests = new GameData { SaveVersion = 2, ChestsOpened = 3 };
            SaveService.Migrate(chests);
            Assert.That(ItemUnlocks.IsUnlocked(chests, "Pentagram"), Is.True);
            Assert.That(ItemUnlocks.HasPlayed(new GameData()), Is.False);
            var broken = new GameData { SaveVersion = GameData.CurrentVersion, LightSourcesBroken = -4, FloorChickensFound = -1 };
            SaveService.Migrate(broken);
            Assert.That(broken.LightSourcesBroken, Is.EqualTo(0));
            Assert.That(broken.FloorChickensFound, Is.EqualTo(0));
        }

        [Test]
        public void EveryConditionKind_UnlocksExactlyWhenMet()
        {
            var data = Fresh();
            var levels = new Dictionary<string, int>();
            var progress = new ItemUnlockProgress
            {
                Character = "Kael", PlayerLevel = 4, StageSeconds = 59f, WeaponsHeld = 5,
                ItemLevel = name => levels.TryGetValue(name, out int level) ? level : 0,
                CharacterUnlocked = name => data.UnlockedCharacters.Contains(name),
            };
            Assert.That(ItemUnlocks.Evaluate(data, progress), Is.Empty, "Nothing is met yet.");

            progress.PlayerLevel = 5;
            CollectionAssert.AreEquivalent(new[] { "PlayerSpeed" }, ItemUnlocks.Evaluate(data, progress).Select(i => i.Item));
            progress.PlayerLevel = 10;
            progress.StageSeconds = 60f;
            CollectionAssert.AreEquivalent(new[] { "Crown", "MaxHealth" }, ItemUnlocks.Evaluate(data, progress).Select(i => i.Item));

            // "with <character>": the clock alone is not enough.
            progress.StageSeconds = 300f;
            Assert.That(ItemUnlocks.Evaluate(data, progress), Is.Empty);
            progress.Character = "Corvan";
            CollectionAssert.AreEquivalent(new[] { "Runetracer" }, ItemUnlocks.Evaluate(data, progress).Select(i => i.Item));
            progress.Character = "Draven";
            CollectionAssert.AreEquivalent(new[] { "Pummarola" }, ItemUnlocks.Evaluate(data, progress).Select(i => i.Item));
            progress.Character = "Kael";

            levels["Bible"] = 4;
            levels["MagicWand"] = 6;
            CollectionAssert.AreEquivalent(new[] { "Bracer" }, ItemUnlocks.Evaluate(data, progress).Select(i => i.Item));
            levels["MagicWand"] = 7;
            CollectionAssert.AreEquivalent(new[] { "Duplicator" }, ItemUnlocks.Evaluate(data, progress).Select(i => i.Item));
            progress.WeaponsHeld = 6;
            CollectionAssert.AreEquivalent(new[] { "EmptyTome" }, ItemUnlocks.Evaluate(data, progress).Select(i => i.Item));

            data.LightSourcesBroken = 19;
            data.FloorChickensFound = 4;
            Assert.That(ItemUnlocks.Evaluate(data, progress), Is.Empty);
            Assert.That(ItemUnlocks.ProgressText(data, ItemUnlocks.Find("FireWand").Value), Is.EqualTo("19 / 20"));
            data.LightSourcesBroken = 20;
            data.FloorChickensFound = 5;
            CollectionAssert.AreEquivalent(new[] { "FireWand", "Garlic" }, ItemUnlocks.Evaluate(data, progress).Select(i => i.Item));

            CollectionBook.MarkFound(data, "Pickup." + ItemUnlocks.LuckCharmPickup);
            CollectionBook.MarkFound(data, "Pickup." + ItemUnlocks.SoulTollPickup);
            CollectionAssert.AreEquivalent(new[] { "Clover", "Cross" }, ItemUnlocks.Evaluate(data, progress).Select(i => i.Item));

            // Kills come from the Bestiary's records: every type for the total, Level 2's types for the Cherry Bomb.
            data.BestiaryKills.Add(new EnemyKillRecord { Enemy = "L01_GraveBat_Data", Kills = 4999 });
            Assert.That(ItemUnlocks.Evaluate(data, progress), Is.Empty);
            data.BestiaryKills.Add(new EnemyKillRecord { Enemy = "L02_AshSprite_Data", Kills = 2999 });
            CollectionAssert.AreEquivalent(new[] { "LightningRing" }, ItemUnlocks.Evaluate(data, progress).Select(i => i.Item));
            data.BestiaryKills.Add(new EnemyKillRecord { Enemy = "L02_BarkHusk_Data", Kills = 1 });
            CollectionAssert.AreEquivalent(new[] { "CherryBomb" }, ItemUnlocks.Evaluate(data, progress).Select(i => i.Item));

            data.UnlockedCharacters.Add("Rurik");
            CollectionAssert.AreEquivalent(new[] { "Bone" }, ItemUnlocks.Evaluate(data, progress).Select(i => i.Item));

            // A stage-found item unlocks by being held once.
            levels["MisersIdol"] = 1;
            CollectionAssert.AreEquivalent(new[] { "MisersIdol" }, ItemUnlocks.Evaluate(data, progress).Select(i => i.Item));
            Assert.That(ItemUnlocks.Evaluate(data, progress), Is.Empty, "Each unlock is reported once.");
        }

        [Test]
        public void ConditionTexts_AreSoulHuntersOwnWords_WithCounters()
        {
            var data = Fresh();
            data.LightSourcesBroken = 12;
            string Name(string type) => type == "Bible" ? "Tome" : type;
            Assert.That(ItemUnlocks.ConditionText(ItemUnlocks.Find("FireWand").Value, Name), Is.EqualTo("Break 20 Soul Lanterns."));
            Assert.That(ItemUnlocks.ConditionText(ItemUnlocks.Find("Bracer").Value, Name), Is.EqualTo("Get Tome to level 4."));
            Assert.That(ItemUnlocks.ConditionText(ItemUnlocks.Find("Runetracer").Value, Name), Is.EqualTo("Survive 5 minutes in a level with Corvan."));
            Assert.That(ItemUnlocks.ConditionText(ItemUnlocks.Find("MaxHealth").Value, Name), Is.EqualTo("Survive 1 minute in a level."));
            Assert.That(ItemUnlocks.ConditionText(ItemUnlocks.Find("CherryBomb").Value, Name), Is.EqualTo("Defeat a total of 3000 enemies of Level 2."));

            var tasks = UnlockTasks.All(data);
            var lanterns = tasks.First(t => t.Group == UnlockTasks.GroupItems && t.Task.StartsWith("Break 20 Soul Lanterns."));
            Assert.That(lanterns.Task, Does.EndWith("12 / 20"), "A counted task shows its counter.");
            Assert.That(lanterns.Done, Is.False);
            data.LightSourcesBroken = 20;
            ItemUnlocks.Unlock(data, "FireWand");
            lanterns = UnlockTasks.All(data).First(t => t.Group == UnlockTasks.GroupItems && t.Task.StartsWith("Break 20 Soul Lanterns."));
            Assert.That(lanterns.Done, Is.True);
            Assert.That(lanterns.Task, Is.EqualTo("Break 20 Soul Lanterns."), "A done task shows no counter.");
            Assert.That(tasks.Count(t => t.Group == UnlockTasks.GroupRelics), Is.EqualTo(RelicBook.All.Length));
            Assert.That(tasks.Count(t => t.Group == UnlockTasks.GroupCaskets), Is.EqualTo(CasketBook.All.Length));
        }

        [Test]
        public void Relics_AreRecordedOnce_UnlockTheirPowerUp_AndGiveSmallLastingBoons()
        {
            Assert.That(RelicBook.Ids.Distinct().Count(), Is.EqualTo(RelicBook.All.Length), "Stable ids are unique.");
            Assert.That(RelicBook.All.Select(r => r.Name).Distinct().Count(), Is.EqualTo(RelicBook.All.Length));
            foreach (var relic in RelicBook.All)
            {
                Assert.That(relic.Level, Is.InRange(1, 10), relic.Name);
                Assert.That(RelicBook.GiftText(relic), Is.Not.Empty, relic.Name + " gives something.");
                Assert.That(RelicBook.FindById(RelicBook.IdOf(relic)).Value.Name, Is.EqualTo(relic.Name));
                // The owner's 5% rule for a stat boon; a charge boon is one charge.
                bool charges = relic.Boon == RelicBoon.Reroll || relic.Boon == RelicBoon.Skip || relic.Boon == RelicBoon.Banish;
                if (charges) Assert.That(relic.Amount, Is.EqualTo(1f), relic.Name);
                else if (relic.Boon != RelicBoon.None) Assert.That(relic.Amount, Is.InRange(0.001f, 0.05f), relic.Name);
            }
            foreach (var boon in RelicBook.All.Where(r => r.Boon != RelicBoon.None).GroupBy(r => r.Boon))
                Assert.That(boon.Count(), Is.EqualTo(1), $"No two relics raise {boon.Key}.");

            var data = Fresh();
            Assert.That(PowerUpUnlocks.UnlockedRanks(data, PowerUpId.Charm), Is.EqualTo(0));
            var bell = RelicBook.All.First(r => r.UnlocksPowerUp == PowerUpId.Charm);
            Assert.That(bell.Level, Is.EqualTo(1));
            Assert.That(bell.AppearsAtSeconds, Is.EqualTo(540), "wiki Achievements: at minute 9:00.");
            int entries = CollectionBook.Count(data);
            var lines = RelicBook.MarkFound(data, bell);
            Assert.That(lines[0], Does.StartWith("Relic found: " + bell.Name));
            Assert.That(lines[0], Does.Contain("Charm"), "The notice says what it does.");
            Assert.That(RelicBook.IsFound(data, bell), Is.True);
            Assert.That(CollectionBook.Count(data), Is.EqualTo(entries + 1), "One Collection entry.");
            Assert.That(PowerUpUnlocks.UnlockedRanks(data, PowerUpId.Charm), Is.EqualTo(PowerUps.Info(PowerUpId.Charm).MaxRank));
            Assert.That(RelicBook.MarkFound(data, bell), Is.Empty, "Found once.");
            Assert.That(CollectionBook.Count(data), Is.EqualTo(entries + 1));

            foreach (var id in new[] { PowerUpId.Charm, PowerUpId.Defang, PowerUpId.Preserve })
            {
                Assert.That(RelicBook.All.Count(r => r.UnlocksPowerUp == id), Is.EqualTo(1), id + " has its relic.");
                var locked = Fresh();
                Assert.That(PowerUpUnlocks.NextConditionText(locked, id), Does.StartWith("Find the "), id.ToString());
            }
            Assert.That(PowerUpUnlocks.NextConditionText(Fresh(), PowerUpId.Omni), Is.EqualTo("Get the Cursed Coffer to level 9."));
            // wiki Torrona's_Box: "It is unlocked by holding 6 different weapon evolutions at once."
            var coffer = ItemUnlocks.All.Single(i => i.Item == "CursedCoffer");
            Assert.That(coffer.Kind, Is.EqualTo(ItemUnlockKind.HoldEvolutions));
            Assert.That(coffer.Target, Is.EqualTo(6));
            Assert.That(ItemUnlocks.IsMet(Fresh(), coffer, new ItemUnlockProgress { EvolutionsHeld = 5 }), Is.False);
            Assert.That(ItemUnlocks.IsMet(Fresh(), coffer, new ItemUnlockProgress { EvolutionsHeld = 6 }), Is.True);
            Assert.That(PowerUpUnlocks.OmniItem, Is.EqualTo("CursedCoffer"));
            Assert.That(PowerUpUnlocks.OmniItemLevel, Is.EqualTo(9), "wiki Omni: Torrona's Box at its maximum level.");
            Assert.That(PowerUpUnlocks.UnlockWhole(data, PowerUpId.Omni), Is.True);
            Assert.That(PowerUpUnlocks.UnlockedRanks(data, PowerUpId.Omni), Is.EqualTo(5));

            // Boons add up from the relics found; the Bestiary relic opens the Bestiary.
            Assert.That(RelicBook.BoonTotal(data, RelicBoon.Luck), Is.EqualTo(0f));
            RelicBook.MarkFound(data, RelicBook.All.First(r => r.Boon == RelicBoon.Luck));
            Assert.That(RelicBook.BoonTotal(data, RelicBoon.Luck), Is.EqualTo(0.05f).Within(0.0001f));
            Assert.That(Bestiary.IsUnlocked(data), Is.False);
            RelicBook.MarkFound(data, RelicBook.All.First(r => r.OpensBestiary));
            Assert.That(Bestiary.IsUnlocked(data), Is.True);
            Assert.That(RelicBook.BoonTotal(data, RelicBoon.Banish), Is.EqualTo(1f));
            Assert.That(RelicBook.FoundCount(data), Is.EqualTo(3));

            // Every relic and the casket have a Collection entry with the save's id.
            var catalog = CollectionCatalog.Build(GameContentCatalog.Load());
            foreach (var relic in RelicBook.All)
                Assert.That(catalog.Any(e => e.Id == RelicBook.CollectionId(relic) && e.Kind == CollectionKind.Relic), Is.True, relic.Name);
            Assert.That(catalog.Any(e => e.Id == CasketBook.CollectionId), Is.True);
            Assert.That(CollectionCatalog.PickupId(CasketBook.PickupName), Is.EqualTo(CasketBook.CollectionId));
            Assert.That(CollectionCatalog.PickupId(bell.Name), Is.EqualTo(RelicBook.CollectionId(bell)), "The Results name gives the same id.");
        }

        [Test]
        public void Caskets_LieOnTheirCoffinStagesLevels_AndFreeAnExistingLockedCharacter()
        {
            // wiki Coffin: Mad Forest, Inlaid Library, Dairy Plant, Gallo Tower, Mt. Moonspell, Lake Foscari, Cappella Magna (not Abyss Foscari).
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 6, 7, 8 }, CasketBook.All.Select(c => c.Level));
            Assert.That(CasketBook.All.Select(c => c.Character).Distinct().Count(), Is.EqualTo(CasketBook.All.Length));
            foreach (var casket in CasketBook.All)
            {
                var character = GameContentCatalog.FindCharacterExact(casket.Character);
                Assert.IsNotNull(character, casket.Character);
                Assert.That(CharacterUnlocks.StartsUnlocked(character), Is.False, casket.Character + " has its own condition, which stays.");
                Assert.That(character.OriginStage, Is.EqualTo(casket.Level), casket.Character + " comes from the casket's level.");
            }
            Assert.That(CasketBook.OnLevel(5).HasValue, Is.False, "Abyss Foscari has no coffin");
            Assert.That(CasketBook.OnLevel(9).HasValue, Is.False);

            var data = Fresh();
            var first = CasketBook.OnLevel(1).Value;
            var corvan = GameContentCatalog.FindCharacterExact(first.Character);
            Assert.That(CharacterUnlocks.IsUnlocked(corvan, data), Is.False);
            Assert.That(CasketBook.Open(data, first, out bool freed), Is.True);
            Assert.That(freed, Is.True);
            Assert.That(CharacterUnlocks.IsUnlocked(corvan, data), Is.True);
            Assert.That(CharacterUnlocks.IsOwned(corvan, data), Is.False, "A freed character is still bought with gold.");
            Assert.That(CasketBook.IsOpened(data, 1), Is.True);
            Assert.That(CasketBook.Open(data, first, out _), Is.False, "Opened once.");

            var task = UnlockTasks.All(data).First(t => t.Group == UnlockTasks.GroupCharacters && t.Reward == "Unlocks " + first.Character);
            Assert.That(task.Task, Is.EqualTo(CasketBook.FreedText));
            Assert.That(task.Done, Is.True);

            // A character unlocked by its own condition first: the casket opens, but it did not free the character.
            var second = CasketBook.OnLevel(2).Value;
            data.UnlockedCharacters.Add(second.Character);
            Assert.That(CasketBook.Open(data, second, out bool freedAgain), Is.True);
            Assert.That(freedAgain, Is.False);
            Assert.That(CasketBook.WasFreed(data, second.Character), Is.False);
            var own = UnlockTasks.All(data).First(t => t.Group == UnlockTasks.GroupCharacters && t.Reward == "Unlocks " + second.Character);
            Assert.That(own.Task, Is.Not.EqualTo(CasketBook.FreedText));
        }

        [Test]
        public void CasketAndRelicSites_NeverShareALine_AndFitACorridor()
        {
            foreach (var relic in RelicBook.All)
                Assert.That(UnlockSites.RelicOffset(relic).magnitude, Is.EqualTo(UnlockSites.RelicDistance).Within(0.01f), relic.Name);
            for (int level = 1; level <= 8; level++)
            {
                var casket = UnlockSites.CasketOffset(level);
                Assert.That(casket.magnitude, Is.EqualTo(UnlockSites.CasketDistance).Within(0.01f));
                foreach (var relic in RelicBook.OnLevel(level))
                    Assert.That(UnityEngine.Vector3.Angle(casket, UnlockSites.RelicOffset(relic)), Is.GreaterThan(20f), $"Level {level}: {relic.Name}");
            }
            // Two relics of one level lie apart.
            foreach (var level in RelicBook.All.GroupBy(r => r.Level).Where(g => g.Count() > 1))
            {
                var spots = level.Select(r => UnlockSites.RelicOffset(r)).ToList();
                Assert.That(UnityEngine.Vector3.Distance(spots[0], spots[1]), Is.GreaterThan(50f), $"Level {level.Key}");
            }
            // In a corridor a north or south spot lies along it, inside the walls.
            var along = UnlockSites.InCorridor(new UnityEngine.Vector3(0f, 0f, -95f), 9f);
            Assert.That(UnityEngine.Mathf.Abs(along.x), Is.EqualTo(95f).Within(0.01f));
            Assert.That(UnityEngine.Mathf.Abs(along.z), Is.LessThan(9f));
        }
    }
}
