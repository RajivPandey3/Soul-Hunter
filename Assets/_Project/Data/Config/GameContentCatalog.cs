using UnityEngine;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.Gameplay.Data
{
    public sealed class GameContentCatalog : ScriptableObject
    {
        public CharacterData[] Characters;
        public UpgradeData[] Upgrades;
        [UnityEngine.Tooltip("One definition per weapon/passive type (WeaponDefinitionBuilder). Read before the older tables.")]
        public WeaponDefinition[] WeaponDefinitions;
        [UnityEngine.Tooltip("VS evolutions added since 2026-10-01 (EvolutionBuilder); WeaponManager adds them to the scene's list.")]
        public SoulHunter.Gameplay.Combat.WeaponEvolutionData[] Evolutions;
        [UnityEngine.Tooltip("Floor tile per stage (index 0 = Level 1), used by InfiniteMap.")]
        public GameObject[] StageFloors;
        /// <summary>VS light source: the Soul Lantern, a breakable prop that drops an item (LightSourceSpawner).</summary>
        public GameObject SoulLantern;
        /// <summary>VS pressure plate: the Rune Plate, which starts a stage event when stepped on (StageItemSpawner).</summary>
        public GameObject RunePlate;
        /// <summary>The Ruined Castle corridor wall module (20 units long), laid by CorridorWalls.</summary>
        public GameObject CastleWall;
        [UnityEngine.Header("Light-source pickups (placeholder art, ExtraPickupBuilder)")]
        [UnityEngine.Tooltip("Soul Toll: kills every non-boss enemy on screen (VS screen wipe).")]
        public GameObject ScreenWipeItem;
        [UnityEngine.Tooltip("Hellfire Breath: the hero breathes fire for 10 s (VS fire breath).")]
        public GameObject FireBreathItem;
        [UnityEngine.Tooltip("Luck Charm: +10% Luck for the run (VS luck pickup).")]
        public GameObject LuckCharmItem;
        [UnityEngine.Tooltip("Gilded Charm: gathers all coins and starts a Gold Fever (VS gold pickup).")]
        public GameObject GildedCharmItem;
        [UnityEngine.Tooltip("Flame projectile Hellfire Breath fires (the Fire Wand's fireball).")]
        public GameObject FireBreathProjectile;
        [UnityEngine.Header("Stage hazards (StageHazardBuilder; the rail cart is placeholder art)")]
        [UnityEngine.Tooltip("Stage 3 rail with its cart (VS dairy cart), laid by StageHazards.")]
        public GameObject RailCart;
        [UnityEngine.Tooltip("Soul Meteor ground marker (VS Shooting Star): the VFX v2 magic circle on a flat quad.")]
        public GameObject HazardMarker;
        [UnityEngine.Tooltip("Bullet the Pile Assault enemies shoot (the Magic Wand bolt, tinted at runtime).")]
        public GameObject HazardBullet;
        [UnityEngine.Tooltip("Level 3 Wandering Merchant (model, trigger and shop), spawned by CampaignSignatureSystem.")]
        public GameObject WanderingMerchant;
        [UnityEngine.Header("Chest opening screen (approved VFX v2 sprites)")]
        [UnityEngine.Tooltip("Rotating light behind an opening 3- or 5-item chest.")]
        public UnityEngine.Texture2D ChestFxCircle;
        [UnityEngine.Tooltip("Ring that bursts out when the chest opens.")]
        public UnityEngine.Texture2D ChestFxRing;
        [UnityEngine.Tooltip("Flash when the chest lands and opens.")]
        public UnityEngine.Texture2D ChestFxFlash;
        [UnityEngine.Tooltip("Stars behind each revealed item, and the 5-item fireworks.")]
        public UnityEngine.Texture2D ChestFxSparkle;

        [UnityEngine.Header("Reward icons (Bug MIS B10, 2026-09-30)")]
        [UnityEngine.Tooltip("Gold Pouch level-up card and the chest's coin-bag card: the approved gold Soul Coin, rendered.")]
        public UnityEngine.Sprite GoldPouchIcon;
        [UnityEngine.Tooltip("Roast Chicken level-up card.")]
        public UnityEngine.Sprite RoastChickenIcon;

        /// <summary>An icon for a PowerUp that has no matching passive item (Power Up shop).</summary>
        [System.Serializable]
        public struct PowerUpIcon
        {
            public SoulHunter.Core.Persistence.PowerUpId PowerUp;
            public UnityEngine.Sprite Icon;
        }

        [UnityEngine.Header("Power Up shop icons (owner, 2026-10-07; PowerUpIconImporter)")]
        [UnityEngine.Tooltip("Greed, Omni, Charm, Defang, Reroll, Skip, Banish and Preserve: Soul Hunter's own badges " +
            "(ArtSource/Generators/build_powerup_icons.py). The others show their passive item's icon.")]
        public PowerUpIcon[] PowerUpIcons;

        /// <summary>The shop icon made for <paramref name="id"/>, or null when it uses its passive item's icon.</summary>
        public UnityEngine.Sprite PowerUpIconFor(SoulHunter.Core.Persistence.PowerUpId id)
        {
            if (PowerUpIcons == null) return null;
            foreach (var entry in PowerUpIcons) if (entry.PowerUp == id) return entry.Icon;
            return null;
        }

        /// <summary>
        /// The game's UI theme: the single source of truth for every colour, picture, icon, font, motion and spacing of
        /// the UI (owner, 2026-10-04). Designers switch and edit it in Soul Hunter > Theme Studio.
        /// </summary>
        public SoulHunter.Gameplay.UI.UiThemeAsset UiTheme;
        /// <summary>
        /// A look waiting for the owner's approval (2026-10-08: the "pro" style sample). Only the style sample screen
        /// shows it; the game keeps <see cref="UiTheme"/> until the owner approves.
        /// </summary>
        public SoulHunter.Gameplay.UI.UiThemeAsset UiThemePreview;
        public static GameContentCatalog Load() => Resources.Load<GameContentCatalog>("GameContent");

        /// <summary>The weapon/passive definition of <paramref name="type"/>, or null when none exists.</summary>
        public static WeaponDefinition FindWeaponDefinition(UpgradeData.UpgradeType type) => WeaponDefinitionLookup.Find(type);

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
