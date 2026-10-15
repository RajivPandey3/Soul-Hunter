using NUnit.Framework;
using SoulHunter.Core.Persistence;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// The VS PowerUp shop (owner, 2026-09-30: "everwhere follow DOT VS ONLY"; vampire.survivors.wiki/w/PowerUps):
    /// the wiki's list, ranks and initial costs, the price formula with fees, Refund, and the one-time refund of the old
    /// fixed-price shop (owner chose "Refund all").
    /// </summary>
    public class PowerUpShopTests
    {
        [Test]
        public void TheCatalog_IsTheWikiList_WithoutTheSeals()
        {
            Assert.That(PowerUps.All.Length, Is.EqualTo(23));
            (PowerUpId id, int ranks, float per, int cost)[] wiki =
            {
                (PowerUpId.Might, 5, 0.05f, 200), (PowerUpId.Armor, 3, 1f, 600), (PowerUpId.MaxHealth, 3, 0.1f, 200), (PowerUpId.Recovery, 5, 0.1f, 200),
                (PowerUpId.Cooldown, 2, 0.025f, 900), (PowerUpId.Area, 2, 0.05f, 300), (PowerUpId.Speed, 2, 0.1f, 300), (PowerUpId.Duration, 2, 0.15f, 300),
                (PowerUpId.Amount, 1, 1f, 5000), (PowerUpId.MoveSpeed, 2, 0.05f, 600), (PowerUpId.Magnet, 2, 0.25f, 300), (PowerUpId.Luck, 3, 0.1f, 600),
                (PowerUpId.Growth, 5, 0.03f, 900), (PowerUpId.Greed, 5, 0.1f, 200), (PowerUpId.Curse, 5, 0.1f, 1666), (PowerUpId.Revival, 1, 1f, 10000),
                (PowerUpId.Omni, 5, 0.02f, 1000), (PowerUpId.Charm, 5, 20f, 10000), (PowerUpId.Defang, 5, 0.03f, 10), (PowerUpId.Reroll, 5, 2f, 1000),
                (PowerUpId.Skip, 5, 2f, 100), (PowerUpId.Banish, 5, 2f, 100), (PowerUpId.Preserve, 5, 0.1f, 500),
            };
            foreach (var w in wiki)
            {
                var info = PowerUps.Info(w.id);
                Assert.That(info.MaxRank, Is.EqualTo(w.ranks), $"{w.id} ranks");
                Assert.That(info.PerRank, Is.EqualTo(w.per).Within(1e-5), $"{w.id} per rank");
                Assert.That(info.InitialPrice, Is.EqualTo(w.cost), $"{w.id} initial cost");
            }
            Assert.That(PowerUps.MagnetMultiplier(2), Is.EqualTo(1.5625f).Within(1e-5), "VS Magnet: x1.25 per rank, multiplied.");
        }

        [Test]
        public void Price_FollowsTheWikiFormula()
        {
            var data = new GameData();
            Assert.That(PowerUps.Price(data, PowerUpId.Might), Is.EqualTo(200), "Nothing bought yet: the initial price, no fee.");
            // Buy five ranks of Might, then check each price against InitialPrice x (1 + Bought) + floor(20 x 1.1^TotalBought).
            int paid = 0;
            for (int bought = 0; bought < 5; bought++)
            {
                int expected = bought == 0 ? 200 : 200 * (1 + bought) + (int)System.Math.Floor(20 * System.Math.Pow(1.1, bought));
                Assert.That(PowerUps.Price(data, PowerUpId.Might), Is.EqualTo(expected), $"rank {bought + 1}");
                paid += expected;
                PowerUps.SetRank(data, PowerUpId.Might, bought + 1);
            }
            Assert.That(PowerUps.IsMaxed(data, PowerUpId.Might), Is.True);
            // The wiki's example: base costs 200+400+600+800+1000 = 3000, plus the fees.
            Assert.That(paid, Is.EqualTo(3000 + 22 + 24 + 26 + 29));
            Assert.That(PowerUps.RefundValue(data), Is.EqualTo(paid), "Refund gives back exactly what was paid.");
            // Another PowerUp now carries the fee of 5 ranks bought.
            Assert.That(PowerUps.Price(data, PowerUpId.Armor), Is.EqualTo(600 + (int)System.Math.Floor(20 * System.Math.Pow(1.1, 5))));
        }

        [Test]
        public void Refund_ReturnsTheGold_AndClearsEveryRank()
        {
            var data = new GameData { Currency = 100 };
            PowerUps.SetRank(data, PowerUpId.Greed, 2);
            PowerUps.SetRank(data, PowerUpId.Recovery, 1);
            int value = PowerUps.RefundValue(data);
            Assert.That(value, Is.EqualTo(200 + 400 + 200 + (int)System.Math.Floor(20 * 1.1) + (int)System.Math.Floor(20 * 1.21)));
            Assert.That(PowerUps.RefundAll(data), Is.EqualTo(value));
            Assert.That(data.Currency, Is.EqualTo(100 + value));
            Assert.That(PowerUps.TotalBought(data), Is.EqualTo(0));
        }

        [Test]
        public void AnOldSave_GetsItsOldPowerUpsRefunded_Once()
        {
            // The owner's save had Might level 9 in the old shop (500 each).
            var data = new GameData { SaveVersion = 1, Currency = 21271, MetaMightLevel = 9, MetaArmorLevel = 1, MetaMaxHealthLevel = 2 };
            SaveService.Migrate(data);
            Assert.That(data.Currency, Is.EqualTo(21271 + 9 * 500 + 300 + 2 * 200));
            Assert.That(data.MetaMightLevel + data.MetaArmorLevel + data.MetaMaxHealthLevel, Is.EqualTo(0));
            Assert.That(data.SaveVersion, Is.EqualTo(GameData.CurrentVersion));
            Assert.That(PowerUps.TotalBought(data), Is.EqualTo(0), "The VS shop starts at 0.");
            SaveService.Migrate(data);
            Assert.That(data.Currency, Is.EqualTo(21271 + 9 * 500 + 300 + 2 * 200), "Only once.");
        }

        [Test]
        public void TheSaveRepair_KeepsRanksInsideTheVsLimits()
        {
            var data = new GameData { SaveVersion = GameData.CurrentVersion };
            data.PowerUpRanks.Add(new PowerUpRank { Id = "Might", Rank = 9 });
            data.PowerUpRanks.Add(new PowerUpRank { Id = "Might", Rank = 1 });
            data.PowerUpRanks.Add(new PowerUpRank { Id = "Seal I", Rank = 2 });
            data.PowerUpRanks.Add(new PowerUpRank { Id = "Revival", Rank = -3 });
            SaveService.Migrate(data);
            Assert.That(PowerUps.Rank(data, PowerUpId.Might), Is.EqualTo(5), "Clamped to the VS max.");
            Assert.That(data.PowerUpRanks.Count, Is.EqualTo(1), "Duplicates, unknown PowerUps and empty ranks are dropped.");
        }
    }
}
