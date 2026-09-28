using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// Learning Comment: one asset per weapon or passive (2026-09-28). Everything the game needs to know about an item
    /// lives here: what it is (weapon or passive), its card name and text, icon, the weapon prefab spawned on the hero,
    /// its max level, its VS rarity weight for level-up draws, and its UpgradeData level assets.
    /// Adding a weapon = its script + prefab + UpgradeData levels + one of these, listed in GameContentCatalog.WeaponDefinitions.
    /// WeaponDefinitionBuilder (menu Soul Hunter/Content/Build Weapon Definitions) builds them from the current data.
    /// Code that reads items asks this first and falls back to the older per-system tables when no definition exists.
    /// </summary>
    [CreateAssetMenu(fileName = "WD_New", menuName = "Soul Hunter/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        public UpgradeData.UpgradeType Type;
        [Tooltip("Passive item (stat bonus, fills a passive slot) instead of a weapon.")]
        public bool IsPassive;
        [Tooltip("Card and shop name, e.g. \"Magic Wand\".")]
        public string DisplayName;
        [TextArea, Tooltip("Level-1 text: what a weapon does, or a passive's per-level effect.")]
        public string Description;
        public Sprite Icon;
        [Tooltip("Weapon prefab spawned on the hero (null for passives).")]
        public GameObject WeaponPrefab;
        [Min(0), Tooltip("Highest level. 0 = no cap from the definition (older rules decide).")]
        public int MaxLevel;
        [Min(0), Tooltip("VS item rarity: the item's weight in the level-up pool (vampire.survivors.wiki/w/Level_up).")]
        public int Rarity = UpgradeRarity.DefaultWeight;
        [Tooltip("The item's UpgradeData assets, level 1 first.")]
        public UpgradeData[] Levels;

        /// <summary>The UpgradeData for <paramref name="level"/>, or null.</summary>
        public UpgradeData GetLevel(int level)
        {
            if (Levels == null) return null;
            foreach (var upgrade in Levels)
                if (upgrade != null && upgrade.Level == level) return upgrade;
            return null;
        }
    }

    /// <summary>
    /// Finds the WeaponDefinition of an item type in GameContentCatalog.WeaponDefinitions. The index is rebuilt when the
    /// catalog's list changes (the builder assigns a new array). Returns null when no definition exists.
    /// </summary>
    public static class WeaponDefinitionLookup
    {
        private static GameContentCatalog _catalog;
        private static WeaponDefinition[] _indexedList;
        private static readonly Dictionary<UpgradeData.UpgradeType, WeaponDefinition> ByType = new Dictionary<UpgradeData.UpgradeType, WeaponDefinition>();
        private static IList<WeaponDefinition> _override;

        public static WeaponDefinition Find(UpgradeData.UpgradeType type)
        {
            if (_override != null)
            {
                foreach (var definition in _override)
                    if (definition != null && definition.Type == type) return definition;
                return null;
            }

            if (_catalog == null) _catalog = GameContentCatalog.Load();
            var list = _catalog != null ? _catalog.WeaponDefinitions : null;
            if (!ReferenceEquals(list, _indexedList))
            {
                ByType.Clear();
                if (list != null)
                    foreach (var definition in list)
                        if (definition != null && !ByType.ContainsKey(definition.Type)) ByType[definition.Type] = definition;
                _indexedList = list;
            }
            if (ByType.TryGetValue(type, out var found) && found != null) return found;
            return null;
        }

        /// <summary>Tests: use only these definitions (an empty list = none) until <see cref="ClearOverride"/>.</summary>
        public static void Override(IList<WeaponDefinition> definitions) => _override = definitions;

        public static void ClearOverride() => _override = null;
    }
}
