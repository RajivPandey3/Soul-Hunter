using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// Level 1's enemy types, minute by minute, from vampire.survivors.wiki/w/Mad_Forest "Waves" and each enemy's statbox
    /// (Pipeestrello, Zombie, Skeleton, Ghost, Mudman, Giant_Bat, Werewolf, Mantichana, Big_Mummy, Flower_Wall, Venus; raw
    /// pages fetched 2026-10-03). Owner (2026-10-03): VS stats on the existing Level 1 enemy and elite models, recoloured
    /// and resized, until each type has its own model (Art section).
    /// - Health, damage, knockback taken and its cap are the wiki values.
    /// - Move speed is the VS value x <see cref="EnemyAbilityDefaults.VsSpeedToUnits"/> (VS 100 = 3 units/s, the speed the
    ///   Level 1 enemy already had).
    /// - XP is the VS value x 10, as Soul Hunter's gems are (owner decision 2026-09-30: gem colours at 20 and 90 XP for VS 2
    ///   and 9).
    /// - "HP x Level" (the wiki's skill on Pipeestrello 4/5 and Flower Wall) multiplies health by the player's level.
    /// - Names, colours and sizes are Soul Hunter's own.
    /// </summary>
    public static class MadForestEnemies
    {
        public struct EnemyType
        {
            public string Id;           // asset and prefab name part
            public string Name;         // Soul Hunter's name
            public string VsVariant;    // the wiki statbox it follows (for the record; never shown)
            public int Health;
            public bool HealthTimesLevel;
            public int Damage;
            public float VsSpeed;
            public float Knockback;
            public float MaxKnockback;
            public float VsXp;
            public bool EliteModel;     // the bigger Level 1 elite model, else the Level 1 enemy model
            public float Scale;         // times the base model's size
            public Color Tint;

            public float MoveSpeed => VsSpeed * EnemyAbilityDefaults.VsSpeedToUnits;
            public int ExperienceValue => Mathf.Max(1, Mathf.RoundToInt(VsXp * 10f));
        }

        public static readonly EnemyType[] Types =
        {
            T("GraveBat",        "Grave Bat",            "Pipeestrello 1", 1,   false, 5,  140, 1f,   3f, 1f,   false, 0.8f, new Color(0.62f, 0.58f, 0.70f)),
            T("RedEyeBat",       "Red-Eye Bat",          "Pipeestrello 2", 5,   false, 5,  140, 1f,   3f, 1f,   false, 0.85f, new Color(0.78f, 0.42f, 0.42f)),
            T("BloodEyeBat",     "Blood-Eye Bat",        "Pipeestrello 3", 5,   false, 5,  140, 1f,   3f, 1f,   false, 0.85f, new Color(0.90f, 0.28f, 0.30f)),
            T("EmberBat",        "Ember Bat",            "Pipeestrello 4", 50,  true,  10, 140, 1f,   3f, 30f,  false, 1.1f, new Color(1.00f, 0.78f, 0.35f)),
            T("AshenBat",        "Ashen Bat",            "Pipeestrello 5", 50,  true,  10, 140, 1f,   3f, 30f,  false, 1.1f, new Color(0.85f, 0.88f, 0.95f)),
            T("RestlessCorpse",  "Restless Corpse",      "Zombie",         10,  false, 10, 100, 0.8f, 3f, 1f,   false, 1.0f, new Color(0.55f, 0.70f, 0.48f)),
            T("BoneSoldier",     "Bone Soldier",         "Skeleton",       15,  false, 10, 100, 1f,   3f, 2f,   false, 1.0f, new Color(0.92f, 0.90f, 0.82f)),
            T("GraveWisp",       "Grave Wisp",           "Ghost 1",        10,  false, 5,  200, 0f,   3f, 1.5f, false, 0.9f, new Color(0.62f, 0.85f, 1.00f)),
            T("AshMireFiend",    "Ash Mire Fiend",       "Mudman 1",       70,  false, 10, 100, 0.3f, 3f, 2.5f, true,  0.9f, new Color(0.55f, 0.52f, 0.50f)),
            T("MossMireFiend",   "Moss Mire Fiend",      "Mudman 2",       150, false, 10, 100, 0.3f, 3f, 2.5f, true,  1.0f, new Color(0.42f, 0.62f, 0.36f)),
            T("GreatGraveBat",   "Great Grave Bat",      "Giant Bat",      270, false, 10, 140, 0.1f, 3f, 2.5f, true,  1.1f, new Color(0.45f, 0.38f, 0.58f)),
            T("GhoulHound",      "Ghoul Hound",          "Werewolf",       180, false, 14, 130, 0.8f, 3f, 2f,   true,  1.05f, new Color(0.58f, 0.46f, 0.36f)),
            T("MantisWraith",    "Mantis Wraith",        "Mantichana",     500, false, 20, 80,  0f,   3f, 3f,   true,  1.2f, new Color(0.50f, 0.78f, 0.45f)),
            T("ShroudGiant",     "Shroud Giant",         "Big Mummy",      500, false, 20, 80,  0f,   3f, 3f,   true,  1.25f, new Color(0.86f, 0.80f, 0.62f)),
            T("ThornWall",       "Thorn Wall",           "Flower Wall",    30,  true,  1,  20,  1f,   3f, 2f,   false, 0.9f, new Color(0.40f, 0.70f, 0.40f)),
            T("GraveLily",       "Grave Lily",           "Venus",          500, false, 20, 80,  0f,   3f, 3f,   true,  1.25f, new Color(0.55f, 0.62f, 0.95f)),
        };

        /// <summary>
        /// The wiki's Mad Forest wave enemies per minute (each row lists the types that minute spawns). Sprite variants of
        /// one statbox (Zombie-2, Skeleton-3, Flower Wall-1-2) are the same type.
        /// </summary>
        public static readonly string[][] ByMinute =
        {
            /* 0 */ new[] { "BloodEyeBat" },
            /* 1 */ new[] { "RestlessCorpse", "GraveBat" },
            /* 2 */ new[] { "GraveBat", "RedEyeBat", "BloodEyeBat" },
            /* 3 */ new[] { "BoneSoldier" },
            /* 4 */ new[] { "BoneSoldier", "GraveWisp" },
            /* 5 */ new[] { "MossMireFiend" },
            /* 6 */ new[] { "RestlessCorpse", "MossMireFiend" },
            /* 7 */ new[] { "RedEyeBat", "BloodEyeBat", "AshMireFiend" },
            /* 8 */ new[] { "RestlessCorpse" },
            /* 9 */ new[] { "GreatGraveBat", "RestlessCorpse" },
            /* 10 */ new[] { "AshMireFiend", "MossMireFiend" },
            /* 11 */ new[] { "BoneSoldier" },
            /* 12 */ new[] { "GhoulHound", "GraveWisp", "BoneSoldier" },
            /* 13 */ new[] { "GhoulHound", "GraveWisp" },
            /* 14 */ new[] { "GreatGraveBat", "GhoulHound" },
            /* 15 */ new[] { "GhoulHound", "GreatGraveBat", "MossMireFiend" },
            /* 16 */ new[] { "MantisWraith", "AshMireFiend", "MossMireFiend" },
            /* 17 */ new[] { "ShroudGiant" },
            /* 18 */ new[] { "ShroudGiant", "AshMireFiend" },
            /* 19 */ new[] { "ShroudGiant", "AshMireFiend" },
            /* 20 */ new[] { "ShroudGiant", "MossMireFiend", "GreatGraveBat" },
            /* 21 */ new[] { "ThornWall" },
            /* 22 */ new[] { "ThornWall", "ShroudGiant" },
            /* 23 */ new[] { "ThornWall", "ShroudGiant" },
            /* 24 */ new[] { "ThornWall", "ShroudGiant" },
            /* 25 */ new[] { "GraveLily" },
            /* 26 */ new[] { "GraveLily", "ThornWall" },
            /* 27 */ new[] { "ShroudGiant", "AshMireFiend", "MossMireFiend" },
            /* 28 */ new[] { "GreatGraveBat", "EmberBat" },
            /* 29 */ new[] { "EmberBat", "AshenBat" },
        };

        public const string DataFolder = "Assets/_Project/Data/Config/Enemies/MadForest";
        public const string PrefabFolder = "Assets/Prefabs/Enemies/Gameplay/MadForest";

        public static string DataPath(string id) => $"{DataFolder}/L01_{id}_Data.asset";
        public static string PrefabPath(string id) => $"{PrefabFolder}/SH10_L01_{id}_Gameplay.prefab";

        public static EnemyType Find(string id)
        {
            foreach (var type in Types) if (type.Id == id) return type;
            throw new KeyNotFoundException(id);
        }

        /// <summary>Level 1's (first minute, prefab paths) rows for <see cref="WaveTables.EnemyTypesByStage"/>.</summary>
        public static List<(int, string[])> StageOneRows()
        {
            var rows = new List<(int, string[])>();
            for (int minute = 0; minute < ByMinute.Length; minute++)
                rows.Add((minute, System.Array.ConvertAll(ByMinute[minute], PrefabPath)));
            return rows;
        }

        private static EnemyType T(string id, string name, string vs, int health, bool timesLevel, int damage, float vsSpeed,
            float knockback, float maxKnockback, float vsXp, bool elite, float scale, Color tint) => new EnemyType
        {
            Id = id, Name = name, VsVariant = vs, Health = health, HealthTimesLevel = timesLevel, Damage = damage,
            VsSpeed = vsSpeed, Knockback = knockback, MaxKnockback = maxKnockback, VsXp = vsXp, EliteModel = elite,
            Scale = scale, Tint = tint
        };
    }
}
