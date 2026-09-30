using NUnit.Framework;
using SoulHunter.Core.Persistence;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// VS locked PowerUps (owner, 2026-10-01: "adopt vs rule"; the Reroll, Skip, Banish, Omni, Charm, Defang and Preserve
    /// pages on vampire.survivors.wiki). Soul Hunter conditions chosen by the owner.
    /// </summary>
    public class PowerUpUnlockTests
    {
        [Test]
        public void ANewSave_HasTheVsLockedPowerUpsLocked_AndTheRestOpen()
        {
            var data = new GameData();
            foreach (var info in PowerUps.All)
            {
                int expected = PowerUpUnlocks.IsLockable(info.Id) ? 0 : info.MaxRank;
                Assert.That(PowerUpUnlocks.UnlockedRanks(data, info.Id), Is.EqualTo(expected), info.Name);
                Assert.That(PowerUps.CanBuyNext(data, info.Id), Is.EqualTo(expected > 0), info.Name);
            }
            Assert.That(PowerUpUnlocks.NextConditionText(data, PowerUpId.Reroll), Is.EqualTo("Reach level 80 with Corvan."));
            Assert.That(PowerUpUnlocks.NextConditionText(data, PowerUpId.Skip), Is.EqualTo("Survive 15 minutes in Level 6."));
            Assert.That(PowerUpUnlocks.NextConditionText(data, PowerUpId.Might), Is.Null);
        }

        [Test]
        public void Level80_WithASetCharacter_UnlocksOneRerollRank()
        {
            var data = new GameData();
            Assert.That(PowerUpUnlocks.Record(data, "Kael", 80, 1, 0f), Is.Empty, "Kael is not one of the five.");
            Assert.That(PowerUpUnlocks.Record(data, "Corvan", 79, 1, 0f), Is.Empty, "Level 79 is not enough.");
            Assert.That(PowerUpUnlocks.Record(data, "Corvan", 80, 1, 0f), Has.Count.EqualTo(1));
            Assert.That(PowerUpUnlocks.Record(data, "Corvan", 90, 1, 0f), Is.Empty, "Once per character.");
            Assert.That(PowerUpUnlocks.UnlockedRanks(data, PowerUpId.Reroll), Is.EqualTo(1));
            Assert.That(PowerUpUnlocks.NextConditionText(data, PowerUpId.Reroll), Is.EqualTo("Reach level 80 with Aria."));
            foreach (var c in PowerUpUnlocks.RerollCharacters) PowerUpUnlocks.Record(data, c, 80, 1, 0f);
            Assert.That(PowerUpUnlocks.UnlockedRanks(data, PowerUpId.Reroll), Is.EqualTo(5));
            Assert.That(PowerUpUnlocks.NextConditionText(data, PowerUpId.Reroll), Is.Null);
        }

        [Test]
        public void SurvivingInLevels6To10_UnlocksSkipRanks()
        {
            var data = new GameData();
            Assert.That(PowerUpUnlocks.Record(data, "Kael", 1, 6, 899f), Is.Empty);
            Assert.That(PowerUpUnlocks.Record(data, "Kael", 1, 5, 1800f), Is.Empty, "Level 5 unlocks nothing.");
            Assert.That(PowerUpUnlocks.Record(data, "Kael", 1, 6, 900f), Has.Count.EqualTo(1));
            Assert.That(PowerUpUnlocks.Record(data, "Kael", 1, 7, 900f), Is.Empty, "Level 7 needs 30 minutes.");
            Assert.That(PowerUpUnlocks.Record(data, "Kael", 1, 7, 1800f), Has.Count.EqualTo(1));
            PowerUpUnlocks.Record(data, "Kael", 1, 8, 1800f);
            PowerUpUnlocks.Record(data, "Kael", 1, 9, 900f);
            PowerUpUnlocks.Record(data, "Kael", 1, 10, 900f);
            Assert.That(PowerUpUnlocks.UnlockedRanks(data, PowerUpId.Skip), Is.EqualTo(5));
        }

        [Test]
        public void OnlyUnlockedRanks_CanBeBought()
        {
            var data = new GameData();
            PowerUpUnlocks.Record(data, "Corvan", 80, 1, 0f);
            Assert.That(PowerUps.CanBuyNext(data, PowerUpId.Reroll), Is.True);
            PowerUps.SetRank(data, PowerUpId.Reroll, 1);
            Assert.That(PowerUps.CanBuyNext(data, PowerUpId.Reroll), Is.False, "Rank 2 is still locked.");
        }

        [Test]
        public void ASaveOwningLockedRanks_GetsTheVsRefund()
        {
            var data = new GameData { Currency = 0 };
            PowerUps.SetRank(data, PowerUpId.Might, 2);
            PowerUps.SetRank(data, PowerUpId.Charm, 1);
            int expected = PowerUps.RefundValue(data);
            Assert.That(PowerUpUnlocks.RefundIfLockedRanksOwned(data), Is.EqualTo(expected));
            Assert.That(data.Currency, Is.EqualTo(expected));
            Assert.That(PowerUps.TotalBought(data), Is.EqualTo(0));

            var fine = new GameData();
            PowerUps.SetRank(fine, PowerUpId.Might, 2);
            Assert.That(PowerUpUnlocks.RefundIfLockedRanksOwned(fine), Is.EqualTo(0), "Only open PowerUps: nothing changes.");
            Assert.That(PowerUps.Rank(fine, PowerUpId.Might), Is.EqualTo(2));
        }
    }
}
