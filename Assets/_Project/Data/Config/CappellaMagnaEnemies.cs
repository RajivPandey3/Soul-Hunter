using System.Collections.Generic;
using static SoulHunter.Gameplay.Data.StageEnemyType;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// Levels 8, 9 and 10 (The Blood Arenas, The Throne of Death, The Soul Core): VS Cappella Magna's waves
    /// (vampire.survivors.wiki/w/Cappella_Magna "Waves", raw, fetched 2026-10-09) and each enemy's statbox
    /// (Fake_Fallen_Cherub, Fake_Fallen_Cherubbello, Reaper_Trainee, Tetrabrachia, Succubus_(enemy), Archon_Spada,
    /// Archon_Fiamma, Archon_Rame, Archon_Oro, Demon_Priest, Demon_Beast, Archdemon, Stage_Killer, Fallen_Cherub,
    /// Fallen_Cherubbello, Fallen_Throne, Trinacria). Owner (2026-10-09): the three last levels share Cappella Magna, each
    /// with its own names (its theme) and its own models; the stats and minutes are the same.
    /// - "0:00 | Fallen Angel | 20 | 1.0" ... "29:00 | Elite Reaper Trainee | 40 | 0.1". The 30:00+ row (Red Reaper Trainee and
    ///   The Reaper) is not used: Soul Hunter has no Reaper (owner decision); Level 10 holds the 29:00 row past 30:00.
    /// - Variants by sprite and link: 2:00 the second "Fake Fallen Cherub" sprite is "Elite Fallen Angel" (Fake Fallen
    ///   Cherub 2, which the Appearances table lists at 2, 3, 4, 7 and 8:00); "Reaper Trainee" sprite at 3-5:00 is the
    ///   yellow one and at 8:00 "Gray Reaper Trainee"; 14:00 "Giant Green Knight" is the Archon Rame sprite; 26:00 "Elite
    ///   Stagekiller" is Stage Killer 2; 27:00 "Small Trinacria" is the non-boss Trinacria (res-defang 1); 28:00 the four
    ///   colossi are the Fake Fallen Cherubbello, Succubus and Archdemon boss statboxes plus Archon Rame; 29:00 "Elite
    ///   Reaper Trainee" is the Reaper Trainee statbox (TRAINEE_ANY, 80 x Level, res-freeze 1).
    /// - Owner signature for Level 8 ("Elite waves replace ordinary swarms"): every Level 8 type is built on the level's
    ///   elite model (StageEnemyTable.AllEliteModel). The VS stats are kept, so the waves are VS's; their look is the
    ///   arena's elite guard. Level 8's map events still send only elites (EnemySpawner.PickSwarmPrefab).
    /// </summary>
    public static class CappellaMagnaEnemies
    {
        /// <summary>The Cappella Magna statboxes under neutral ids; each level gives them its own names.</summary>
        private static StageEnemyType[] BaseTypes() => new[]
        {
            T("WingedThrall",      "", "Fake Fallen Cherub 1",           5,   false, 4,  140, 1f,   3f, 1f,    0.12f),
            T("WingedVeteran",     "", "Fake Fallen Cherub 2",           15,  false, 6,  100, 1f,   3f, 2f,    0.10f),
            T("WingedWarden",      "", "Fake Fallen Cherubbello",        150, false, 7,  100, 0f,   1f, 3f,    0.08f),
            T("AcolyteYellow",     "", "Reaper Trainee (yellow)",        5,   false, 5,  140, 1f,   3f, 1f,    0.15f),
            T("AcolyteBlue",       "", "Reaper Trainee (blue)",          5,   false, 5,  140, 1f,   3f, 1f,    0.60f),
            T("AcolyteGray",       "", "Reaper Trainee (gray)",          15,  false, 5,  140, 1f,   3f, 1.5f,  0.00f),
            T("AcolyteGreen",      "", "Reaper Trainee (green)",         40,  false, 5,  140, 1f,   3f, 1f,    0.33f),
            T("AcolytePurple",     "", "Reaper Trainee (purple)",        180, false, 5,  140, 1f,   3f, 2.5f,  0.78f),
            T("FourArmed",         "", "Tetrabrachia 1",                 70,  false, 8,  100, 1f,   3f, 2.5f,  0.95f),
            T("FourArmedFury",     "", "Tetrabrachia 2",                 150, false, 8,  200, 1f,   3f, 3f,    0.92f),
            T("Temptress",         "", "Succubus",                       250, false, 10, 130, 0.5f, 3f, 2.75f, 0.88f),
            T("Swordsman",         "", "Sword Knight",                   150, false, 10, 120, 0f,   0f, 1.75f, 0.58f),
            T("FlameKnight",       "", "Archon Fiamma",                  150, false, 8,  120, 1f,   3f, 3f,    0.04f),
            T("CopperKnight",      "", "Archon Rame",                    300, false, 11, 100, 0f,   1f, 3f,    0.40f),
            T("GoldKnight",        "", "Archon Oro",                     300, false, 15, 140, 0f,   2f, 2.5f,  0.13f),
            T("Priest",            "", "Demon Priest",                   250, false, 10, 140, 0.5f, 3f, 2.75f, 0.83f),
            T("Beast",             "", "Demon Beast",                    300, false, 15, 160, 1f,   2f, 3f,    0.02f),
            T("GreatBeast",        "", "Demon Beast 2",                  500, false, 15, 150, 0f,   2f, 3f,    0.99f),
            T("Archfiend",         "", "Archdemon",                      700, false, 20, 140, 0f,   2f, 3f,    0.97f),
            T("Executioner",       "", "Stage Killer 1",                 10,  true,  11, 140, 1f,   3f, 3f,    0.70f),
            T("GreatExecutioner",  "", "Stage Killer 2",                 11,  true,  11, 140, 1f,   3f, 3f,    0.72f),
            T("Seraph",            "", "Fallen Cherub",                  300, false, 11, 100, 0f,   1f, 3f,    0.16f),
            T("SwiftSeraph",       "", "Fallen Cherubbello",             250, false, 10, 280, 0.5f, 3f, 2.75f, 0.18f),
            T("Throne",            "", "Fallen Throne",                  300, false, 15, 160, 1f,   2f, 3f,    0.11f),
            T("Triskele",          "", "Trinacria",                      700, false, 15, 140, 0f,   2f, 3f,    0.75f).NoDefang(),
            T("ColossalWarden",    "", "Fake Fallen Cherubbello (boss)", 50,  true,  11, 140, 1f,   3f, 30f,   0.09f),
            T("ColossalTemptress", "", "Succubus (boss)",                60,  true,  11, 140, 1f,   3f, 30f,   0.87f),
            T("ColossalArchfiend", "", "Archdemon (boss)",               80,  true,  11, 140, 1f,   3f, 30f,   0.96f),
            T("EliteAcolyte",      "", "Reaper Trainee (TRAINEE_ANY)",   80,  true,  20, 120, 0f,   1f, 25f,   0.50f).Resists(1f),
            // Chest bosses (StageChestBosses), sent only by the boss schedule.
            T("ColossalEye",       "", "Unknown (boss)",                 60,  true,  11, 200, 0.5f, 3f, 30f,   0.45f),
            T("ColossalCopperKnight", "", "Archon Rame (boss)",          70,  true,  11, 140, 1f,   3f, 30f,   0.41f),
            T("AxeSentinel",       "", "Axe Guardian",                   200, false, 20, 180, 0f,   1f, 40f,   0.03f),
        };

        /// <summary>The chest-boss types: in the table, but in no minute's wave.</summary>
        private static readonly string[] ChestBosses = { "ColossalEye", "ColossalCopperKnight", "AxeSentinel" };

        private static readonly string[][] Minutes =
        {
            /* 0 */ new[] { "WingedThrall" },
            /* 1 */ new[] { "WingedThrall" },
            /* 2 */ new[] { "WingedThrall", "WingedVeteran" },
            /* 3 */ new[] { "AcolyteYellow", "WingedThrall", "WingedVeteran" },
            /* 4 */ new[] { "AcolyteYellow", "WingedThrall", "WingedVeteran" },
            /* 5 */ new[] { "AcolyteYellow", "WingedWarden" },
            /* 6 */ new[] { "AcolyteBlue", "WingedWarden" },
            /* 7 */ new[] { "FourArmed", "WingedVeteran", "AcolyteBlue" },
            /* 8 */ new[] { "FourArmedFury", "AcolyteBlue", "WingedVeteran", "AcolyteGray" },
            /* 9 */ new[] { "Temptress", "AcolyteBlue" },
            /* 10 */ new[] { "Temptress", "AcolyteBlue", "FourArmedFury" },
            /* 11 */ new[] { "Swordsman", "AcolyteGreen" },
            /* 12 */ new[] { "FlameKnight", "Swordsman" },
            /* 13 */ new[] { "FlameKnight" },
            /* 14 */ new[] { "AcolyteGreen", "CopperKnight" },
            /* 15 */ new[] { "GoldKnight", "CopperKnight" },
            /* 16 */ new[] { "Priest", "Beast" },
            /* 17 */ new[] { "Beast", "Priest" },
            /* 18 */ new[] { "Beast", "Priest", "AcolytePurple" },
            /* 19 */ new[] { "Beast", "GreatBeast" },
            /* 20 */ new[] { "Archfiend", "GreatBeast", "AcolytePurple" },
            /* 21 */ new[] { "Executioner" },
            /* 22 */ new[] { "Seraph" },
            /* 23 */ new[] { "SwiftSeraph" },
            /* 24 */ new[] { "Throne" },
            /* 25 */ new[] { "Seraph", "SwiftSeraph", "Throne" },
            /* 26 */ new[] { "GreatExecutioner" },
            /* 27 */ new[] { "Triskele" },
            /* 28 */ new[] { "ColossalWarden", "ColossalTemptress", "CopperKnight", "ColossalArchfiend" },
            /* 29 */ new[] { "EliteAcolyte" },
        };

        public static readonly int[] Minimum = { 20, 30, 40, 40, 25, 10, 30, 60, 50, 30, 20, 100, 20, 200, 150, 50, 80, 40, 80, 80, 100, 60, 200, 200, 200, 100, 250, 150, 10, 40 };
        public static readonly float[] Interval = { 1f, 1f, 0.5f, 0.25f, 0.25f, 1f, 0.5f, 0.5f, 1.5f, 0.5f, 0.5f, 0.5f, 1f, 0.1f, 0.5f, 0.5f, 0.5f, 0.1f, 0.5f, 0.5f, 0.1f, 0.02f, 0.1f, 0.1f, 0.1f, 0.1f, 0.02f, 0.1f, 0.1f, 0.1f };

        /// <summary>Soul Hunter's names per level: the Blood Arenas' gladiators, Death's court, Kael's own broken soul.</summary>
        private static readonly Dictionary<int, Dictionary<string, string>> Names = new Dictionary<int, Dictionary<string, string>>
        {
            [8] = new Dictionary<string, string>
            {
                ["WingedThrall"] = "Arena Imp", ["WingedVeteran"] = "Veteran Arena Imp", ["WingedWarden"] = "Pit Warden",
                ["AcolyteYellow"] = "Gold Initiate", ["AcolyteBlue"] = "Blue Initiate", ["AcolyteGray"] = "Gray Initiate",
                ["AcolyteGreen"] = "Green Initiate", ["AcolytePurple"] = "Purple Initiate",
                ["FourArmed"] = "Four-Armed Gladiator", ["FourArmedFury"] = "Four-Armed Champion", ["Temptress"] = "Blood Siren",
                ["Swordsman"] = "Arena Swordsman", ["FlameKnight"] = "Brand Knight", ["CopperKnight"] = "Bronze Guard",
                ["GoldKnight"] = "Gilded Guard", ["Priest"] = "Blood Priest", ["Beast"] = "War Beast", ["GreatBeast"] = "Great War Beast",
                ["Archfiend"] = "Arena Lord", ["Executioner"] = "Headsman", ["GreatExecutioner"] = "Elite Headsman",
                ["Seraph"] = "Bloodwing", ["SwiftSeraph"] = "Swift Bloodwing", ["Throne"] = "Pit Throne", ["Triskele"] = "Tri-Blade Guard",
                ["ColossalWarden"] = "Colossal Pit Warden", ["ColossalTemptress"] = "Colossal Blood Siren",
                ["ColossalArchfiend"] = "Colossal Arena Lord", ["EliteAcolyte"] = "Champion of the Pit",
                ["ColossalEye"] = "Colossal Arena Eye", ["ColossalCopperKnight"] = "Colossal Bronze Guard", ["AxeSentinel"] = "Arena Axeman",
            },
            [9] = new Dictionary<string, string>
            {
                ["WingedThrall"] = "Death's Page", ["WingedVeteran"] = "Death's Herald", ["WingedWarden"] = "Throne Warden",
                ["AcolyteYellow"] = "Gold Scythe Novice", ["AcolyteBlue"] = "Blue Scythe Novice", ["AcolyteGray"] = "Gray Scythe Novice",
                ["AcolyteGreen"] = "Green Scythe Novice", ["AcolytePurple"] = "Purple Scythe Novice",
                ["FourArmed"] = "Four-Armed Courtier", ["FourArmedFury"] = "Four-Armed Regent", ["Temptress"] = "Throne Consort",
                ["Swordsman"] = "Royal Blade", ["FlameKnight"] = "Pyre Knight", ["CopperKnight"] = "Bronze Sentinel",
                ["GoldKnight"] = "Golden Sentinel", ["Priest"] = "Funeral Priest", ["Beast"] = "Throne Hound",
                ["GreatBeast"] = "Great Throne Hound", ["Archfiend"] = "Death's Vizier", ["Executioner"] = "Royal Executioner",
                ["GreatExecutioner"] = "High Executioner", ["Seraph"] = "Ashen Seraph", ["SwiftSeraph"] = "Swift Ashen Seraph",
                ["Throne"] = "Bone Throne", ["Triskele"] = "Three-Crowned Guard",
                ["ColossalWarden"] = "Colossal Throne Warden", ["ColossalTemptress"] = "Colossal Throne Consort",
                ["ColossalArchfiend"] = "Colossal Vizier", ["EliteAcolyte"] = "Scythe Adept",
                ["ColossalEye"] = "Colossal Throne Eye", ["ColossalCopperKnight"] = "Colossal Bronze Sentinel", ["AxeSentinel"] = "Throne Axeman",
            },
            [10] = new Dictionary<string, string>
            {
                ["WingedThrall"] = "Soul Shard", ["WingedVeteran"] = "Cracked Soul Shard", ["WingedWarden"] = "Soul Warden",
                ["AcolyteYellow"] = "Gold Fragment", ["AcolyteBlue"] = "Blue Fragment", ["AcolyteGray"] = "Gray Fragment",
                ["AcolyteGreen"] = "Green Fragment", ["AcolytePurple"] = "Purple Fragment",
                ["FourArmed"] = "Four-Armed Regret", ["FourArmedFury"] = "Four-Armed Rage", ["Temptress"] = "Lost Memory",
                ["Swordsman"] = "Echo Blade", ["FlameKnight"] = "Burning Guilt", ["CopperKnight"] = "Rusted Vow",
                ["GoldKnight"] = "Golden Vow", ["Priest"] = "Hollow Prayer", ["Beast"] = "Inner Beast",
                ["GreatBeast"] = "Great Inner Beast", ["Archfiend"] = "Core Tyrant", ["Executioner"] = "Self-Judgement",
                ["GreatExecutioner"] = "Final Judgement", ["Seraph"] = "Fallen Hope", ["SwiftSeraph"] = "Swift Fallen Hope",
                ["Throne"] = "Crystal Throne", ["Triskele"] = "Three-Faced Doubt",
                ["ColossalWarden"] = "Colossal Soul Warden", ["ColossalTemptress"] = "Colossal Lost Memory",
                ["ColossalArchfiend"] = "Colossal Core Tyrant", ["EliteAcolyte"] = "Pure Fragment",
                ["ColossalEye"] = "Colossal Watching Doubt", ["ColossalCopperKnight"] = "Colossal Rusted Vow", ["AxeSentinel"] = "Axe of Regret",
            },
        };

        private static readonly Dictionary<int, StageEnemyTable> Tables = new Dictionary<int, StageEnemyTable>();

        /// <summary>The table of Level 8, 9 or 10.</summary>
        public static StageEnemyTable Table(int level)
        {
            if (Tables.TryGetValue(level, out var table)) return table;
            if (!Names.TryGetValue(level, out var names)) throw new KeyNotFoundException($"Cappella Magna serves Levels 8-10, not {level}.");
            var types = BaseTypes();
            for (int i = 0; i < types.Length; i++) types[i] = types[i].Named(names[types[i].Id]);
            table = new StageEnemyTable
            {
                Level = level, Source = "Cappella_Magna", Folder = $"Level{level:00}", Types = types,
                ByMinute = Minutes, Minimum = Minimum, Interval = Interval, AllEliteModel = level == 8, BossOnly = ChestBosses,
            };
            Tables[level] = table;
            return table;
        }
    }
}
