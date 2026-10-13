using System;
using System.Collections.Generic;

namespace SoulHunter.Core.Persistence
{
    /// <summary>How a locked weapon or passive is unlocked (the kinds the VS conditions below need).</summary>
    public enum ItemUnlockKind
    {
        /// <summary>Open on a new save.</summary>
        FromStart,
        /// <summary>Reach player level Target in a run.</summary>
        ReachLevel,
        /// <summary>Survive Target seconds of a level's clock with any character.</summary>
        SurviveSeconds,
        /// <summary>Survive Target seconds of a level's clock with the character Subject.</summary>
        SurviveSecondsWith,
        /// <summary>Get the item Subject to level Target in a run.</summary>
        ItemLevel,
        /// <summary>Hold Target different weapons at once.</summary>
        HoldWeapons,
        /// <summary>Hold Target evolved (or united) weapons at once.</summary>
        HoldEvolutions,
        /// <summary>Break Target Soul Lanterns (VS light sources), counted on the save.</summary>
        LightSourcesBroken,
        /// <summary>Pick up Target Roast Chickens (VS Floor Chickens), counted on the save.</summary>
        ChickensFound,
        /// <summary>Pick up the pickup named Subject once (its Collection entry is found).</summary>
        FindPickup,
        /// <summary>Defeat Target enemies in total on the save.</summary>
        TotalKills,
        /// <summary>Defeat Target enemies of one level in total; Subject is that level's enemy id prefix ("L02_").</summary>
        KillsOfLevel,
        /// <summary>The character Subject is unlocked.</summary>
        CharacterUnlocked,
        /// <summary>Hold the item once (it is found on a stage, never offered while locked).</summary>
        FoundAsStageItem,
    }

    /// <summary>One item's unlock rule: the VS condition (for the record) and Soul Hunter's.</summary>
    public readonly struct ItemUnlockInfo
    {
        /// <summary>The item, as its UpgradeData.UpgradeType name (Foundation cannot see that enum).</summary>
        public readonly string Item;
        public readonly ItemUnlockKind Kind;
        public readonly int Target;
        public readonly string Subject;
        /// <summary>False for items added with or after the unlock system: an older save is not given them for free.</summary>
        public readonly bool ExistedBefore;
        /// <summary>The VS wiki's condition this row follows (documentation; never shown in the game).</summary>
        public readonly string VsCondition;

        public ItemUnlockInfo(string item, ItemUnlockKind kind, int target, string subject, string vsCondition, bool existedBefore = true)
        {
            Item = item; Kind = kind; Target = target; Subject = subject; VsCondition = vsCondition; ExistedBefore = existedBefore;
        }
    }

    /// <summary>What the run in play has reached, for <see cref="ItemUnlocks.Evaluate"/>.</summary>
    public struct ItemUnlockProgress
    {
        /// <summary>The character played (its current catalog name).</summary>
        public string Character;
        public int PlayerLevel;
        /// <summary>The level clock, in seconds (a level cleared at 30:00 reports 1800).</summary>
        public float StageSeconds;
        /// <summary>Different weapons held now.</summary>
        public int WeaponsHeld;
        /// <summary>Evolved (or united) weapons held now.</summary>
        public int EvolutionsHeld;
        /// <summary>The level an item is held at, by its type name (0 = not held).</summary>
        public Func<string, int> ItemLevel;
        /// <summary>Whether a character is unlocked on the save, by name.</summary>
        public Func<string, bool> CharacterUnlocked;
    }

    /// <summary>
    /// The VS item unlock system (owner, 2026-09-30: "missing VS items and the VS unlock system are added later"; built
    /// 2026-10-12). vampire.survivors.wiki/w/Weapons and /w/Passive_items (raw, fetched 2026-10-12): "Most weapons must be
    /// unlocked before they can be offered to the player"; "Most passive items must be unlocked before they can be offered
    /// to the player." Each condition is the row of vampire.survivors.wiki/w/Achievements (raw, fetched 2026-10-12) that
    /// unlocks the item, or the item's own page where the Achievements list has none. VS conditions name VS characters,
    /// stages and pickups, so each is translated to Soul Hunter's content; where no counterpart exists the reason is in
    /// the row's comment (owner's balance method, 2026-10-09).
    ///
    /// What "locked" means here: a locked item is not offered as a NEW item by level-ups (LevelUpManager). Everything else
    /// follows the wiki or is unchanged:
    /// - A character still starts with its own weapon (wiki Phiera_Der_Tuphello: "one of the starting weapons of Pugnala
    ///   Provola. It is unlocked by surviving 10 minutes as Pugnala Provola", so she holds it while it is locked).
    /// - Chests only level items already held (wiki Treasure_Chest), so they need no rule.
    /// - Stage items stay on the ground while locked (wiki Stone_Mask: "It is unlocked by finding it as a stage item for
    ///   the first time"), and an item held this way can be levelled by level-ups in that run (wiki silent; balance
    ///   method: the item is already in the inventory, and refusing its levels would waste the slot).
    /// - The Wandering Merchant sells Bone and Cherry Bomb whether or not they are unlocked (wiki Bone / Cherry_Bomb:
    ///   "It can also be obtained from Merchant in-game by spending 1,000").
    ///
    /// "Survive N minutes" uses the level clock of any level: a VS run is one 30-minute stage, and Soul Hunter's level
    /// clock is that stage clock (owner, Run structure).
    ///
    /// Unlocks are ids in <see cref="GameData.UnlockedAchievements"/> ("ItemUnlock." + the type name). A type this table
    /// does not list (a weapon added later) is unlocked.
    /// </summary>
    public static class ItemUnlocks
    {
        public const string IdPrefix = "ItemUnlock.";
        /// <summary>Set once a save has been through <see cref="Repair"/> (the one-time rule for older saves).</summary>
        public const string InitializedId = "ItemUnlock.Initialized";

        /// <summary>Soul Hunter's names of the VS pickups the "Find a ..." conditions use (PickupNames and DisplayName constants; a test keeps them equal).</summary>
        public const string LuckCharmPickup = "Luck Charm", MagnetPickup = "Soul Magnet", TimeFreezePickup = "Time Freeze",
            SoulTollPickup = "Soul Toll", ChickenPickup = "Roast Chicken";

        public static readonly ItemUnlockInfo[] All =
        {
            // ---- Open from the start. Each item's wiki page (fetched 2026-10-12): "It is unlocked by default." ----
            new ItemUnlockInfo("Whip", ItemUnlockKind.FromStart, 0, null, "Whip: unlocked by default"),
            new ItemUnlockInfo("MagicWand", ItemUnlockKind.FromStart, 0, null, "Magic Wand: unlocked by default"),
            new ItemUnlockInfo("Knife", ItemUnlockKind.FromStart, 0, null, "Knife: unlocked by default"),
            new ItemUnlockInfo("Axe", ItemUnlockKind.FromStart, 0, null, "Axe: unlocked by default"),
            new ItemUnlockInfo("Bible", ItemUnlockKind.FromStart, 0, null, "King Bible: unlocked by default"),
            new ItemUnlockInfo("SantaWater", ItemUnlockKind.FromStart, 0, null, "Santa Water: unlocked by default"),
            new ItemUnlockInfo("Laurel", ItemUnlockKind.FromStart, 0, null, "Laurel: unlocked by default"),
            new ItemUnlockInfo("Spinach", ItemUnlockKind.FromStart, 0, null, "Spinach: unlocked by default"),
            new ItemUnlockInfo("Armor", ItemUnlockKind.FromStart, 0, null, "Armor: unlocked by default"),
            // VS (wiki Metaglio_Left): "It is unlocked after obtaining Yellow Sign". The owner already placed both on every
            // stage as stage items (2026-10-01); nothing the owner opened is locked again, so they stay open.
            new ItemUnlockInfo("MetaglioLeft", ItemUnlockKind.FromStart, 0, null, "Metaglio Left: unlocked after obtaining Yellow Sign"),
            new ItemUnlockInfo("MetaglioRight", ItemUnlockKind.FromStart, 0, null, "Metaglio Right: unlocked after obtaining Yellow Sign"),

            // ---- wiki Achievements, the same condition in Soul Hunter ----
            new ItemUnlockInfo("PlayerSpeed", ItemUnlockKind.ReachLevel, 5, null, "Wings: Reach Level 5."),
            new ItemUnlockInfo("Crown", ItemUnlockKind.ReachLevel, 10, null, "Crown: Reach Level 10."),
            new ItemUnlockInfo("MaxHealth", ItemUnlockKind.SurviveSeconds, 60, null, "Hollow Heart: Survive 1 minute with any character."),
            new ItemUnlockInfo("Peachone", ItemUnlockKind.SurviveSeconds, 600, null, "Peachone: Survive 10 minutes with any character."),
            new ItemUnlockInfo("Pentagram", ItemUnlockKind.SurviveSeconds, 1200, null, "Pentagram: Survive 20 minutes with any character."),
            new ItemUnlockInfo("Bracer", ItemUnlockKind.ItemLevel, 4, "Bible", "Bracer: Get King Bible to Level 4."),
            new ItemUnlockInfo("Candelabrador", ItemUnlockKind.ItemLevel, 4, "SantaWater", "Candelabrador: Get Santa Water to Level 4."),
            new ItemUnlockInfo("Duplicator", ItemUnlockKind.ItemLevel, 7, "MagicWand", "Duplicator: Get Magic Wand to Level 7."),
            new ItemUnlockInfo("Spellbinder", ItemUnlockKind.ItemLevel, 7, "Runetracer", "Spellbinder: Get Runetracer to Level 7."),
            new ItemUnlockInfo("EmptyTome", ItemUnlockKind.HoldWeapons, 6, null, "Empty Tome: Hold 6 different weapons at once."),
            // wiki Light_source: "Destroying 20 light sources unlocks the Fire Wand." Soul Hunter's light source is the Soul Lantern.
            new ItemUnlockInfo("FireWand", ItemUnlockKind.LightSourcesBroken, 20, null, "Fire Wand: Destroy 20 light sources."),
            new ItemUnlockInfo("Garlic", ItemUnlockKind.ChickensFound, 5, null, "Garlic: Find 5 Floor Chickens."),
            // The VS pickups' Soul Hunter versions (LightSourceDrops): Little Clover -> Luck Charm, Vacuum -> Soul Magnet,
            // Orologion -> Time Freeze, Rosary -> Soul Toll.
            new ItemUnlockInfo("Clover", ItemUnlockKind.FindPickup, 1, LuckCharmPickup, "Clover: Find a Little Clover."),
            new ItemUnlockInfo("Attractorb", ItemUnlockKind.FindPickup, 1, MagnetPickup, "Attractorb (achievement \"Magnet\"): Find a Vacuum."),
            new ItemUnlockInfo("ClockLancet", ItemUnlockKind.FindPickup, 1, TimeFreezePickup, "Clock Lancet (achievement \"Lancet\"): Find an Orologion."),
            new ItemUnlockInfo("Cross", ItemUnlockKind.FindPickup, 1, SoulTollPickup, "Cross: Find a Rosary."),
            new ItemUnlockInfo("LightningRing", ItemUnlockKind.TotalKills, 5000, null, "Lightning Ring: Defeat a total of 5000 enemies."),

            // ---- wiki Achievements, "with <VS character>": the Soul Hunter character in that character's place ----
            // VS: "Survive 5 minutes with Pasqualina" (Runetracer is her starting weapon). Soul Hunter's Runetracer starter is
            // Sylas, who needs Level 9 cleared; that would keep Runetracer, and Spellbinder behind it, out of most of the
            // campaign. Balance method: Corvan, the first character a save unlocks (5:00 in Level 1), so the condition is
            // reachable in the second run, as a 5-minute condition should be.
            new ItemUnlockInfo("Runetracer", ItemUnlockKind.SurviveSecondsWith, 300, "Corvan", "Runetracer: Survive 5 minutes with Pasqualina."),
            // VS: "Survive 5 minutes with Gennaro" (wiki Knife: "the starting weapon of Gennaro Belpaese"). Draven starts with the Knife.
            new ItemUnlockInfo("Pummarola", ItemUnlockKind.SurviveSecondsWith, 300, "Draven", "Pummarola: Survive 5 minutes with Gennaro."),
            // VS: "Survive 10 minutes with Pugnala", the character of the Mad Forest coffin (wiki Coffin). No Soul Hunter
            // character starts with the Gun, so (balance method) it is the character Level 1's Soul Casket frees: Corvan.
            new ItemUnlockInfo("Gun", ItemUnlockKind.SurviveSecondsWith, 600, "Corvan", "Phiera Der Tuphello: Survive 10 minutes with Pugnala."),
            // VS: "Survive 15 minutes with Poppea" (Song of Mana is her starting weapon). Aria starts with the Song of Mana.
            new ItemUnlockInfo("SongOfMana", ItemUnlockKind.SurviveSecondsWith, 900, "Aria", "Song of Mana: Survive 15 minutes with Poppea."),
            // VS: "Survive 20 minutes with Krochi". Soul Hunter has no counterpart of Krochi; balance method: Kaelen, a
            // mid-campaign character (Level 4), since a 20-minute condition with a named character sits mid-way in the list.
            new ItemUnlockInfo("Tiragisu", ItemUnlockKind.SurviveSecondsWith, 1200, "Kaelen", "Tiragisu: Survive 20 minutes with Krochi."),
            // VS: "Survive 30 minutes with Lama" (wiki Axe: "the starting weapon of Lama Ladonna"). Gideon starts with the Axe.
            new ItemUnlockInfo("SkullOManiac", ItemUnlockKind.SurviveSecondsWith, 1800, "Gideon", "Skull O'Maniac: Survive 30 minutes with Lama."),

            // ---- The item's own wiki page ----
            // wiki Bone: "the starting weapon of Mortaccio ... Other characters have a small chance to obtain it after
            // Mortaccio is unlocked." Rurik starts with the Bone.
            new ItemUnlockInfo("Bone", ItemUnlockKind.CharacterUnlocked, 0, "Rurik", "Bone: obtainable by other characters after Mortaccio is unlocked."),
            // wiki Cherry_Bomb: "Other characters have a small chance to obtain it after Yatta Cavallo is unlocked"; wiki
            // Achievements: Yatta Cavallo, "Defeat a total of 3000 Lion Heads" (an Inlaid Library enemy). No Soul Hunter
            // character starts with the Cherry Bomb, so the character's own condition is used: 3,000 enemies of Level 2
            // (Inlaid Library's place), counted by the Bestiary's records of that level's types ("L02_").
            new ItemUnlockInfo("CherryBomb", ItemUnlockKind.KillsOfLevel, 3000, "L02_", "Cherry Bomb: after Yatta Cavallo is unlocked (3000 Lion Heads)."),

            // ---- Items added on 2026-10-12 with the unlock system (NewVsItemsBuilder): Miser's Idol = VS Stone Mask, Dusk
            // Raven = VS Ebony Wings, Azure Repeater = VS Eight The Sparrow. The two Rings (VS: stage items, "Unlocked after
            // obtaining Yellow Sign") and the union are not listed, so they are open, like the Metaglios. ----
            new ItemUnlockInfo("MisersIdol", ItemUnlockKind.FoundAsStageItem, 0, null, "Stone Mask: unlocked by finding it as a stage item for the first time.", false),
            new ItemUnlockInfo("DuskRaven", ItemUnlockKind.ItemLevel, 7, "Peachone", "Ebony Wings: Get Peachone to Level 7.", false),
            // VS: "Survive 15 minutes with Pugnala"; Corvan stands for Pugnala (see Gun).
            new ItemUnlockInfo("AzureRepeater", ItemUnlockKind.SurviveSecondsWith, 900, "Corvan", "Eight The Sparrow: Survive 15 minutes with Pugnala.", false),
            // Added 2026-10-13: Cursed Coffer = VS Torrona's Box (wiki Torrona's_Box: "It is unlocked by holding 6 different
            // weapon evolutions at once").
            new ItemUnlockInfo("CursedCoffer", ItemUnlockKind.HoldEvolutions, 6, null, "Torrona's Box: Hold 6 different weapon evolutions at once.", false),
        };

        public static string Id(string item) => IdPrefix + item;

        /// <summary>The table row of an item, or null when the table does not list it.</summary>
        public static ItemUnlockInfo? Find(string item)
        {
            if (string.IsNullOrEmpty(item)) return null;
            foreach (var info in All) if (info.Item == item) return info;
            return null;
        }

        /// <summary>True when the item starts locked on a new save.</summary>
        public static bool IsLockable(string item)
        {
            var info = Find(item);
            return info.HasValue && info.Value.Kind != ItemUnlockKind.FromStart;
        }

        /// <summary>
        /// Whether level-ups may offer the item as a new item. True without a save (EditMode tests, tools) and for any
        /// item the table does not list.
        /// </summary>
        public static bool IsUnlocked(GameData data, string item)
        {
            if (data == null || !IsLockable(item)) return true;
            return data.UnlockedAchievements != null && data.UnlockedAchievements.Contains(Id(item));
        }

        /// <summary>Unlocks one item; true when it was new.</summary>
        public static bool Unlock(GameData data, string item)
        {
            if (data == null || string.IsNullOrEmpty(item)) return false;
            data.UnlockedAchievements ??= new List<string>();
            string id = Id(item);
            if (data.UnlockedAchievements.Contains(id)) return false;
            data.UnlockedAchievements.Add(id);
            return true;
        }

        /// <summary>Every item unlocked at once (tests, the autoplay bot, the benchmark and debug tools).</summary>
        public static void UnlockEverything(GameData data)
        {
            if (data == null) return;
            foreach (var info in All) if (info.Kind != ItemUnlockKind.FromStart) Unlock(data, info.Item);
            Unlock(data, "Initialized");
        }

        /// <summary>Every item locked again, as on a new save (tests of the unlock rules).</summary>
        public static void LockEverything(GameData data)
        {
            if (data?.UnlockedAchievements == null) return;
            data.UnlockedAchievements.RemoveAll(id => id != null && id.StartsWith(IdPrefix) && id != InitializedId);
            data.LightSourcesBroken = 0;
            data.FloorChickensFound = 0;
        }

        /// <summary>
        /// Save repair (SaveService.Migrate). The one-time rule for a save from before the unlock system (owner,
        /// 2026-10-12: existing saves must not lose anything): if the save shows any play (gold, a best time, a chest, a
        /// character, a PowerUp rank, a Bestiary kill, a Soul Egg), every item that existed before the unlock system is
        /// unlocked. Reason: before 2026-10-12 every item was offered from the first level-up, so such a save could already
        /// hold any of them; locking them now would take items away. A save with no play at all starts locked, as a new
        /// VS save does. Items added with the unlock system (<see cref="ItemUnlockInfo.ExistedBefore"/> false) are not
        /// given: no older save ever had them. The counters cannot be negative.
        /// </summary>
        public static void Repair(GameData data)
        {
            if (data == null) return;
            data.UnlockedAchievements ??= new List<string>();
            data.LightSourcesBroken = Math.Max(0, data.LightSourcesBroken);
            data.FloorChickensFound = Math.Max(0, data.FloorChickensFound);
            if (data.UnlockedAchievements.Contains(InitializedId)) return;
            if (HasPlayed(data))
                foreach (var info in All)
                    if (info.Kind != ItemUnlockKind.FromStart && info.ExistedBefore) Unlock(data, info.Item);
            data.UnlockedAchievements.Add(InitializedId);
        }

        /// <summary>Whether the save shows any play (see <see cref="Repair"/>).</summary>
        public static bool HasPlayed(GameData data)
        {
            if (data == null) return false;
            return data.Currency > 0 || data.HighScoreTime > 0f || data.ChestsOpened > 0 || data.BigCoinBagsReceived > 0 ||
                   (data.UnlockedCharacters != null && data.UnlockedCharacters.Count > 0) ||
                   (data.OwnedCharacters != null && data.OwnedCharacters.Count > 0) ||
                   (data.PowerUpRanks != null && data.PowerUpRanks.Count > 0) ||
                   (data.BestiaryKills != null && data.BestiaryKills.Count > 0) ||
                   (data.SoulEggs != null && data.SoulEggs.Count > 0) ||
                   (data.DiscoveredEvolutions != null && data.DiscoveredEvolutions.Count > 0);
        }

        /// <summary>Enemies defeated on this save: the Bestiary's records together (or only ids starting with <paramref name="prefix"/>).</summary>
        public static long Kills(GameData data, string prefix = null)
        {
            long total = 0;
            if (data?.BestiaryKills == null) return 0;
            foreach (var record in data.BestiaryKills)
            {
                if (record == null || string.IsNullOrEmpty(record.Enemy)) continue;
                if (prefix != null && !record.Enemy.StartsWith(prefix, StringComparison.Ordinal)) continue;
                total += Math.Max(0, record.Kills);
            }
            return total;
        }

        /// <summary>Whether <paramref name="info"/>'s condition is met by the save and the run in play.</summary>
        public static bool IsMet(GameData data, in ItemUnlockInfo info, in ItemUnlockProgress progress)
        {
            if (data == null) return false;
            switch (info.Kind)
            {
                case ItemUnlockKind.FromStart: return true;
                case ItemUnlockKind.ReachLevel: return progress.PlayerLevel >= info.Target;
                case ItemUnlockKind.SurviveSeconds: return progress.StageSeconds >= info.Target;
                case ItemUnlockKind.SurviveSecondsWith: return progress.Character == info.Subject && progress.StageSeconds >= info.Target;
                case ItemUnlockKind.ItemLevel: return progress.ItemLevel != null && progress.ItemLevel(info.Subject) >= info.Target;
                case ItemUnlockKind.HoldWeapons: return progress.WeaponsHeld >= info.Target;
                case ItemUnlockKind.HoldEvolutions: return progress.EvolutionsHeld >= info.Target;
                case ItemUnlockKind.LightSourcesBroken: return data.LightSourcesBroken >= info.Target;
                case ItemUnlockKind.ChickensFound: return data.FloorChickensFound >= info.Target;
                case ItemUnlockKind.FindPickup: return CollectionBook.IsFound(data, "Pickup." + info.Subject);
                case ItemUnlockKind.TotalKills: return Kills(data) >= info.Target;
                case ItemUnlockKind.KillsOfLevel: return Kills(data, info.Subject) >= info.Target;
                case ItemUnlockKind.CharacterUnlocked: return progress.CharacterUnlocked != null && progress.CharacterUnlocked(info.Subject);
                case ItemUnlockKind.FoundAsStageItem: return progress.ItemLevel != null && progress.ItemLevel(info.Item) > 0;
                default: return false;
            }
        }

        /// <summary>Unlocks every item whose condition is met and returns the new ones.</summary>
        public static List<ItemUnlockInfo> Evaluate(GameData data, in ItemUnlockProgress progress)
        {
            var news = new List<ItemUnlockInfo>();
            if (data == null) return news;
            foreach (var info in All)
            {
                if (info.Kind == ItemUnlockKind.FromStart || IsUnlocked(data, info.Item)) continue;
                if (IsMet(data, info, progress) && Unlock(data, info.Item)) news.Add(info);
            }
            return news;
        }

        /// <summary>
        /// The condition as the Unlocks screen shows it, in Soul Hunter's own words. <paramref name="itemName"/> turns a
        /// type name into the item's shown name (the Gameplay layer knows it).
        /// </summary>
        public static string ConditionText(in ItemUnlockInfo info, Func<string, string> itemName)
        {
            string Name(string item) => itemName != null ? itemName(item) : item;
            switch (info.Kind)
            {
                case ItemUnlockKind.FromStart: return "Available from the start.";
                case ItemUnlockKind.ReachLevel: return $"Reach level {info.Target} in a run.";
                case ItemUnlockKind.SurviveSeconds: return $"Survive {Minutes(info.Target)} in a level.";
                case ItemUnlockKind.SurviveSecondsWith: return $"Survive {Minutes(info.Target)} in a level with {info.Subject}.";
                case ItemUnlockKind.ItemLevel: return $"Get {Name(info.Subject)} to level {info.Target}.";
                case ItemUnlockKind.HoldWeapons: return $"Hold {info.Target} different weapons at once.";
                case ItemUnlockKind.HoldEvolutions: return $"Hold {info.Target} evolved weapons at once.";
                case ItemUnlockKind.LightSourcesBroken: return $"Break {info.Target} Soul Lanterns.";
                case ItemUnlockKind.ChickensFound: return $"Find {info.Target} Roast Chickens.";
                case ItemUnlockKind.FindPickup: return $"Find a {info.Subject}.";
                case ItemUnlockKind.TotalKills: return $"Defeat a total of {info.Target} enemies.";
                case ItemUnlockKind.KillsOfLevel: return $"Defeat a total of {info.Target} enemies of Level {LevelOfPrefix(info.Subject)}.";
                case ItemUnlockKind.CharacterUnlocked: return $"Unlock {info.Subject}.";
                case ItemUnlockKind.FoundAsStageItem: return "Find it lying on a level.";
                default: return "";
            }
        }

        /// <summary>Progress toward a counted condition, as "n / target" (empty for conditions without a save counter).</summary>
        public static string ProgressText(GameData data, in ItemUnlockInfo info)
        {
            if (data == null) return "";
            switch (info.Kind)
            {
                case ItemUnlockKind.LightSourcesBroken: return $"{Math.Min(data.LightSourcesBroken, info.Target)} / {info.Target}";
                case ItemUnlockKind.ChickensFound: return $"{Math.Min(data.FloorChickensFound, info.Target)} / {info.Target}";
                case ItemUnlockKind.TotalKills: return $"{Math.Min(Kills(data), info.Target)} / {info.Target}";
                case ItemUnlockKind.KillsOfLevel: return $"{Math.Min(Kills(data, info.Subject), info.Target)} / {info.Target}";
                default: return "";
            }
        }

        private static string Minutes(int seconds) => seconds % 60 == 0 ? (seconds / 60 == 1 ? "1 minute" : $"{seconds / 60} minutes") : $"{seconds} seconds";

        /// <summary>"L02_" -> 2.</summary>
        private static int LevelOfPrefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix) || prefix.Length < 3) return 0;
            return int.TryParse(prefix.Substring(1, 2), out int level) ? level : 0;
        }
    }
}
