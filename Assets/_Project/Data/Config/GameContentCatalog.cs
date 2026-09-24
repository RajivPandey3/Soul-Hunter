using UnityEngine;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Data
{
    public sealed class GameContentCatalog : ScriptableObject
    {
        public CharacterData[] Characters;
        public UpgradeData[] Upgrades;
        public static GameContentCatalog Load() => Resources.Load<GameContentCatalog>("GameContent");
        /// <summary>
        /// Exact match only (CharacterName, asset name, or "Char_" + name); null when not found.
        /// Use this for gameplay data (stats, starting weapon) so an unknown name never borrows
        /// another character's loadout the way FindCharacter's visual fallback does.
        /// </summary>
        public static CharacterData FindCharacterExact(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var catalog = Load();
            if (catalog == null || catalog.Characters == null) return null;
            foreach (var character in catalog.Characters)
                if (character != null && (character.CharacterName == name || character.name == name || "Char_" + name == character.name)) return character;
            return null;
        }

        public static CharacterData FindCharacter(string name)
        {
            var catalog = Load();
            if (catalog == null || catalog.Characters == null) return null;
            foreach (var character in catalog.Characters)
                if (character != null && (character.CharacterName == name || character.name == name || "Char_" + name == character.name)) return character;
            return System.Array.Find(catalog.Characters, c => c != null && c.CharacterModelPrefab != null);
        }
    }
}
