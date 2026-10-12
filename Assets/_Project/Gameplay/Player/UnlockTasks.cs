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
    /// from an in-run set: character unlocks (CharacterUnlocks), PowerUp ranks (PowerUpUnlocks), the
    /// Collection's count unlocks (CollectionBook) and the Bestiary (Bestiary); since 2026-10-12 also every weapon and
    /// passive unlock (ItemUnlocks), relic (RelicBook) and Soul Casket (CasketBook). The wiki does not say how done and
    /// not-done tasks look; each row says it in words, and a task with a counter shows it until it is done.
    /// </summary>
    public static class UnlockTasks
    {
        public const string GroupCharacters = "Characters", GroupPowerUps = "PowerUps", GroupFeatures = "Features";
        /// <summary>The VS item unlocks, relics and Soul Caskets (2026-10-12; ItemUnlocks, RelicBook, CasketBook).</summary>
        public const string GroupItems = "Weapons and items", GroupRelics = "Relics", GroupCaskets = "Soul Caskets";

        /// <summary>A task with its counter while it is not done ("Break 20 Soul Lanterns.  12 / 20").</summary>
        public static string WithProgress(string task, string progress, bool done) =>
            done || string.IsNullOrEmpty(progress) ? task : $"{task}  {progress}";

        private static string ItemName(string typeName) =>
            SoulHunter.Gameplay.Core.ItemUnlockTracker.TryType(typeName, out var type) ? SoulHunter.Gameplay.Combat.UpgradeText.NameOf(type) : typeName;

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
                    // A character a Soul Casket freed shows that as its done condition (its own condition was not what unlocked it).
                    bool freed = CasketBook.WasFreed(data, character.CharacterName);
                    tasks.Add(new UnlockTask(GroupCharacters, freed ? CasketBook.FreedText : CharacterUnlocks.ConditionText(character),
                        $"Unlocks {character.CharacterName}", CharacterUnlocks.IsUnlocked(character, data)));
                }

            // Weapons and passives (ItemUnlocks): only items this build has, and only those that start locked.
            foreach (var info in ItemUnlocks.All)
            {
                if (info.Kind == ItemUnlockKind.FromStart || !SoulHunter.Gameplay.Core.ItemUnlockTracker.TryType(info.Item, out var type)) continue;
                bool done = data != null && ItemUnlocks.IsUnlocked(data, info.Item);
                tasks.Add(new UnlockTask(GroupItems, WithProgress(ItemUnlocks.ConditionText(info, ItemName), ItemUnlocks.ProgressText(data, info), done),
                    $"Unlocks {SoulHunter.Gameplay.Combat.UpgradeText.NameOf(type)}", done));
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
            int filled = CollectionBook.Count(data);
            foreach (int entries in PowerUpUnlocks.BanishCollectionEntries)
                tasks.Add(new UnlockTask(GroupPowerUps, WithProgress($"Fill {entries} entries in the Collection.", $"{System.Math.Min(filled, entries)} / {entries}",
                    Has(PowerUpUnlocks.BanishId(entries))), $"Banish PowerUp, rank {++rank}", Has(PowerUpUnlocks.BanishId(entries))));
            foreach (var id in new[] { PowerUpId.Omni, PowerUpId.Charm, PowerUpId.Defang, PowerUpId.Preserve })
            {
                bool done = PowerUpUnlocks.UnlockedRanks(data, id) > 0;
                string condition = PowerUpUnlocks.NextConditionText(data, id) ?? "Done.";
                tasks.Add(new UnlockTask(GroupPowerUps, condition, $"{PowerUps.Info(id).Name} PowerUp", done));
            }

            tasks.Add(new UnlockTask(GroupFeatures, WithProgress($"Fill {CollectionBook.MindbenderEntries} entries in the Collection.",
                $"{System.Math.Min(filled, CollectionBook.MindbenderEntries)} / {CollectionBook.MindbenderEntries}", CharacterFavourites.IsUnlocked(data)),
                "Favourite characters", CharacterFavourites.IsUnlocked(data)));
            tasks.Add(new UnlockTask(GroupFeatures, $"Reach Level {Bestiary.UnlockLevel} in a run, or find the Beast Codex.", "The Bestiary", Bestiary.IsUnlocked(data)));

            // Relics (RelicBook): where each lies and what it gives for good.
            foreach (var relic in RelicBook.All)
            {
                string when = relic.AppearsAtSeconds > 0 ? $" (it appears at {relic.AppearsAtSeconds / 60}:{relic.AppearsAtSeconds % 60:00})" : "";
                tasks.Add(new UnlockTask(GroupRelics, $"Find the {relic.Name} on Level {relic.Level}{when}.", RelicBook.GiftText(relic).TrimEnd('.'),
                    RelicBook.IsFound(data, relic)));
            }
            // Soul Caskets (CasketBook): a second way to the character, beside the character's own task above.
            foreach (var casket in CasketBook.All)
                tasks.Add(new UnlockTask(GroupCaskets, $"Defeat the guards of the Soul Casket on Level {casket.Level} and open it.",
                    $"Unlocks {casket.Character}", CasketBook.IsOpened(data, casket.Level)));
            return tasks;
        }
    }
}
