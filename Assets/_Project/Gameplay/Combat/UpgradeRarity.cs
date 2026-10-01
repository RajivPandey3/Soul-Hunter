using System.Collections.Generic;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Learning Comment: VS level-up weighting (vampire.survivors.wiki/w/Level_up, "Random selection of items"):
    /// every item in the pool has a weight given by its rarity value, the pool weight is the sum of the eligible items'
    /// rarities, and P(item) = itemRarity / itemPoolWeight. Weapons and passives share one pool.
    ///
    /// The weight comes from the item's WeaponDefinition.Rarity; without a definition, from the VS table below; else 100.
    /// The table is each VS item's infobox "rarity" field, fetched 2026-09-28 from
    /// https://vampire.survivors.wiki/w/&lt;Page&gt;?action=raw (page in the comment). WeaponDefinitionBuilder copies it into
    /// the definitions. Soul Hunter items map to the VS item they are modelled on.
    /// </summary>
    public static class UpgradeRarity
    {
        /// <summary>VS: Metaglio Left and Right are found only as stage items, never offered by a level-up.</summary>
        public static bool IsStageItemOnly(UpgradeData.UpgradeType type) =>
            type == UpgradeData.UpgradeType.MetaglioLeft || type == UpgradeData.UpgradeType.MetaglioRight;

        /// <summary>Weight of an item with no known rarity (100 = VS's most common rarity value).</summary>
        public const int DefaultWeight = 100;

        public const string WikiUrlFormat = "https://vampire.survivors.wiki/w/{0}?action=raw";

        /// <summary>VS rarity per Soul Hunter item type, with the VS wiki page it was read from.</summary>
        public static readonly IReadOnlyDictionary<UpgradeData.UpgradeType, (int Rarity, string VsPage)> VsRarity =
            new Dictionary<UpgradeData.UpgradeType, (int Rarity, string VsPage)>
        {
            // Weapons
            { UpgradeData.UpgradeType.MagicWand, (100, "Magic_Wand") },
            { UpgradeData.UpgradeType.Whip, (100, "Whip") },
            { UpgradeData.UpgradeType.Garlic, (70, "Garlic") },
            { UpgradeData.UpgradeType.Axe, (100, "Axe") },
            { UpgradeData.UpgradeType.Bible, (80, "King_Bible") },
            { UpgradeData.UpgradeType.Cross, (80, "Cross") },
            { UpgradeData.UpgradeType.LightningRing, (80, "Lightning_Ring") },
            { UpgradeData.UpgradeType.FireWand, (80, "Fire_Wand") },
            { UpgradeData.UpgradeType.SantaWater, (100, "Santa_Water") },
            { UpgradeData.UpgradeType.Knife, (100, "Knife") },
            { UpgradeData.UpgradeType.Runetracer, (80, "Runetracer") },
            { UpgradeData.UpgradeType.Pentagram, (60, "Pentagram") },
            { UpgradeData.UpgradeType.ClockLancet, (70, "Clock_Lancet") },
            { UpgradeData.UpgradeType.Laurel, (60, "Laurel") },
            { UpgradeData.UpgradeType.Bone, (1, "Bone") },                 // VS: a character/merchant weapon, rarity 1
            { UpgradeData.UpgradeType.Peachone, (50, "Peachone") },
            { UpgradeData.UpgradeType.SongOfMana, (50, "Song_of_Mana") },
            { UpgradeData.UpgradeType.CherryBomb, (1, "Cherry_Bomb") },    // VS: a character/merchant weapon, rarity 1
            { UpgradeData.UpgradeType.Gun, (50, "Phiera_Der_Tuphello") },
            // Passives
            { UpgradeData.UpgradeType.Spinach, (100, "Spinach") },
            { UpgradeData.UpgradeType.Armor, (100, "Armor_(item)") },
            { UpgradeData.UpgradeType.MaxHealth, (90, "Hollow_Heart") },
            { UpgradeData.UpgradeType.Pummarola, (90, "Pummarola") },
            { UpgradeData.UpgradeType.EmptyTome, (50, "Empty_Tome") },
            { UpgradeData.UpgradeType.Candelabrador, (100, "Candelabrador") },
            { UpgradeData.UpgradeType.Bracer, (100, "Bracer") },
            { UpgradeData.UpgradeType.Spellbinder, (100, "Spellbinder") },
            { UpgradeData.UpgradeType.Duplicator, (50, "Duplicator") },
            { UpgradeData.UpgradeType.PlayerSpeed, (50, "Wings") },
            { UpgradeData.UpgradeType.Attractorb, (100, "Attractorb") },
            { UpgradeData.UpgradeType.Clover, (100, "Clover") },
            { UpgradeData.UpgradeType.Crown, (70, "Crown") },
            { UpgradeData.UpgradeType.SkullOManiac, (40, "Skull_O'Maniac") },
            { UpgradeData.UpgradeType.Tiragisu, (40, "Tirajisú") },
            { UpgradeData.UpgradeType.MetaglioLeft, (10, "Metaglio_Left") },
            { UpgradeData.UpgradeType.MetaglioRight, (10, "Metaglio_Right") },
        };

        /// <summary>The VS rarity of <paramref name="type"/> from the table, or <see cref="DefaultWeight"/>.</summary>
        public static int VsWeight(UpgradeData.UpgradeType type) =>
            VsRarity.TryGetValue(type, out var entry) ? entry.Rarity : DefaultWeight;

        /// <summary>
        /// VS passive max levels (each item's wiki infobox `max-level`, fetched 2026-09-28): 5 for Spinach, Armor, Hollow
        /// Heart, Pummarola, Empty Tome, Candelabrador, Bracer, Spellbinder, Wings, Attractorb, Clover, Crown and
        /// Skull O'Maniac; 2 for Duplicator and Tirajisú ("the only passive items with a max level of 2"); 9 for the
        /// Metaglio stage items. 0 for weapons (their prefab sets it).
        /// </summary>
        public static int VsPassiveMaxLevel(UpgradeData.UpgradeType type)
        {
            switch (type)
            {
                case UpgradeData.UpgradeType.Duplicator:
                case UpgradeData.UpgradeType.Tiragisu: return 2;
                case UpgradeData.UpgradeType.MetaglioLeft:
                case UpgradeData.UpgradeType.MetaglioRight: return 9;
                default: return WeaponManager.IsWeaponType(type) ? 0 : 5;
            }
        }

        /// <summary>Level-up weight: the definition's Rarity, else the VS table, else 100.</summary>
        public static int GetWeight(UpgradeData.UpgradeType type)
        {
            var definition = SoulHunter.Gameplay.Data.WeaponDefinitionLookup.Find(type);
            return definition != null ? UnityEngine.Mathf.Max(0, definition.Rarity) : VsWeight(type);
        }
    }
}
