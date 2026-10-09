using NUnit.Framework;
using SoulHunter.Core.Persistence;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// VS PowerUp On/Off (vampire.survivors.wiki/w/PowerUps, fetched 2026-10-09): "After maxing out a PowerUp (except
    /// Rerolls, Skips, Banishes and Seals), the player can then disable the PowerUp". Soul Hunter's readings where the
    /// wiki is silent: a switched-off PowerUp keeps its ranks for the fees and the refund; Refund clears the off list.
    /// Plus the select-screen records: levels completed per character and favourites (locked until Mindbender).
    /// </summary>
    public class PowerUpToggleTests
    {
        private static GameData Maxed(params PowerUpId[] ids)
        {
            var data = new GameData();
            PowerUpUnlocks.UnlockEverything(data);
            foreach (var id in ids) PowerUps.SetRank(data, id, PowerUps.Info(id).MaxRank);
            return data;
        }

        [Test]
        public void OnlyAMaxedPowerUp_CanBeSwitched_AndNeverRerollSkipOrBanish()
        {
            var data = Maxed(PowerUpId.Curse, PowerUpId.Reroll, PowerUpId.Skip, PowerUpId.Banish, PowerUpId.Preserve);
            PowerUps.SetRank(data, PowerUpId.Might, 4);
            Assert.That(PowerUps.CanToggle(data, PowerUpId.Curse), Is.True);
            Assert.That(PowerUps.CanToggle(data, PowerUpId.Preserve), Is.True, "Preserve is not one of the wiki's exceptions.");
            Assert.That(PowerUps.CanToggle(data, PowerUpId.Might), Is.False, "Not maxed.");
            foreach (var id in new[] { PowerUpId.Reroll, PowerUpId.Skip, PowerUpId.Banish })
            {
                Assert.That(PowerUps.CanToggle(data, id), Is.False, $"{id} cannot be switched (wiki).");
                PowerUps.SetEnabled(data, id, false);
                Assert.That(PowerUps.ActiveRank(data, id), Is.EqualTo(5), $"{id} stays on.");
            }
            PowerUps.SetEnabled(data, PowerUpId.Might, false);
            Assert.That(PowerUps.ActiveRank(data, PowerUpId.Might), Is.EqualTo(4), "A PowerUp below max cannot be switched off.");
        }

        [Test]
        public void SwitchedOff_GivesNothing_ButKeepsItsRanksForFeesAndPrices()
        {
            var data = Maxed(PowerUpId.Curse);
            int total = PowerUps.TotalBought(data);
            int nextPrice = PowerUps.Price(data, PowerUpId.Might);
            PowerUps.SetEnabled(data, PowerUpId.Curse, false);
            Assert.That(PowerUps.IsEnabled(data, PowerUpId.Curse), Is.False);
            Assert.That(PowerUps.ActiveRank(data, PowerUpId.Curse), Is.EqualTo(0), "Off: the run gets no Curse.");
            Assert.That(PowerUps.Rank(data, PowerUpId.Curse), Is.EqualTo(5), "The ranks stay bought.");
            Assert.That(PowerUps.TotalBought(data), Is.EqualTo(total));
            Assert.That(PowerUps.Price(data, PowerUpId.Might), Is.EqualTo(nextPrice), "Switching does not change the next price.");
            PowerUps.SetEnabled(data, PowerUpId.Curse, true);
            Assert.That(PowerUps.ActiveRank(data, PowerUpId.Curse), Is.EqualTo(5));
        }

        [Test]
        public void Refund_WithAPowerUpOff_ReturnsEverything_AndClearsTheOffList()
        {
            var on = Maxed(PowerUpId.Curse, PowerUpId.Might);
            var off = Maxed(PowerUpId.Curse, PowerUpId.Might);
            PowerUps.SetEnabled(off, PowerUpId.Curse, false);
            Assert.That(PowerUps.RefundValue(off), Is.EqualTo(PowerUps.RefundValue(on)), "A switched-off PowerUp is refunded too.");
            int gold = PowerUps.RefundAll(off);
            Assert.That(gold, Is.EqualTo(PowerUps.RefundValue(on)));
            Assert.That(off.DisabledPowerUps, Is.Empty);
            Assert.That(PowerUps.TotalBought(off), Is.EqualTo(0));
        }

        [Test]
        public void TheSaveRepair_KeepsOnlySwitchablePowerUpsInTheOffList()
        {
            var data = Maxed(PowerUpId.Curse, PowerUpId.Reroll);
            data.SaveVersion = GameData.CurrentVersion;
            PowerUps.SetRank(data, PowerUpId.Might, 2);
            data.DisabledPowerUps.AddRange(new[] { "Curse", "Curse", "Reroll", "Might", "Seal I", null, "Amount" });
            SaveService.Migrate(data);
            Assert.That(data.DisabledPowerUps, Is.EqualTo(new[] { "Curse" }),
                "Unknown, duplicate, non-maxed (Might 2/5, Amount 0/1) and Reroll ids are dropped.");

            var old = new GameData { SaveVersion = GameData.CurrentVersion, DisabledPowerUps = null };
            SaveService.Migrate(old);
            Assert.That(old.DisabledPowerUps, Is.Not.Null.And.Empty, "An older save loads with every PowerUp on.");
        }

        [Test]
        public void TheStatSheet_LeavesOutASwitchedOffPowerUp()
        {
            var data = Maxed(PowerUpId.Might);
            Assert.That(CharacterStatSheet.Build(null, data).Might, Is.EqualTo(1.25f).Within(1e-5), "On: +25%.");
            PowerUps.SetEnabled(data, PowerUpId.Might, false);
            Assert.That(CharacterStatSheet.Build(null, data).Might, Is.EqualTo(1f).Within(1e-5), "Off: no Might bonus.");
            PowerUps.SetEnabled(data, PowerUpId.Might, true);
            Assert.That(CharacterStatSheet.Build(null, data).Might, Is.EqualTo(1.25f).Within(1e-5));
        }

        [Test]
        public void TheResults_ListOnlyActivePowerUps()
        {
            // Wiki Results: "All the powerups that were active during the run, and their level".
            var data = Maxed(PowerUpId.Might, PowerUpId.Curse);
            PowerUps.SetEnabled(data, PowerUpId.Curse, false);
            var results = new SoulHunter.Gameplay.Core.RunResults();
            results.AddPowerUps(data);
            Assert.That(results.PowerUps.Exists(p => p.Key == "Might" && p.Value == 5), Is.True);
            Assert.That(results.PowerUps.Exists(p => p.Key == "Curse"), Is.False, "A switched-off PowerUp was not active.");
        }

        [Test]
        public void LevelsCompleted_AreCountedOncePerCharacter_AndRepaired()
        {
            var data = new GameData();
            Assert.That(CharacterStages.Record(data, "Kael", 1), Is.True);
            Assert.That(CharacterStages.Record(data, "Kael", 1), Is.False, "Once per level.");
            CharacterStages.Record(data, "Kael", 2);
            CharacterStages.Record(data, "Aria", 10);
            Assert.That(CharacterStages.Record(data, "Aria", 11), Is.False, "Only Levels 1-10.");
            Assert.That(CharacterStages.Count(data, "Kael"), Is.EqualTo(2));
            Assert.That(CharacterStages.Count(data, "Aria"), Is.EqualTo(1));
            Assert.That(CharacterStages.Count(data, "Gideon"), Is.EqualTo(0));

            data.SaveVersion = GameData.CurrentVersion;
            data.CharacterStages.Add(new CharacterStageRecord { Character = "Kael", Levels = { 5 } }); // a duplicate record
            data.CharacterStages.Add(null);
            data.CharacterStages[0].Levels.AddRange(new[] { 2, 0, 12 });
            SaveService.Migrate(data);
            Assert.That(CharacterStages.Count(data, "Kael"), Is.EqualTo(2), "Bad and repeated levels are dropped.");
            Assert.That(data.CharacterStages.Count, Is.EqualTo(2), "One record per character.");
        }

        [Test]
        public void Favourites_AreLockedUntilMindbender()
        {
            var data = new GameData();
            CharacterFavourites.Set(data, "Aria", true);
            Assert.That(CharacterFavourites.IsFavourite(data, "Aria"), Is.False, "Locked: VS unlocks favourites through Mindbender.");
            CharacterFavourites.Unlock(data);
            CharacterFavourites.Set(data, "Aria", true);
            Assert.That(CharacterFavourites.IsFavourite(data, "Aria"), Is.True);
            CharacterFavourites.Set(data, "Aria", false);
            Assert.That(CharacterFavourites.IsFavourite(data, "Aria"), Is.False);
        }
    }
}
