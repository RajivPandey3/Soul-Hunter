using UnityEngine;
using UpgradeType = SoulHunter.Gameplay.Combat.UpgradeData.UpgradeType;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// The guardians of the four hidden stage items (VS Atlanteans; vampire.survivors.wiki/w/City_Atlantean,
    /// Volcano_Atlantean, Moon_Atlantean and Sun_Atlantean, raw, fetched 2026-10-13). All four share one statbox:
    /// "health = 30,000 | movespeed = 200 | damage = 50 | knockback = 0 | knockback-max = 0 | xp = 0 | res-freeze = 2 |
    /// res-kill = 1 | res-debuff = 1", with no HP x Level skill.
    /// - Who guards what: City Atlantean "Guards the Metaglio Left stage item", Volcano Atlantean Metaglio Right, Moon
    ///   Atlantean Silver Ring, Sun Atlantean Gold Ring, on every stage. Stone Mask's page names no guard, so Miser's Idol
    ///   has none.
    /// - When (Passive_items, "Stage items"): "Each hidden stage item is guarded by an Atlantean that spawns when any
    ///   player gets close to the item. The player does not have to defeat the guard to pick up the item, but they will
    ///   continue to follow the player until defeated. If the stage item was attracted using Mad Groove (VIII), the guard
    ///   will not appear until the item is interacted with."
    /// - Left behind: "It does not despawn, if it leaves the player's screen, but is instead teleported back, like bosses"
    ///   (the spawner's straggler rule already does this for every enemy).
    /// - Reward: "When killed, City Atlantean drops a Golden Egg instead of experience" (Soul Hunter's Soul Egg).
    /// Names, tints and size are Soul Hunter's own; the model is each level's elite model (not owner-approved art).
    /// </summary>
    public static class ItemGuardians
    {
        public const int Health = 30000;          // VS: health 30,000 (no HP x Level)
        public const float VsMoveSpeed = 200f;     // VS: movespeed 200
        public const int Damage = 50;              // VS: damage 50
        public const float Knockback = 0f;         // VS: knockback 0
        public const float MaxKnockback = 0f;      // VS: knockback-max 0
        public const int Experience = 0;           // VS: xp 0 (a Golden Egg instead)
        public const float FreezeResistance = 2f;  // VS: res-freeze 2
        public const bool InstantKillResistance = true; // VS: res-kill 1
        public const bool DebuffResistance = true;      // VS: res-debuff 1
        /// <summary>The level's elite model at this size (Soul Hunter's look; the wave types' largest is 1.35).</summary>
        public const float Scale = 1.3f;
        /// <summary>
        /// How close the hero comes before the guardian appears. The wiki says only "gets close to the item"; balance
        /// method: the distance at which a Soul Casket's guards wake (12), a little inside the screen's edge.
        /// </summary>
        public const float WakeDistance = 12f;

        public const string DataFolder = "Assets/_Project/Data/Config/Guardians";
        public const string PrefabFolder = "Assets/Prefabs/Enemies/Gameplay/Guardians";

        public struct Guard
        {
            public string Id;
            public string Name;
            public string VsEnemy;      // for the record; never shown
            public UpgradeType Item;
            public Color Tint;
        }

        public static readonly Guard[] All =
        {
            new Guard { Id = "WardenLeftHand",  Name = "Warden of the Left Hand",  VsEnemy = "City Atlantean",    Item = UpgradeType.MetaglioLeft,    Tint = Color.HSVToRGB(0.55f, 0.45f, 0.92f) },
            new Guard { Id = "WardenRightHand", Name = "Warden of the Right Hand", VsEnemy = "Volcano Atlantean", Item = UpgradeType.MetaglioRight,   Tint = Color.HSVToRGB(0.03f, 0.45f, 0.92f) },
            new Guard { Id = "WardenLongHours", Name = "Warden of Long Hours",     VsEnemy = "Moon Atlantean",    Item = UpgradeType.RingOfLongHours, Tint = Color.HSVToRGB(0.68f, 0.25f, 0.95f) },
            new Guard { Id = "WardenDarkHours", Name = "Warden of Dark Hours",     VsEnemy = "Sun Atlantean",     Item = UpgradeType.RingOfDarkHours, Tint = Color.HSVToRGB(0.13f, 0.45f, 0.95f) },
        };

        /// <summary>The guardian of a stage item, or null when the item has none.</summary>
        public static Guard? For(UpgradeType item)
        {
            foreach (var guard in All) if (guard.Item == item) return guard;
            return null;
        }

        public static string DataPath(string id) => $"{DataFolder}/Guardian_{id}_Data.asset";
        public static string PrefabName(int level, string id) => $"SH10_L{Mathf.Clamp(level, 1, 10):00}_{id}_Gameplay";
        public static string PrefabPath(int level, string id) => $"{PrefabFolder}/{PrefabName(level, id)}.prefab";

        /// <summary>The built gameplay prefab of a guardian on a level (GameContent.ItemGuards), or null before it is built.</summary>
        public static GameObject Prefab(int level, string id)
        {
            var catalog = GameContentCatalog.Load();
            if (catalog == null || catalog.ItemGuards == null) return null;
            string wanted = PrefabName(level, id);
            foreach (var prefab in catalog.ItemGuards)
                if (prefab != null && prefab.name == wanted) return prefab;
            return null;
        }
    }
}
