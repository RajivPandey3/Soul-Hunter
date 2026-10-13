using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// One VS wave enemy type for Levels 2-10 (release item P2-02, owner 2026-10-09): the wiki statbox of the VS enemy it
    /// follows, under Soul Hunter's own name, drawn as the level's own enemy or elite model, tinted and resized (the Level 1
    /// method, MadForestEnemies).
    /// - Health, "HP x Level", damage ("Power"), knockback taken and its cap, resistances and movement skills are the
    ///   statbox values (raw pages fetched 2026-10-09).
    /// - Move speed is the VS value x <see cref="EnemyAbilityDefaults.VsSpeedToUnits"/> (VS 100 = 3 units/s), as on Level 1.
    /// - XP is the VS value x 10, rounded (Soul Hunter's gem scale, MadForestEnemies). VS XP 0 (Spicy Meatball, Maronna,
    ///   Hand of Glory) becomes Soul Hunter's smallest gem, 1, because EnemyData.ExperienceValue is at least 1.
    /// - Size and model (balance method, the wiki has none for Soul Hunter's models): types up to 5 HP are drawn at 0.8,
    ///   up to 20 at 0.9, up to 100 at 1.0, up to 300 at 1.1, heavier at 1.25 and the "HP x Level" colossi (40 or more
    ///   per level) at 1.35; types with 150 HP or more and the colossi use the level's bigger elite model.
    /// </summary>
    public struct StageEnemyType
    {
        public string Id;           // asset and prefab name part
        public string Name;         // Soul Hunter's name (shown in the game)
        public string VsVariant;    // the wiki statbox it follows (for the record; never shown)
        public float Health;        // VS "health"; with HealthTimesLevel, health per player level (Skullone (scaling) 6.5)
        public bool HealthTimesLevel;
        public int Damage;
        public float VsSpeed;
        public float Knockback;
        public float MaxKnockback;
        public float VsXp;
        public bool EliteModel;
        public float Scale;
        public Color Tint;
        public EnemyMovement Movement;
        public float FreezeResistance;
        public bool InstantKillResistance;
        public bool DebuffResistance;
        public bool DefangResistance;
        public int Lives;
        public float ShootInterval;  // > 0: shoots a Fixed Direction bullet every this many seconds
        public float ShootVsSpeed;
        public int ShootDamage;

        public float MoveSpeed => VsSpeed * EnemyAbilityDefaults.VsSpeedToUnits;
        public int ExperienceValue => Mathf.Max(1, Mathf.RoundToInt(VsXp * 10f));
        public bool Shoots => ShootInterval > 0f;
        public int MaxHealth => Mathf.Max(1, Mathf.RoundToInt(Health));
        public int HealthPerLevelWhole => HealthTimesLevel ? Mathf.FloorToInt(Health) : 0;
        public float HealthPerLevelFraction => HealthTimesLevel ? Health - Mathf.FloorToInt(Health) : 0f;

        /// <summary>A statbox: id, own name, VS variant, health, HP x Level, damage, VS speed, knockback, cap, VS XP, hue.</summary>
        public static StageEnemyType T(string id, string name, string vs, float health, bool timesLevel, int damage,
            float vsSpeed, float knockback, float maxKnockback, float vsXp, float hue)
        {
            bool colossus = timesLevel && health >= 40f;
            float scale = colossus ? 1.35f : health <= 5f ? 0.8f : health <= 20f ? 0.9f : health <= 100f ? 1f
                        : health <= 300f ? 1.1f : 1.25f;
            return new StageEnemyType
            {
                Id = id, Name = name, VsVariant = vs, Health = health, HealthTimesLevel = timesLevel, Damage = damage,
                VsSpeed = vsSpeed, Knockback = knockback, MaxKnockback = maxKnockback, VsXp = vsXp,
                EliteModel = colossus || health >= 150f, Scale = scale,
                Tint = Color.HSVToRGB(Mathf.Repeat(hue, 1f), 0.45f, 0.92f),
                Movement = EnemyMovement.Homing, Lives = 1,
            };
        }

        // Statbox skills and resistances.
        public StageEnemyType Fixed() { Movement = EnemyMovement.FixedDirection; return this; }
        public StageEnemyType FloatyFixed() { Movement = EnemyMovement.FloatyFixedDirection; return this; }
        public StageEnemyType Resists(float freeze, bool kill = false, bool debuff = false)
        {
            FreezeResistance = freeze; InstantKillResistance = kill; DebuffResistance = debuff; return this;
        }
        public StageEnemyType NoDefang() { DefangResistance = true; return this; }
        public StageEnemyType WithLives(int lives) { Lives = lives; return this; }
        public StageEnemyType Shooting(float interval, float vsBulletSpeed, int bulletDamage)
        {
            ShootInterval = interval; ShootVsSpeed = vsBulletSpeed; ShootDamage = bulletDamage; return this;
        }
        public StageEnemyType Named(string name) { Name = name; return this; }
    }

    /// <summary>One level's VS wave table: its types, the enemies of each minute, and each minute's minimum and interval.</summary>
    public sealed class StageEnemyTable
    {
        public int Level;
        public string Source;        // the VS stage page the waves follow
        public string Folder;        // "Level02": data and prefab sub-folder
        public StageEnemyType[] Types;
        public string[][] ByMinute;  // minutes 0-29: type ids
        /// <summary>Types the wiki's "Enemies" column never lists: they come only as wave bosses (ChestlessWaveBosses).</summary>
        public string[] BossOnly = new string[0];
        public int[] Minimum;
        public float[] Interval;
        /// <summary>Owner signature (AGENTS.md Part 2): this level's normal enemies are Floaty (L2 and L7).</summary>
        public bool AllFloaty;
        /// <summary>Owner signature (Part 5): L7's enemies are ghosts and "VS ghosts take none" (no knockback).</summary>
        public bool NoKnockback;
        /// <summary>Owner signature: L8's waves are elite-class (every type on the level's elite model).</summary>
        public bool AllEliteModel;

        public string DataFolder => $"Assets/_Project/Data/Config/Enemies/{Folder}";
        public string PrefabFolder => $"Assets/Prefabs/Enemies/Gameplay/{Folder}";
        public string DataPath(string id) => $"{DataFolder}/L{Level:00}_{id}_Data.asset";
        public string PrefabName(string id) => $"SH10_L{Level:00}_{id}_Gameplay";
        public string PrefabPath(string id) => $"{PrefabFolder}/{PrefabName(id)}.prefab";

        public StageEnemyType Find(string id)
        {
            foreach (var type in Types) if (type.Id == id) return type;
            throw new KeyNotFoundException($"Level {Level}: {id}");
        }

        /// <summary>The movement the built enemy has: the statbox skill, or Floaty on a Floaty level for homing types.</summary>
        public EnemyMovement MovementOf(StageEnemyType type) =>
            AllFloaty && type.Movement == EnemyMovement.Homing ? EnemyMovement.Floaty : type.Movement;

        public float KnockbackOf(StageEnemyType type) => NoKnockback ? 0f : type.Knockback;
        public bool EliteModelOf(StageEnemyType type) => AllEliteModel || type.EliteModel;

        /// <summary>(first minute, prefab paths) rows for <see cref="WaveTables.EnemyTypesByStage"/>.</summary>
        public List<(int, string[])> Rows()
        {
            var rows = new List<(int, string[])>();
            for (int minute = 0; minute < ByMinute.Length; minute++)
                rows.Add((minute, System.Array.ConvertAll(ByMinute[minute], PrefabPath)));
            return rows;
        }
    }

    /// <summary>Every level's VS wave table, Levels 2-10 (Level 1 is MadForestEnemies).</summary>
    public static class StageEnemyTables
    {
        public static readonly StageEnemyTable[] All =
        {
            InlaidLibraryEnemies.Table,
            DairyPlantEnemies.Table,
            GalloTowerEnemies.Table,
            AbyssFoscariEnemies.Table,
            MtMoonspellEnemies.Table,
            LakeFoscariEnemies.Table,
            CappellaMagnaEnemies.Table(8),
            CappellaMagnaEnemies.Table(9),
            CappellaMagnaEnemies.Table(10),
        };

        public static StageEnemyTable For(int level)
        {
            foreach (var table in All) if (table.Level == level) return table;
            return null;
        }

        /// <summary>True for a built Level 2-10 type asset (its own data folder), not a level's enemy/elite/boss data.</summary>
        public static bool IsTypeAsset(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            path = path.Replace('\\', '/');
            foreach (var table in All)
                if (path.StartsWith(table.DataFolder + "/") || path.StartsWith(table.PrefabFolder + "/")) return true;
            return false;
        }
    }
}
