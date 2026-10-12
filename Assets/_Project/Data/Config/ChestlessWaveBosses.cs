using System.Collections.Generic;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// Wave bosses that carry no chest, level by level. VS stage pages, "Waves" table, column "Bosses & Treasure" (raw
    /// pages Inlaid_Library, Dairy_Plant, Gallo_Tower, Abyss_Foscari, Mt.Moonspell, Lake_Foscari and Cappella_Magna,
    /// fetched 2026-10-13; Mad_Forest 2026-10-12): a boss the cell names with no {{treasure}} after it. One enemy of that
    /// type comes when its minute starts (EnemySpawner.SpawnChestlessBoss); like every boss it is brought back to the screen
    /// instead of being left behind.
    ///
    /// Not here, on purpose:
    /// - bosses WITH {{treasure}}: Soul Hunter's own chest-elite minutes cover them (owner, 2026-09-27);
    /// - the 25:00 stage boss of every level and Level 10's Shadow Kael (the owner's);
    /// - Arcana holders (they are the Arcana chest elites).
    ///
    /// The rows, as the wiki writes them, and the Soul Hunter type that follows the same statbox (StageEnemyTables):
    /// - Level 2, Inlaid Library: none. Every boss cell has a {{treasure}} (20:00 Nesuferit's is on the next line).
    /// - Level 3, Dairy Plant: "24:00 ... {{Sprite|Sword Guardian|medium}} [[Giant Armored Knight]]", no treasure. The
    ///   Sword_Guardian page lists "Sword Guardian | Dairy Plant | 24:00 | Boss" under the plain statbox (700 HP), which is
    ///   Ruined Knight.
    /// - Level 4, Gallo Tower: "1:00 ... {{Sprite|Scarleton|medium}}[[Scarleton]]" ("It cannot drop a Treasure Chest",
    ///   Scarleton page): Thrice-Risen. "4:00 ... {{Sprite|Undead Mage|medium}}[[Undead Mage]]": Gate Sorcerer.
    /// - Level 5, Abyss Foscari: "24:00 ... {{Sprite|Maronna Meea|...|Maronna Meea Boss}}", no treasure. The
    ///   Maronna_Meea page gives that boss its own statbox ("Maronna Meea Boss ... skills = HP x Level, health = 200,
    ///   movespeed = 180, damage = 20, knockback = 0, knockback-max = 1, xp = 30"). Soul Hunter has no type with that
    ///   statbox, and a new type needs its asset built in the editor. Until then the nearest Level 5 type by health
    ///   stands in: Great Marshfire Skull (VS Flaming Skull Boss: HP x Level 100, movespeed 180, damage 20, knockback 0,
    ///   cap 1, xp 30), the same numbers but half the health per level and without the freeze/kill/debuff resistances.
    ///   This is outside the 5% rule and is recorded as a gap (owner / next builder run).
    /// - Level 6, Mt.Moonspell: "8:00 ... {{Sprite|Thunderous Oni}}", no treasure: Thunder Ogre. "21:00 ...
    ///   {{Sprite|Tsuchigumo}} {{Sprite|Spiritello-4|link=Spiritello Boss}} {{treasure ... reward1=arcana}}" with the note
    ///   "The Spiritello Boss boss is an Arcana holder ... the chest dropped by it only awards an Arcana", so the
    ///   Tsuchigumo carries nothing (Tsuchigumo page: "21:00 | Boss"): Glacier Spider. "24:00 ...
    ///   {{Sprite|Tsuchigumo-2|link=Tsuchigumo Boss}}", no treasure: Great Glacier Spider.
    /// - Level 7, Lake Foscari: none (see the two-boss rows below).
    /// - Levels 8-10, Cappella Magna: none. Every boss cell has a {{treasure}} (18:00 Axe Guardian's is on the next line).
    ///
    /// Two-boss rows with one {{treasure}} and no note saying who carries it (Mt.Moonspell 10:00 "Windy Oni, Thunderous
    /// Oni, {{treasure}}"; Lake Foscari 20:00 "Missing Church Boss 2, Missing Church Boss, {{treasure}}"): the wiki does not
    /// say whether one of the pair is chest-less. Balance method (owner rule 2026-10-09): they are read as rows with
    /// treasure, which Soul Hunter's 10:00 and 20:00 chest elites already cover; no second boss is added there.
    /// </summary>
    public static class ChestlessWaveBosses
    {
        /// <summary>One chest-less boss: the level, its minute, the Soul Hunter type id and the VS enemy (for the record; never shown).</summary>
        public struct Row
        {
            public int Level;
            public int Minute;
            public string Id;
            public string VsEnemy;

            public Row(int level, int minute, string id, string vsEnemy)
            {
                Level = level; Minute = minute; Id = id; VsEnemy = vsEnemy;
            }
        }

        /// <summary>Levels 2-10, sorted by level and minute (Level 1 is <see cref="MadForestEnemies.ChestlessBosses"/>).</summary>
        public static readonly Row[] Rows =
        {
            new Row(3, 24, "RuinedKnight",        "Sword Guardian (Giant Armored Knight)"),
            new Row(4, 1,  "ThriceRisen",         "Scarleton"),
            new Row(4, 4,  "GateSorcerer",        "Undead Mage"),
            new Row(5, 24, "GreatMarshfireSkull", "Maronna Meea Boss (stand-in: Flaming Skull Boss)"),
            new Row(6, 8,  "ThunderOgre",         "Thunderous Oni"),
            new Row(6, 21, "GlacierSpider",       "Tsuchigumo"),
            new Row(6, 24, "GreatGlacierSpider",  "Tsuchigumo Boss"),
        };

        private static readonly (int minute, string id)[] None = new (int minute, string id)[0];
        private static readonly Dictionary<int, (int minute, string id)[]> ByLevel = new Dictionary<int, (int minute, string id)[]>();

        /// <summary>A level's chest-less bosses as (minute, type id), earliest first; empty for a level that has none.</summary>
        public static (int minute, string id)[] For(int level)
        {
            if (level == 1) return MadForestEnemies.ChestlessBosses;
            if (ByLevel.TryGetValue(level, out var cached)) return cached;
            var list = new List<(int minute, string id)>();
            foreach (var row in Rows)
                if (row.Level == level) list.Add((row.Minute, row.Id));
            list.Sort((a, b) => a.minute.CompareTo(b.minute));
            var result = list.Count > 0 ? list.ToArray() : None;
            ByLevel[level] = result;
            return result;
        }

        /// <summary>The gameplay prefab's name of a level's type id (null when the level has no such table).</summary>
        public static string PrefabName(int level, string id)
        {
            if (level == 1) return MadForestEnemies.PrefabName(id);
            var table = StageEnemyTables.For(level);
            return table != null ? table.PrefabName(id) : null;
        }
    }
}
