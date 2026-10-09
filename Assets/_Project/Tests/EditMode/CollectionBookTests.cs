using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using SoulHunter.Core.Persistence;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// The VS main menu sections (release item P3-02, 2026-10-09): the Collection record and its count unlocks (wiki
    /// Collection, Banish, Mindbender), the Bestiary record (wiki Ars_Gouda), the Unlocks list and the Credits text.
    /// </summary>
    public class CollectionBookTests
    {
        private static GameData Repaired()
        {
            var data = new GameData();
            SaveService.Migrate(data);
            return data;
        }

        [Test]
        public void AFreshSave_HasTheEntriesFoundFromTheStart_AndNothingElse()
        {
            var data = Repaired();
            CollectionAssert.AreEquivalent(CollectionBook.FoundFromTheStart, data.CollectionFound);
            Assert.That(CollectionBook.Count(data), Is.EqualTo(2), "Soul Gem and Soul Coin (the Friendship Amulet has no counterpart).");
            Assert.That(data.BestiaryKills, Is.Empty);
            Assert.That(Bestiary.IsUnlocked(data), Is.False);
            // The ids match the catalog's: the Experience Gem and the Gold Coin.
            Assert.That(CollectionCatalog.PickupId(CollectionCatalog.GemName), Is.EqualTo(CollectionBook.SoulGemId));
            Assert.That(CollectionCatalog.PickupId(SoulHunter.Gameplay.Pickups.PickupNames.Coin), Is.EqualTo(CollectionBook.SoulCoinId));
        }

        [Test]
        public void MarkFound_IsIdempotent()
        {
            var data = Repaired();
            CollectionBook.MarkFound(data, "Item.Knife", out bool first);
            CollectionBook.MarkFound(data, "Item.Knife", out bool again);
            Assert.That(first, Is.True);
            Assert.That(again, Is.False);
            Assert.That(data.CollectionFound.Count(id => id == "Item.Knife"), Is.EqualTo(1));
        }

        [Test]
        public void AnEvolution_IsFound_FromTheSavesOneEvolutionRecord_AndCountsAsAnEntry()
        {
            var data = Repaired();
            var bloodyTear = GameContentCatalog.Load().CollectionEvolutions.First(e => e.name == "Evo_BloodyTear");
            string id = CollectionCatalog.EvolutionId(bloodyTear);
            Assert.That(CollectionBook.IsFound(data, id), Is.False);
            EvolutionDiscovery.MarkDiscovered(data, bloodyTear.name); // what a chest evolution writes (P3-01/P3-06)
            Assert.That(CollectionBook.IsFound(data, id), Is.True);
            Assert.That(CollectionBook.Count(data), Is.EqualTo(3));
            Assert.That(data.CollectionFound, Does.Not.Contain(id), "No second list of evolutions.");
            CollectionBook.MarkFound(data, id, out bool isNew);
            Assert.That(isNew, Is.False, "Already discovered.");
            Assert.That(CollectionBook.Count(data), Is.EqualTo(3));
        }

        [Test]
        public void BanishRanks_UnlockAtTheWikisCollectionCounts_AndCanBeBought()
        {
            var data = Repaired();
            Assert.That(PowerUps.CanBuyNext(data, PowerUpId.Banish), Is.False, "Locked on a fresh save.");
            int[] thresholds = PowerUpUnlocks.BanishCollectionEntries;
            CollectionAssert.AreEqual(new[] { 60, 70, 80, 90, 100 }, thresholds, "wiki Banish: 60, 70, 80, 90 and 100 entries.");
            int n = 0;
            var lines = new List<string>();
            while (CollectionBook.Count(data) < 100)
                lines.AddRange(CollectionBook.MarkFound(data, "Test.Entry" + n++));
            for (int i = 0; i < thresholds.Length; i++)
                Assert.That(data.UnlockedAchievements, Does.Contain(PowerUpUnlocks.BanishId(thresholds[i])));
            Assert.That(PowerUpUnlocks.UnlockedRanks(data, PowerUpId.Banish), Is.EqualTo(5));
            Assert.That(PowerUps.CanBuyNext(data, PowerUpId.Banish), Is.True);
            Assert.That(lines.Count(l => l.StartsWith("PowerUp unlocked: Banish")), Is.EqualTo(5), "One notice per rank.");
            Assert.That(CharacterFavourites.IsUnlocked(data), Is.True, "wiki Mindbender: 50 entries.");
        }

        [Test]
        public void EachBanishRank_UnlocksExactlyAtItsCount()
        {
            var data = Repaired();
            int n = 0;
            while (CollectionBook.Count(data) < 59) CollectionBook.MarkFound(data, "Test.Entry" + n++);
            Assert.That(PowerUpUnlocks.UnlockedRanks(data, PowerUpId.Banish), Is.EqualTo(0), "59 entries: still locked.");
            Assert.That(CharacterFavourites.IsUnlocked(data), Is.True, "50 entries already reached favourites.");
            CollectionBook.MarkFound(data, "Test.Entry" + n++);
            Assert.That(PowerUpUnlocks.UnlockedRanks(data, PowerUpId.Banish), Is.EqualTo(1), "60 entries: rank 1.");
            while (CollectionBook.Count(data) < 69) CollectionBook.MarkFound(data, "Test.Entry" + n++);
            Assert.That(PowerUpUnlocks.UnlockedRanks(data, PowerUpId.Banish), Is.EqualTo(1));
            CollectionBook.MarkFound(data, "Test.Entry" + n++);
            Assert.That(PowerUpUnlocks.UnlockedRanks(data, PowerUpId.Banish), Is.EqualTo(2), "70 entries: rank 2.");
            Assert.That(PowerUpUnlocks.NextConditionText(data, PowerUpId.Banish), Is.EqualTo("Fill 80 entries in the Collection."));
        }

        [Test]
        public void Repair_KeepsOldFields_RemovesDoubles_MergesKills_AndGrantsEarnedRanks()
        {
            var data = new GameData { SaveVersion = 2, Currency = 1234 };
            data.UnlockedCharacters.Add("Corvan");
            data.CollectionFound = new List<string> { "Item.Whip", "Item.Whip", "", null };
            for (int i = 0; i < 60; i++) data.CollectionFound.Add("Old.Entry" + i);
            data.BestiaryKills = new List<EnemyKillRecord>
            {
                new EnemyKillRecord { Enemy = "L01_GraveBat_Data", Kills = 5 },
                new EnemyKillRecord { Enemy = "L01_GraveBat_Data", Kills = 3 },
                new EnemyKillRecord { Enemy = "L01_BoneSoldier_Data", Kills = -4 },
                null, new EnemyKillRecord { Enemy = "", Kills = 2 }
            };
            data.SoulEggs.Add(new SoulEggRecord { Character = "Kael", Eggs = 1 });
            SaveService.Migrate(data);
            Assert.That(data.Currency, Is.EqualTo(1234));
            Assert.That(data.UnlockedCharacters, Does.Contain("Corvan"));
            Assert.That(data.CollectionFound.Count(id => id == "Item.Whip"), Is.EqualTo(1));
            Assert.That(data.CollectionFound.Any(string.IsNullOrEmpty), Is.False);
            Assert.That(data.CollectionFound, Does.Contain(CollectionBook.SoulGemId).And.Contain(CollectionBook.SoulCoinId).And.Contain(CollectionBook.SoulEggId));
            Assert.That(Bestiary.Kills(data, "L01_GraveBat_Data"), Is.EqualTo(8), "Doubles merge.");
            Assert.That(Bestiary.Kills(data, "L01_BoneSoldier_Data"), Is.EqualTo(0), "No negative counts.");
            Assert.That(data.BestiaryKills.Count, Is.EqualTo(2));
            Assert.That(PowerUpUnlocks.UnlockedRanks(data, PowerUpId.Banish), Is.EqualTo(1), "A save with 60+ entries earns Banish rank 1 on load.");
        }

        [Test]
        public void Bestiary_CountsKillsPerType_AndAnEnemyIsUnknownUntilItsFirstDefeat()
        {
            var data = Repaired();
            Assert.That(Bestiary.IsKnown(data, "L01_GraveBat_Data"), Is.False);
            Assert.That(Bestiary.RecordKill(data, "L01_GraveBat_Data"), Is.True, "The first defeat.");
            Assert.That(Bestiary.RecordKill(data, "L01_GraveBat_Data"), Is.False);
            Bestiary.RecordKill(data, "L01_BoneSoldier_Data");
            Assert.That(Bestiary.Kills(data, "L01_GraveBat_Data"), Is.EqualTo(2));
            Assert.That(Bestiary.Kills(data, "L01_BoneSoldier_Data"), Is.EqualTo(1));
            Assert.That(Bestiary.IsKnown(data, "L01_GhoulHound_Data"), Is.False);
            Assert.That(Bestiary.Unlock(data), Is.True);
            Assert.That(Bestiary.Unlock(data), Is.False, "Once.");
        }

        [Test]
        public void TheCatalog_HoldsEveryItem_OnceEach_InTheWikisOrderOfKinds()
        {
            var catalog = GameContentCatalog.Load();
            Assert.IsNotNull(catalog);
            var entries = CollectionCatalog.Build(catalog);
            Assert.That(entries.Select(e => e.Id).Distinct().Count(), Is.EqualTo(entries.Count), "Unique ids.");
            foreach (var definition in catalog.WeaponDefinitions.Where(d => d != null))
                Assert.That(entries.Any(e => e.Id == CollectionCatalog.ItemId(definition.Type)), Is.True, definition.DisplayName);
            Assert.That(catalog.CollectionEvolutions, Has.Length.EqualTo(14), "The 13 Evo_ assets and Crimson Shroud.");
            foreach (var evolution in catalog.CollectionEvolutions)
            {
                Assert.IsNotNull(evolution);
                Assert.That(entries.Any(e => e.Id == CollectionCatalog.EvolutionId(evolution) && e.Kind == CollectionKind.Evolution), Is.True, evolution.name);
                Assert.That(CollectionCatalog.EvolutionOf(evolution.BaseWeapon), Is.SameAs(evolution), "One evolution per base weapon.");
            }
            foreach (var evolution in catalog.Evolutions) Assert.That(catalog.CollectionEvolutions, Does.Contain(evolution));
            Assert.That(catalog.Arcanas, Has.Length.EqualTo(22)); // all 22 base cards since P2-01 (2026-10-09)
            foreach (var arcana in catalog.Arcanas) Assert.That(entries.Any(e => e.Id == CollectionCatalog.ArcanaId(arcana.Type)), Is.True);
            foreach (var id in CollectionBook.FoundFromTheStart.Append(CollectionBook.SoulEggId))
                Assert.That(entries.Any(e => e.Id == id), Is.True, id);
            // Every chest size and the Arcana chest are the one Soul Reliquary entry.
            foreach (var name in new[] { "Soul Reliquary", "Soul Reliquary x3", "Soul Reliquary x5", "Arcana Reliquary" })
                Assert.That(CollectionCatalog.PickupId(name), Is.EqualTo("Pickup.Soul Reliquary"));
            // The wiki's order of kinds: weapons (each followed by its evolutions), passives, pickups, relics, Arcanas.
            int lastWeaponish = entries.FindLastIndex(e => e.Kind == CollectionKind.Weapon || e.Kind == CollectionKind.Evolution);
            Assert.That(entries.FindIndex(e => e.Kind == CollectionKind.Passive), Is.GreaterThan(lastWeaponish));
            Assert.That(entries.FindIndex(e => e.Kind == CollectionKind.Pickup), Is.GreaterThan(entries.FindLastIndex(e => e.Kind == CollectionKind.Passive)));
            Assert.That(entries.FindIndex(e => e.Kind == CollectionKind.Arcana), Is.GreaterThan(entries.FindLastIndex(e => e.Kind == CollectionKind.Pickup)));
            int whip = entries.FindIndex(e => e.Id == CollectionCatalog.ItemId(UpgradeData.UpgradeType.Whip));
            Assert.That(entries[whip + 1].Name, Is.EqualTo("Bloody Tear"), "Whip, then Bloody Tear, as the wiki's table.");
            Assert.That(entries.All(e => !string.IsNullOrEmpty(e.Name)), Is.True);
        }

        [Test]
        public void TheBestiary_ListsEveryEnemyType_OnceEach_WithItsStats()
        {
            var enemies = BestiaryCatalog.All();
            Assert.That(enemies.Count, Is.EqualTo(MadForestEnemies.Types.Length + 30), "Level 1's VS types, then 10 levels x enemy, elite, boss.");
            Assert.That(enemies.Select(BestiaryCatalog.IdOf).Distinct().Count(), Is.EqualTo(enemies.Count));
            for (int i = 0; i < MadForestEnemies.Types.Length; i++)
                Assert.That(enemies[i].name, Is.EqualTo($"L01_{MadForestEnemies.Types[i].Id}_Data"), "Grave Bat first, as VS opens on Pipeestrello.");
            var hound = enemies.First(e => e.name == "L01_GhoulHound_Data");
            Assert.That(BestiaryCatalog.Health(hound), Is.EqualTo("180"));
            Assert.That(BestiaryCatalog.Power(hound), Is.EqualTo("14"));
            Assert.That(BestiaryCatalog.Speed(hound), Is.EqualTo("130"), "wiki Werewolf: speed 130.");
            Assert.That(BestiaryCatalog.Stages(hound), Does.StartWith("Level 1: "));
            var ember = enemies.First(e => e.name == "L01_EmberBat_Data");
            Assert.That(BestiaryCatalog.Health(ember), Is.EqualTo("50 x player level"));
            Assert.That(BestiaryCatalog.Skills(ember), Does.Contain("HP x Level"));
            var kael = enemies.First(e => e.name == "L10_SoulCore_Boss_Data");
            Assert.That(BestiaryCatalog.Name(kael), Is.EqualTo("Shadow Kael"));
            Assert.That(BestiaryCatalog.Stages(kael), Does.StartWith("Level 10: "));
        }

        [Test]
        public void TheUnlocksList_HasEveryTask_AndItsDoneStateFollowsTheSave()
        {
            var data = Repaired();
            var catalog = GameContentCatalog.Load();
            int characters = catalog.Characters.Count(c => c != null && !CharacterUnlocks.StartsUnlocked(c));
            var tasks = UnlockTasks.All(data);
            int powerUps = PowerUpUnlocks.RerollCharacters.Length + PowerUpUnlocks.SkipSurvivals.Length + PowerUpUnlocks.BanishCollectionEntries.Length + 4;
            Assert.That(tasks.Count, Is.EqualTo(characters + powerUps + 2));
            Assert.That(tasks.All(t => !t.Done), Is.True, "A fresh save has done none.");
            Assert.That(tasks.Count(t => t.Group == UnlockTasks.GroupCharacters), Is.EqualTo(characters));

            data.UnlockedCharacters.Add(catalog.Characters.First(c => c != null && !CharacterUnlocks.StartsUnlocked(c)).CharacterName);
            PowerUpUnlocks.Record(data, "Corvan", 80, 1, 0f);
            Bestiary.Unlock(data);
            tasks = UnlockTasks.All(data);
            Assert.That(tasks.Count(t => t.Done), Is.EqualTo(3), "A character, a Reroll rank and the Bestiary.");
            Assert.That(tasks.First(t => t.Reward == "Reroll PowerUp, rank 1").Done, Is.True);
            Assert.That(tasks.First(t => t.Reward == "The Bestiary").Done, Is.True);
        }

        [Test]
        public void Credits_AreTheCreditFiles_AsPlainLines()
        {
            string row = CreditsText.FromMarkdown("| Game sound | Source file | Pack / track | Author | Page |\n|---|---|---|---|---|\n" +
                                                  "| GemPickup | glass_001.ogg | Interface Sounds | Kenney | https://kenney.nl/assets/interface-sounds |");
            Assert.That(row, Is.EqualTo("GemPickup: Interface Sounds by Kenney (https://kenney.nl/assets/interface-sounds)"));
            var catalog = GameContentCatalog.Load();
            Assert.That(catalog.Credits, Has.Length.EqualTo(2));
            string text = CreditsText.Compose(catalog.Credits);
            StringAssert.Contains("CC0", text);
            StringAssert.Contains("Kenney", text);
            StringAssert.Contains("Cinzel", text, "The OFL fonts.");
            StringAssert.Contains("Barlow Semi Condensed", text);
            StringAssert.DoesNotContain("|", text, "No markdown table left.");
        }

        [Test]
        public void TheGamesLook_HasAnIconForEachNewMenuButton()
        {
            var theme = GameContentCatalog.Load().UiTheme;
            Assert.IsNotNull(theme);
            foreach (var icon in new[] { theme.Icon.Collection, theme.Icon.Bestiary, theme.Icon.Unlocks, theme.Icon.Credits })
                Assert.IsNotNull(icon, "Every main menu button shows its icon.");
            Assert.That(theme.Icon.Collection, Is.Not.SameAs(theme.Icon.PowerUp), "Collection and Power Up look different.");
            Assert.That(theme.Icon.Bestiary, Is.Not.SameAs(theme.Icon.Quit), "Bestiary and Quit look different.");
        }
    }
}
