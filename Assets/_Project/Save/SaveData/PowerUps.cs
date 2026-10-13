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
    /// Hunter does not have. Some PowerUps are locked until unlocked, as in VS: see <see cref="PowerUpUnlocks"/>.
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

        // ---- On / Off (vampire.survivors.wiki/w/PowerUps, fetched 2026-10-09): "After maxing out a PowerUp (except
        // Rerolls, Skips, Banishes and Seals), the player can then disable the PowerUp, preventing the need to refund all
        // PowerUps to test out PowerUp setups or to get rid of Curse." Soul Hunter has no Seals.
        // Where the wiki is silent (owner's balance method, 2026-10-09: choose and record the reason):
        // - A switched-off PowerUp keeps its ranks: they still count in TotalBought for the fees and the next price, and
        //   Refund still returns their gold. Reason: switching off is free and is "preventing the need to refund", so it
        //   changes nothing that was paid; only what a run gets changes.
        // - Refund clears the off list too, since no rank is left to switch.

        /// <summary>A PowerUp can be switched off or on: maxed, and not Reroll, Skip or Banish (wiki PowerUps).</summary>
        public static bool CanToggle(GameData data, PowerUpId id) =>
            id != PowerUpId.Reroll && id != PowerUpId.Skip && id != PowerUpId.Banish && IsMaxed(data, id);

        /// <summary>False only for a maxed PowerUp the player switched off.</summary>
        public static bool IsEnabled(GameData data, PowerUpId id) =>
            data?.DisabledPowerUps == null || !CanToggle(data, id) || !data.DisabledPowerUps.Contains(id.ToString());

        /// <summary>Switches a maxed PowerUp on or off (ignored for anything that cannot be switched).</summary>
        public static void SetEnabled(GameData data, PowerUpId id, bool enabled)
        {
            if (data == null || !CanToggle(data, id)) return;
            data.DisabledPowerUps ??= new List<string>();
            string key = id.ToString();
            data.DisabledPowerUps.Remove(key);
            if (!enabled) data.DisabledPowerUps.Add(key);
        }

        /// <summary>The ranks a run gets: the bought rank, or 0 while the PowerUp is switched off.</summary>
        public static int ActiveRank(GameData data, PowerUpId id) => IsEnabled(data, id) ? Rank(data, id) : 0;

        /// <summary>A next rank exists and is unlocked (VS: locked ranks cannot be bought).</summary>
        public static bool CanBuyNext(GameData data, PowerUpId id) => Rank(data, id) < PowerUpUnlocks.UnlockedRanks(data, id);

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
            data.DisabledPowerUps?.Clear();
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

    /// <summary>
    /// VS locked PowerUps (owner, 2026-10-01: "adopt vs rule"; each PowerUp's page on vampire.survivors.wiki, fetched
    /// 2026-10-01). A locked rank cannot be bought. VS conditions use VS characters, stages and items, so the owner chose
    /// Soul Hunter ones:
    /// - Reroll: one rank per character that reaches level 80, for 5 set characters (VS: level 80 with Mortaccio, Yatta
    ///   Cavallo, Bianca Ramba, O'Sole Meeo and Sir Ambrojoe).
    /// - Skip: one rank per survival in Levels 6-10 (VS: 15 min in Il Molise, 30 in Green Acres, 30 in The Bone Zone,
    ///   15 in Moongolow, 15 in Boss Rash).
    /// - Banish (VS: 60/70/80/90/100 Collection entries): one rank per threshold, granted by CollectionBook since the
    ///   Collection exists (P3-02, 2026-10-09).
    /// - Omni (wiki Omni, fetched 2026-10-12: "unlocked by leveling up Torrona's Box to the maximum level"): get the
    ///   Cursed Coffer, Soul Hunter's Torrona's Box, to its ninth level in a run (recorded by ItemUnlockTracker). The
    ///   Coffer itself unlocks by holding six evolved weapons at once (ItemUnlocks), as the Box does. From 2026-10-12 to
    ///   2026-10-13, before the Coffer existed, Omni unlocked by those six evolutions directly; a save that earned it then
    ///   keeps it.
    /// - Charm, Defang and Preserve (wiki Charm / Defang / Preserve, fetched 2026-10-12: unlocked by collecting the
    ///   Apoplexy, Antidote and Wax Fetish relics): unlocked by Soul Hunter's relics in their place (<see cref="RelicBook"/>).
    /// Every other PowerUp is open from the start, as in VS. Unlocks are ids in <see cref="GameData.UnlockedAchievements"/>.
    /// </summary>
    public static class PowerUpUnlocks
    {
        public const int RerollLevel = 80;
        public static readonly string[] RerollCharacters = { "Corvan", "Aria", "Draven", "Gideon", "Kaelen" };

        /// <summary>(Level, seconds survived) for Skip ranks 1-5.</summary>
        public static readonly (int Level, int Seconds)[] SkipSurvivals = { (6, 900), (7, 1800), (8, 1800), (9, 900), (10, 900) };

        public static readonly int[] BanishCollectionEntries = { 60, 70, 80, 90, 100 };
        /// <summary>The item, by its type name, and the level that unlock Omni (wiki Omni: Torrona's Box at its maximum level, 9).</summary>
        public const string OmniItem = "CursedCoffer";
        public const int OmniItemLevel = 9;

        /// <summary>Unlocks every rank of Omni, Charm, Defang or Preserve; true when it was new.</summary>
        public static bool UnlockWhole(GameData data, PowerUpId id)
        {
            if (data == null) return false;
            data.UnlockedAchievements ??= new List<string>();
            return Add(data, WholeId(id));
        }

        /// <summary>The relic that unlocks <paramref name="id"/>, as "the X on Level n", or null.</summary>
        private static string RelicFor(PowerUpId id)
        {
            foreach (var relic in RelicBook.All)
                if (relic.UnlocksPowerUp.HasValue && relic.UnlocksPowerUp.Value == id)
                    return $"Find the {relic.Name} on Level {relic.Level}" +
                           (relic.AppearsAtSeconds > 0 ? $" (it appears at {relic.AppearsAtSeconds / 60}:{relic.AppearsAtSeconds % 60:00})." : ".");
            return null;
        }

        public static string RerollId(string character) => "PowerUp.Reroll.Level80." + character;
        public static string SkipId(int level, int seconds) => $"PowerUp.Skip.L{level}.{seconds / 60}m";
        public static string BanishId(int entries) => "PowerUp.Banish.Collection" + entries;
        public static string WholeId(PowerUpId id) => "PowerUp." + id + ".Unlocked";

        public static bool IsLockable(PowerUpId id) =>
            id == PowerUpId.Reroll || id == PowerUpId.Skip || id == PowerUpId.Banish ||
            id == PowerUpId.Omni || id == PowerUpId.Charm || id == PowerUpId.Defang || id == PowerUpId.Preserve;

        private static bool Has(GameData data, string achievement) => data?.UnlockedAchievements != null && data.UnlockedAchievements.Contains(achievement);

        /// <summary>How many ranks of <paramref name="id"/> can be bought.</summary>
        public static int UnlockedRanks(GameData data, PowerUpId id)
        {
            int max = PowerUps.Info(id).MaxRank;
            int n = 0;
            switch (id)
            {
                case PowerUpId.Reroll:
                    foreach (var c in RerollCharacters) if (Has(data, RerollId(c))) n++;
                    return Mathf.Min(n, max);
                case PowerUpId.Skip:
                    foreach (var s in SkipSurvivals) if (Has(data, SkipId(s.Level, s.Seconds))) n++;
                    return Mathf.Min(n, max);
                case PowerUpId.Banish:
                    foreach (var e in BanishCollectionEntries) if (Has(data, BanishId(e))) n++;
                    return Mathf.Min(n, max);
                case PowerUpId.Omni:
                case PowerUpId.Charm:
                case PowerUpId.Defang:
                case PowerUpId.Preserve:
                    return Has(data, WholeId(id)) ? max : 0;
                default:
                    return max;
            }
        }

        /// <summary>What unlocks the next locked rank, or null when every rank is unlocked.</summary>
        public static string NextConditionText(GameData data, PowerUpId id)
        {
            if (UnlockedRanks(data, id) >= PowerUps.Info(id).MaxRank) return null;
            switch (id)
            {
                case PowerUpId.Reroll:
                    foreach (var c in RerollCharacters) if (!Has(data, RerollId(c))) return $"Reach level {RerollLevel} with {c}.";
                    break;
                case PowerUpId.Skip:
                    foreach (var s in SkipSurvivals) if (!Has(data, SkipId(s.Level, s.Seconds))) return $"Survive {s.Seconds / 60} minutes in Level {s.Level}.";
                    break;
                case PowerUpId.Banish:
                    foreach (var e in BanishCollectionEntries) if (!Has(data, BanishId(e))) return $"Fill {e} entries in the Collection.";
                    break;
                case PowerUpId.Omni: return $"Get the Cursed Coffer to level {OmniItemLevel}.";
                case PowerUpId.Charm:
                case PowerUpId.Defang:
                case PowerUpId.Preserve: return RelicFor(id) ?? "Find its relic on a level.";
            }
            return null;
        }

        /// <summary>
        /// Records the Reroll and Skip unlocks this run has met (a set character at level 80; time survived in a level).
        /// Returns a notice line for each new one.
        /// </summary>
        public static List<string> Record(GameData data, string character, int playerLevel, int level, float stageSeconds)
        {
            var news = new List<string>();
            if (data == null) return news;
            data.UnlockedAchievements ??= new List<string>();
            if (playerLevel >= RerollLevel && System.Array.IndexOf(RerollCharacters, character) >= 0 && Add(data, RerollId(character)))
                news.Add($"PowerUp unlocked: Reroll rank {UnlockedRanks(data, PowerUpId.Reroll)}");
            foreach (var s in SkipSurvivals)
                if (level == s.Level && stageSeconds >= s.Seconds && Add(data, SkipId(s.Level, s.Seconds)))
                    news.Add($"PowerUp unlocked: Skip rank {UnlockedRanks(data, PowerUpId.Skip)}");
            return news;
        }

        /// <summary>Every PowerUp unlock at once (tests and debug tools).</summary>
        public static void UnlockEverything(GameData data)
        {
            if (data == null) return;
            data.UnlockedAchievements ??= new List<string>();
            foreach (var c in RerollCharacters) Add(data, RerollId(c));
            foreach (var s in SkipSurvivals) Add(data, SkipId(s.Level, s.Seconds));
            foreach (var e in BanishCollectionEntries) Add(data, BanishId(e));
            foreach (var id in new[] { PowerUpId.Omni, PowerUpId.Charm, PowerUpId.Defang, PowerUpId.Preserve }) Add(data, WholeId(id));
        }

        private static bool Add(GameData data, string achievement)
        {
            if (data.UnlockedAchievements.Contains(achievement)) return false;
            data.UnlockedAchievements.Add(achievement);
            return true;
        }

        /// <summary>
        /// A save that bought ranks now locked (possible between 2026-09-30 and 2026-10-01) gets the VS Refund: every
        /// rank's gold back. Returns the gold refunded.
        /// </summary>
        public static int RefundIfLockedRanksOwned(GameData data)
        {
            if (data?.PowerUpRanks == null) return 0;
            foreach (var info in PowerUps.All)
                if (PowerUps.Rank(data, info.Id) > UnlockedRanks(data, info.Id)) return PowerUps.RefundAll(data);
            return 0;
        }
    }
}
