using System.Collections.Generic;
using SoulHunter.Core.Persistence;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Player;

namespace SoulHunter.UI
{
    /// <summary>
    /// The main menu's Quick Start (vampire.survivors.wiki/w/Main_menu, raw, fetched 2026-10-12): "Quick Start: Immediately
    /// accesses a random stage with a randomly selected character", unlocked by "Unlocking the five main stages of the game:
    /// Mad Forest, Inlaid Library, Dairy Plant, Gallo Tower, and Cappella Magna"; Patch 1.0: ""quick start" now becomes
    /// available after unlocking the 5 main stages".
    ///
    /// Soul Hunter's reading (owner decisions first, then the balance method where the wiki cannot apply):
    /// - No random stage: one run plays all ten levels in order (owner, Run structure, 2026-09-24), so Quick Start starts
    ///   that run at once, skipping the character select screen, with the run modes saved there.
    /// - A random character the player owns (VS picks among its characters; a locked or unbought one cannot be played).
    /// - The condition. VS unlocks its five main stages by playing them; Soul Hunter's Levels 1-5 are those five stages
    ///   (L1 Mad Forest ... L5, the mapping used since 2026-10-09) and a run reaches them in order, so Quick Start opens
    ///   once any character has cleared Level 5 (<see cref="CharacterStages"/>). Locked, the button says so in words.
    /// - The button names the character it will start with ("Quick Start: Aria"), so it is not a blind jump (coordinator
    ///   direction, 2026-10-12; the wiki does not say VS shows the pick). The pick is made again each time the menu opens,
    ///   and with more than one character owned it is never the same as the last one shown.
    /// </summary>
    public static class QuickStart
    {
        public const int LevelNeeded = 5;
        public const string Label = "Quick Start";
        /// <summary>The locked button's words: what it is and what opens it.</summary>
        public static readonly string LockedLabel = Label + " (clear Level " + LevelNeeded + ")";

        /// <summary>True once any character has cleared Level 5 on this save.</summary>
        public static bool IsUnlocked(GameData data)
        {
            if (data?.CharacterStages == null) return false;
            foreach (var record in data.CharacterStages)
                if (record?.Levels != null && record.Levels.Contains(LevelNeeded)) return true;
            return false;
        }

        /// <summary>The characters Quick Start may pick: the catalog's, owned on this save.</summary>
        public static List<CharacterData> Owned(GameData data)
        {
            var owned = new List<CharacterData>();
            var catalog = GameContentCatalog.Load();
            if (catalog == null || catalog.Characters == null) return owned;
            foreach (var character in catalog.Characters)
                if (character != null && CharacterUnlocks.IsOwned(character, data)) owned.Add(character);
            return owned;
        }

        /// <summary>
        /// One owned character at random (<paramref name="roll"/> in 0..1), other than <paramref name="avoid"/> when there
        /// is a choice. Null when the save owns none (the catalog is missing).
        /// </summary>
        public static CharacterData Pick(GameData data, float roll, CharacterData avoid = null)
        {
            var owned = Owned(data);
            if (owned.Count > 1 && avoid != null) owned.Remove(avoid);
            if (owned.Count == 0) return null;
            int index = (int)(UnityEngine.Mathf.Clamp01(roll) * owned.Count);
            return owned[UnityEngine.Mathf.Min(index, owned.Count - 1)];
        }

        /// <summary>The button's words: the pick's name when unlocked, the condition when locked.</summary>
        public static string LabelFor(GameData data, CharacterData pick) =>
            !IsUnlocked(data) ? LockedLabel : pick != null ? Label + ": " + pick.CharacterName : Label;
    }
}
