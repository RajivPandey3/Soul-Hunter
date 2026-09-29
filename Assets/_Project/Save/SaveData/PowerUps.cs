using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Core.Persistence
{
    /// <summary>The VS PowerUps Soul Hunter sells (vampire.survivors.wiki/w/PowerUps, the list order).</summary>
    public enum PowerUpId
    {
        Might, Armor, MaxHealth, Recovery, Cooldown, Area, Speed, Duration, Amount, MoveSpeed, Magnet, Luck, Growth,
        Greed, Curse, Revival, Omni, Charm, Defang, Reroll, Skip, Banish, Preserve,
    }

    /// <summary>One PowerUp's VS numbers: ranks, bonus per rank and initial cost.</summary>
    public readonly struct PowerUpInfo
    {
        public readonly PowerUpId Id;
        public readonly string Name, Description;
        public readonly int MaxRank, InitialPrice;
        public readonly float PerRank;

        public PowerUpInfo(PowerUpId id, string name, string description, int maxRank, float perRank, int initialPrice)
        {
            Id = id; Name = name; Description = description; MaxRank = maxRank; PerRank = perRank; InitialPrice = initialPrice;
        }
    }

    /// <summary>A bought PowerUp rank in the save.</summary>
    [System.Serializable]
    public class PowerUpRank
    {
        public string Id;
        public int Rank;
    }

    /// <summary>
    /// The VS PowerUp shop (owner, 2026-09-30: "everwhere follow DOT VS ONLY"; vampire.survivors.wiki/w/PowerUps and each
    /// PowerUp's page, fetched 2026-09-30). Every value, rank limit and initial cost is the wiki's. Price of the next rank:
    /// InitialPrice when nothing at all has been bought, else InitialPrice x (1 + Bought) + floor(20 x 1.1^TotalBought).
    /// Refund returns every rank's price. The four Seals are left out: they are used in VS's Collection menu, which Soul
    /// Hunter does not have. VS unlocks some ranks (Reroll, Skip, Banish, Omni, Charm, Defang) through achievements and
    /// items that Soul Hunter does not have; here every rank can be bought.
    /// </summary>
    public static class PowerUps
    {
        public static readonly PowerUpInfo[] All =
        {
            new PowerUpInfo(PowerUpId.Might, "Might", "Raises inflicted damage by 5% per rank (max +25%).", 5, 0.05f, 200),
            new PowerUpInfo(PowerUpId.Armor, "Armor", "Reduces incoming damage by 1 per rank (max -3).", 3, 1f, 600),
            new PowerUpInfo(PowerUpId.MaxHealth, "Max Health", "Augments Max Health by 10% per rank (max +30%).", 3, 0.10f, 200),
            new PowerUpInfo(PowerUpId.Recovery, "Recovery", "Recovers 0.1 HP per rank (max 0.5) per second.", 5, 0.1f, 200),
            new PowerUpInfo(PowerUpId.Cooldown, "Cooldown", "Uses weapons 2.5% faster per rank (max 5%).", 2, 0.025f, 900),
            new PowerUpInfo(PowerUpId.Area, "Area", "Augments area of attacks by 5% per rank (max +10%).", 2, 0.05f, 300),
            new PowerUpInfo(PowerUpId.Speed, "Speed", "Projectiles move 10% faster per rank (max 20%).", 2, 0.10f, 300),
            new PowerUpInfo(PowerUpId.Duration, "Duration", "Effects from weapons last 15% longer per rank (max +30%).", 2, 0.15f, 300),
            new PowerUpInfo(PowerUpId.Amount, "Amount", "Fires 1 more projectile (all weapons).", 1, 1f, 5000),
            new PowerUpInfo(PowerUpId.MoveSpeed, "Move Speed", "Character moves 5% faster per rank (max 10%).", 2, 0.05f, 600),
            new PowerUpInfo(PowerUpId.Magnet, "Magnet", "Items Pickup range +25% per rank (max +50%).", 2, 0.25f, 300),
            new PowerUpInfo(PowerUpId.Luck, "Luck", "Chance to get lucky goes up by 10% per rank (max +30%).", 3, 0.10f, 600),
            new PowerUpInfo(PowerUpId.Growth, "Growth", "Gains 3% more experience per rank (max 15%).", 5, 0.03f, 900),
            new PowerUpInfo(PowerUpId.Greed, "Greed", "Gains 10% more Gold per rank (max +50%).", 5, 0.10f, 200),
            new PowerUpInfo(PowerUpId.Curse, "Curse", "Increase enemy speed, health, quantity, and frequency by 10% per rank (max +50%).", 5, 0.10f, 1666),
            new PowerUpInfo(PowerUpId.Revival, "Revival", "Revives once with 50% health.", 1, 1f, 10000),
            new PowerUpInfo(PowerUpId.Omni, "Omni", "Increases Might, Projectile Speed, Duration, and Area by 2% per rank (max +10%).", 5, 0.02f, 1000),
            new PowerUpInfo(PowerUpId.Charm, "Charm", "Increases enemies spawn quantity by 20 per rank (max +100).", 5, 20f, 10000),
            new PowerUpInfo(PowerUpId.Defang, "Defang", "3% of enemies spawn unable to deal damage per rank (max 15%).", 5, 0.03f, 10),
            new PowerUpInfo(PowerUpId.Reroll, "Reroll", "Twice per rank, allows you to get different choices when leveling up.", 5, 2f, 1000),
            new PowerUpInfo(PowerUpId.Skip, "Skip", "Twice per rank, allows you to skip level up choices and get Experience instead.", 5, 2f, 100),
            new PowerUpInfo(PowerUpId.Banish, "Banish", "Twice per rank, allows you to remove an item from level up choices, for the rest of the run.", 5, 2f, 100),
            new PowerUpInfo(PowerUpId.Preserve, "Preserve", "Reroll, Skip, and Banish actions have a 10% chance to be preserved per rank (max 50%).", 5, 0.10f, 500),
        };

        public static PowerUpInfo Info(PowerUpId id) => All[(int)id];

        public static int Rank(GameData data, PowerUpId id)
        {
            if (data?.PowerUpRanks == null) return 0;
            string key = id.ToString();
            foreach (var r in data.PowerUpRanks) if (r != null && r.Id == key) return Mathf.Clamp(r.Rank, 0, Info(id).MaxRank);
            return 0;
        }

        public static void SetRank(GameData data, PowerUpId id, int rank)
        {
            if (data == null) return;
            data.PowerUpRanks ??= new List<PowerUpRank>();
            string key = id.ToString();
            rank = Mathf.Clamp(rank, 0, Info(id).MaxRank);
            foreach (var r in data.PowerUpRanks)
                if (r != null && r.Id == key) { r.Rank = rank; return; }
            if (rank > 0) data.PowerUpRanks.Add(new PowerUpRank { Id = key, Rank = rank });
        }

        /// <summary>Ranks bought of every PowerUp together (the wiki's TotalBought).</summary>
        public static int TotalBought(GameData data)
        {
            int total = 0;
            foreach (var info in All) total += Rank(data, info.Id);
            return total;
        }

        /// <summary>VS fees: 0 before anything is bought, then floor(20 x 1.1^TotalBought).</summary>
        public static int Fees(int totalBought) => totalBought <= 0 ? 0 : Mathf.FloorToInt(20f * Mathf.Pow(1.1f, totalBought));

        /// <summary>VS price of the next rank of <paramref name="id"/>.</summary>
        public static int Price(GameData data, PowerUpId id)
        {
            var info = Info(id);
            int total = TotalBought(data);
            return total == 0 ? info.InitialPrice : info.InitialPrice * (1 + Rank(data, id)) + Fees(total);
        }

        public static bool IsMaxed(GameData data, PowerUpId id) => Rank(data, id) >= Info(id).MaxRank;

        /// <summary>Gold every bought rank cost (fees are additive since VS v0.7.2, so the order does not matter).</summary>
        public static int RefundValue(GameData data)
        {
            int value = 0, total = 0;
            foreach (var info in All)
            {
                int rank = Rank(data, info.Id);
                for (int k = 0; k < rank; k++) value += info.InitialPrice * (1 + k);
                total += rank;
            }
            for (int t = 1; t < total; t++) value += Fees(t);
            return value;
        }

        /// <summary>VS "Refund PowerUps": every rank is removed and the gold it cost comes back.</summary>
        public static int RefundAll(GameData data)
        {
            if (data == null) return 0;
            int value = RefundValue(data);
            data.Currency += value;
            data.PowerUpRanks?.Clear();
            return value;
        }

        // ---- The effect of a rank, as each PowerUp's wiki page gives it ----

        /// <summary>Max Health: x1.1 per rank, multiplied (+33.1% at rank 3).</summary>
        public static float MaxHealthMultiplier(int rank) => Mathf.Pow(1f + Info(PowerUpId.MaxHealth).PerRank, Mathf.Clamp(rank, 0, Info(PowerUpId.MaxHealth).MaxRank));

        /// <summary>Magnet: x1.25 per rank, multiplied (x1.5625 at rank 2).</summary>
        public static float MagnetMultiplier(int rank) => Mathf.Pow(1f + Info(PowerUpId.Magnet).PerRank, Mathf.Clamp(rank, 0, Info(PowerUpId.Magnet).MaxRank));

        /// <summary>The additive bonus of <paramref name="rank"/> ranks (rank x bonus per rank).</summary>
        public static float Bonus(PowerUpId id, int rank) => Mathf.Clamp(rank, 0, Info(id).MaxRank) * Info(id).PerRank;

        /// <summary>
        /// Soul Hunter's old fixed-price shop (before 2026-09-30) and what each level cost, for the one-time refund:
        /// Might 500, Armor 300, Greed 400, Revival 1000, and the 2026-09-29 Max Health / Recovery buttons 200.
        /// </summary>
        public const int OldMightPrice = 500, OldArmorPrice = 300, OldGreedPrice = 400, OldRevivalPrice = 1000, OldMaxHealthPrice = 200, OldRecoveryPrice = 200;
    }
}
