using System.Collections.Generic;
using SoulHunter.Core.Persistence;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.Player
{
    /// <summary>One task on the Unlocks screen: what to do, what it unlocks, and whether this save has done it.</summary>
    public readonly struct UnlockTask
    {
        public readonly string Group;
        public readonly string Task;
        public readonly string Reward;
        public readonly bool Done;

        public UnlockTask(string group, string task, string reward, bool done)
        {
            Group = group;
            Task = task;
            Reward = reward;
            Done = done;
        }
    }

    /// <summary>
    /// The VS Unlocks menu (release item P3-02, 2026-10-09). vampire.survivors.wiki/w/Main_menu: Unlocks is "A menu that
    /// lists various tasks that players can accomplish to unlock new items, characters, and more", "Available by default";
    /// wiki Achievements: "Achievements, displayed as Unlocks in-game, unlock new items, characters, stages, gameplay
    /// features, or gold coins". Every Soul Hunter unlock task, with its done state read only from the save (GameData), not
    /// from AchievementManager's in-run set: character unlocks (CharacterUnlocks), PowerUp ranks (PowerUpUnlocks), the
    /// Collection's count unlocks (CollectionBook) and the Bestiary (Bestiary). The wiki does not say how done and not-done
    /// tasks look; each row says it in words.
    /// </summary>
    public static class UnlockTasks
    {
        public const string GroupCharacters = "Characters", GroupPowerUps = "PowerUps", GroupFeatures = "Features";

        public static List<UnlockTask> All(GameData data)
        {
            var tasks = new List<UnlockTask>();
            var achievements = data?.UnlockedAchievements;
            bool Has(string id) => achievements != null && achievements.Contains(id);

            var catalog = GameContentCatalog.Load();
            if (catalog != null && catalog.Characters != null)
                foreach (var character in catalog.Characters)
                {
                    if (character == null || CharacterUnlocks.StartsUnlocked(character)) continue;
                    tasks.Add(new UnlockTask(GroupCharacters, CharacterUnlocks.ConditionText(character), $"Unlocks {character.CharacterName}",
                        CharacterUnlocks.IsUnlocked(character, data)));
                }

            int rank = 0;
            foreach (var c in PowerUpUnlocks.RerollCharacters)
                tasks.Add(new UnlockTask(GroupPowerUps, $"Reach level {PowerUpUnlocks.RerollLevel} with {c}.", $"Reroll PowerUp, rank {++rank}",
                    Has(PowerUpUnlocks.RerollId(c))));
            rank = 0;
            foreach (var s in PowerUpUnlocks.SkipSurvivals)
                tasks.Add(new UnlockTask(GroupPowerUps, $"Survive {s.Seconds / 60} minutes in Level {s.Level}.", $"Skip PowerUp, rank {++rank}",
                    Has(PowerUpUnlocks.SkipId(s.Level, s.Seconds))));
            rank = 0;
            foreach (int entries in PowerUpUnlocks.BanishCollectionEntries)
                tasks.Add(new UnlockTask(GroupPowerUps, $"Fill {entries} entries in the Collection.", $"Banish PowerUp, rank {++rank}",
                    Has(PowerUpUnlocks.BanishId(entries))));
            foreach (var id in new[] { PowerUpId.Omni, PowerUpId.Charm, PowerUpId.Defang, PowerUpId.Preserve })
            {
                bool done = PowerUpUnlocks.UnlockedRanks(data, id) > 0;
                string condition = PowerUpUnlocks.NextConditionText(data, id) ?? "Done.";
                tasks.Add(new UnlockTask(GroupPowerUps, condition, $"{PowerUps.Info(id).Name} PowerUp", done));
            }

            tasks.Add(new UnlockTask(GroupFeatures, $"Fill {CollectionBook.MindbenderEntries} entries in the Collection.", "Favourite characters",
                CharacterFavourites.IsUnlocked(data)));
            tasks.Add(new UnlockTask(GroupFeatures, $"Reach Level {Bestiary.UnlockLevel} in a run.", "The Bestiary", Bestiary.IsUnlocked(data)));
            return tasks;
        }
    }
}
