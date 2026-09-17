using UnityEngine;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Data
{
    public sealed class GameContentCatalog : ScriptableObject
    {
        public CharacterData[] Characters;
        public UpgradeData[] Upgrades;
        public static GameContentCatalog Load() => Resources.Load<GameContentCatalog>("GameContent");
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
