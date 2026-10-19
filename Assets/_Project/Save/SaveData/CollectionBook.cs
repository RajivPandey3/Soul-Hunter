using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Core.Persistence
{
    /// <summary>One enemy type's defeats on this save (VS Bestiary).</summary>
    [Serializable]
    public class EnemyKillRecord
    {
        /// <summary>The EnemyData asset name (it never changes, unlike a shown name).</summary>
        public string Enemy;
        public int Kills;
    }

    /// <summary>
    /// The VS Collection record of one save (release item P3-02, 2026-10-09). vampire.survivors.wiki/w/Collection (raw,
    /// fetched 2026-10-09): the Collection "lists all of the items that the player has used in their game so far. The items
    /// listed in the Collection include weapons, passive items, pickups, relics, and arcanas that are used by the player at
    /// least once in their save file"; "most items that have not been used yet are marked with a question mark"; "Before the
    /// player starts their first ever round in the game, there are three items that are unlocked in the Collection by
    /// default. They are the Experience Gem, the Gold Coin, and the Friendship Amulet."
    /// Entry ids are built by the Gameplay catalog (CollectionCatalog); this class only keeps the save's list, so it can
    /// live in Foundation and be repaired by SaveService.
    /// Unlocks tied to the entry count:
    /// - Banish (wiki Banish, fetched 2026-10-09): "Each rank of PowerUp is unlocked by these achievements: Filling 60
    ///   entries in the Collection. Filling 70 ... 80 ... 90 ... 100 entries in the Collection."
    /// - Mindbender (wiki Mindbender, fetched 2026-10-09): "It is automatically obtained by filling out 50 entries in the
    ///   Collection" (Soul Hunter: favourite characters, <see cref="CharacterFavourites"/>).
    /// </summary>
    public static class CollectionBook
    {
        /// <summary>Soul Hunter's Experience Gem and Gold Coin (PickupNames: "Soul Coin"; the gem is the Soul Gem).</summary>
        public const string SoulGemId = "Pickup.Soul Gem";
        public const string SoulCoinId = "Pickup.Soul Coin";
        /// <summary>The Soul Egg (Soul Hunter's Golden Egg, a VS relic), found once one is bought for any character.</summary>
        public const string SoulEggId = "Relic.Soul Egg";

        /// <summary>
        /// The wiki's three entries found from the start, mapped to Soul Hunter's own (balance method, owner rule 2026-10-09):
        /// Experience Gem -> Soul Gem, Gold Coin -> Soul Coin. The third, the Friendship Amulet, is VS's co-op relic; Soul
        /// Hunter has no co-op (AGENTS.md, character select: "No gold counter or co-op") and no counterpart, so it is left
        /// out rather than invented, and a fresh save starts with two entries.
        /// </summary>
        public static readonly string[] FoundFromTheStart = { SoulGemId, SoulCoinId };

        /// <summary>wiki Mindbender: filled entries that grant it.</summary>
        public const int MindbenderEntries = 50;

        /// <summary>
        /// Evolution entries ("Evolution." + the WeaponEvolutionData asset name) are not kept here: the save's one record of
        /// evolutions obtained is <see cref="EvolutionDiscovery"/> (P3-01/P3-06, written when a chest evolves a weapon), and
        /// the Collection reads it, so there is no second list to disagree with it.
        /// </summary>
        public const string EvolutionPrefix = "Evolution.";

        public static bool IsFound(GameData data, string id)
        {
            if (data == null || string.IsNullOrEmpty(id)) return false;
            if (id.StartsWith(EvolutionPrefix)) return EvolutionDiscovery.IsDiscovered(data, id.Substring(EvolutionPrefix.Length));
            return data.CollectionFound != null && data.CollectionFound.Contains(id);
        }

        /// <summary>Filled entries on this save: the items found plus the evolutions discovered, each once.</summary>
        public static int Count(GameData data)
        {
            if (data == null) return 0;
            var ids = new HashSet<string>();
            if (data.CollectionFound != null)
                foreach (var id in data.CollectionFound)
                    if (!string.IsNullOrEmpty(id) && !id.StartsWith(EvolutionPrefix)) ids.Add(id);
            if (data.DiscoveredEvolutions != null)
                foreach (var id in data.DiscoveredEvolutions)
                    if (!string.IsNullOrEmpty(id)) ids.Add(EvolutionPrefix + id);
            return ids.Count;
        }

        /// <summary>
        /// Marks an entry found (used at least once). Returns a notice line for every unlock the new count reaches (Banish
        /// ranks, Mindbender); empty when nothing new was unlocked or the entry was already found.
        /// </summary>
        public static List<string> MarkFound(GameData data, string id) => MarkFound(data, id, out _);

        public static List<string> MarkFound(GameData data, string id, out bool isNew)
        {
            isNew = false;
            if (data == null || string.IsNullOrEmpty(id)) return new List<string>();
            if (id.StartsWith(EvolutionPrefix)) isNew = EvolutionDiscovery.MarkDiscovered(data, id.Substring(EvolutionPrefix.Length));
            else
            {
                data.CollectionFound ??= new List<string>();
                if (!data.CollectionFound.Contains(id))
                {
                    data.CollectionFound.Add(id);
                    isNew = true;
                }
            }
            return GrantCountUnlocks(data);
        }

        /// <summary>
        /// Grants every unlock the current entry count has reached and returns a line for each new one. The Banish ids are
        /// <see cref="PowerUpUnlocks.BanishId"/>, which PowerUpUnlocks already reads, so the shop sells those ranks.
        /// </summary>
        public static List<string> GrantCountUnlocks(GameData data)
        {
            var news = new List<string>();
            if (data == null) return news;
            data.UnlockedAchievements ??= new List<string>();
            int count = Count(data);
            foreach (int entries in PowerUpUnlocks.BanishCollectionEntries)
            {
                string id = PowerUpUnlocks.BanishId(entries);
                if (count < entries || data.UnlockedAchievements.Contains(id)) continue;
                data.UnlockedAchievements.Add(id);
                news.Add($"PowerUp unlocked: Banish rank {PowerUpUnlocks.UnlockedRanks(data, PowerUpId.Banish)}");
            }
            if (count >= MindbenderEntries && !CharacterFavourites.IsUnlocked(data))
            {
                CharacterFavourites.Unlock(data);
                news.Add("Unlocked: favourite characters");
            }
            return news;
        }

        /// <summary>
        /// Save repair (SaveService.Migrate): a list without blanks or doubles, the entries found from the start, the Soul
        /// Egg when the save already holds eggs, and the count unlocks the save has earned. Kill records: one per enemy, no
        /// negative counts.
        /// </summary>
        public static void Repair(GameData data)
        {
            if (data == null) return;
            data.CollectionFound ??= new List<string>();
            var ids = new HashSet<string>();
            data.CollectionFound.RemoveAll(id => string.IsNullOrEmpty(id) || id.StartsWith(EvolutionPrefix) || !ids.Add(id));
            foreach (var id in FoundFromTheStart)
                if (ids.Add(id)) data.CollectionFound.Add(id);
            if (data.SoulEggs != null && data.SoulEggs.Exists(egg => egg != null && egg.Eggs > 0) && ids.Add(SoulEggId))
                data.CollectionFound.Add(SoulEggId);
            GrantCountUnlocks(data);

            data.BestiaryKills ??= new List<EnemyKillRecord>();
            var merged = new Dictionary<string, EnemyKillRecord>();
            data.BestiaryKills.RemoveAll(record =>
            {
                if (record == null || string.IsNullOrEmpty(record.Enemy)) return true;
                record.Kills = Mathf.Max(0, record.Kills);
                if (merged.TryGetValue(record.Enemy, out var first))
                {
                    first.Kills = (int)Math.Min(int.MaxValue, (long)first.Kills + record.Kills);
                    return true;
                }
                merged[record.Enemy] = record;
                return false;
            });
        }
    }

    /// <summary>
    /// The VS Bestiary record of one save. vampire.survivors.wiki/w/Ars_Gouda (the Bestiary page, raw, fetched 2026-10-09):
    /// "the player can view statistics for all the enemies they have killed on their save file"; the right panel shows
    /// "The number of times this enemy has been defeated by the player"; "An enemy that isn't encountered yet will have
    /// their number listed, but their name listed as ---------------". So an enemy becomes known by its first defeat.
    /// The wiki opens the Bestiary with a relic, "Obtaining the Ars Gouda from Dairy Plant" (wiki Main_menu). Soul Hunter
    /// has no relics or relic art, so (balance method, owner rule 2026-10-09) the closest rule without new art is used:
    /// the Bestiary opens when a run first reaches Level 3, the stage in Dairy Plant's place (VS's third stage), where the
    /// Ars Gouda lies "to the far south of the starting area". Until then its menu button stays hidden, as VS's does
    /// ("some options, such as the Bestiary ... will only become visible by doing certain tasks", wiki Main_menu).
    /// </summary>
    public static class Bestiary
    {
        public const string UnlockId = "Menu.Bestiary";
        public const int UnlockLevel = 3;

        public static bool IsUnlocked(GameData data) => data?.UnlockedAchievements != null && data.UnlockedAchievements.Contains(UnlockId);

        /// <summary>Opens the Bestiary for good; true when it was new.</summary>
        public static bool Unlock(GameData data)
        {
            if (data == null || IsUnlocked(data)) return false;
            data.UnlockedAchievements ??= new List<string>();
            data.UnlockedAchievements.Add(UnlockId);
            return true;
        }

        /// <summary>Counts one defeat of <paramref name="enemyId"/>; true when it is the first (the entry becomes known).</summary>
        public static bool RecordKill(GameData data, string enemyId) => RecordKill(data, enemyId, out _);

        /// <summary>As <see cref="RecordKill(GameData,string)"/>, and gives the record counted, so a caller can count the
        /// next defeat of the same type with <see cref="CountKill"/> without searching the list again.</summary>
        public static bool RecordKill(GameData data, string enemyId, out EnemyKillRecord record)
        {
            record = null;
            if (data == null || string.IsNullOrEmpty(enemyId)) return false;
            data.BestiaryKills ??= new List<EnemyKillRecord>();
            var kills = data.BestiaryKills;
            for (int i = 0; i < kills.Count; i++)
                if (kills[i] != null && kills[i].Enemy == enemyId) { record = kills[i]; break; }
            if (record == null)
            {
                record = new EnemyKillRecord { Enemy = enemyId, Kills = 1 };
                kills.Add(record);
                return true;
            }
            return CountKill(record);
        }

        /// <summary>Counts one more defeat on a record of the save; true when it is the first.</summary>
        public static bool CountKill(EnemyKillRecord record)
        {
            if (record.Kills < int.MaxValue) record.Kills++;
            return record.Kills == 1;
        }

        public static int Kills(GameData data, string enemyId)
        {
            var record = data?.BestiaryKills?.Find(r => r != null && r.Enemy == enemyId);
            return record != null ? record.Kills : 0;
        }

        public static bool IsKnown(GameData data, string enemyId) => Kills(data, enemyId) > 0;
    }
}
