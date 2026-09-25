using UnityEngine;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Data
{
    public sealed class GameContentCatalog : ScriptableObject
    {
        public CharacterData[] Characters;
        public UpgradeData[] Upgrades;
        [UnityEngine.Tooltip("Floor tile per stage (index 0 = Level 1), used by InfiniteMap.")]
        public GameObject[] StageFloors;
        public static GameContentCatalog Load() => Resources.Load<GameContentCatalog>("GameContent");

        /// <summary>Old saves may still name a renamed character (Antonio became Corvan on 2026-09-25).</summary>
        public static string CurrentName(string name) => name == "Antonio" ? "Corvan" : name;

        /// <summary>
        /// Exact match only (CharacterName, asset name, or "Char_" + name); null when not found.
        /// Use this for gameplay data (stats, starting weapon) so an unknown name never borrows
        /// another character's loadout the way FindCharacter's visual fallback does.
        /// </summary>
        public static CharacterData FindCharacterExact(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            name = CurrentName(name);
            var catalog = Load();
            if (catalog == null || catalog.Characters == null) return null;
            foreach (var character in catalog.Characters)
                if (character != null && (character.CharacterName == name || character.name == name || "Char_" + name == character.name)) return character;
            return null;
        }

        public static CharacterData FindCharacter(string name)
        {
            name = CurrentName(name);
            var catalog = Load();
            if (catalog == null || catalog.Characters == null) return null;
            foreach (var character in catalog.Characters)
                if (character != null && (character.CharacterName == name || character.name == name || "Char_" + name == character.name)) return character;
            return System.Array.Find(catalog.Characters, c => c != null && c.CharacterModelPrefab != null);
        }
    }
}
