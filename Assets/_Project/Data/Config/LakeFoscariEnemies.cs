using static SoulHunter.Gameplay.Data.StageEnemyType;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// Level 7 (The Sea of Lost Souls): VS Lake Foscari's waves (vampire.survivors.wiki/w/Lake_Foscari "Waves", raw, fetched
    /// 2026-10-09) and each enemy's statbox (Chompo, Brownie, Ceffoose, Hill_Trow, Sea_Trow, Wiseparke, Nutmeg, Vulvio,
    /// Lammuga, Njuggles, Sam_the_Sundown_Clown, Notadam, Crocifriggitore, Missing_Church, Green_Knight,
    /// Ghostly_Apparition, Average_Hag, Maronna, Hand_of_Glory). Owner (2026-10-09): Level 7's source stage is Lake
    /// Foscari. Names are Soul Hunter's own (the drowned souls who gave up).
    /// - "0:00 | Chompo | 50 | 3.0" ... "29:00 | Maronna, Hand of Glory | 200 | 0.1".
    /// - Sprite variants by their link: Chompo-2 = Red Chompo, Green Knight-2 = Red Knight, Green Knight-4 / -5 = Big Green
    ///   Knight / Big Red Knight, Wiseparke-2 / -3 = Big / Pink Wiseparke, Missing Church-2 = Missing Church 2.
    /// - Owner signatures kept (AGENTS.md section 5 and Part 2/5): every Level 7 enemy is spectral (EnemySpawner, by the
    ///   level's signature), Floaty, and takes no knockback ("VS ghosts take none"); each statbox's knockback cap is kept.
    /// - 22:00 minimum: the page writes "0.8" (with interval "1.0"), which is not an enemy count. Balance method (owner
    ///   rule 2026-10-09): 80, the cell read as a hundredfold slip, between its neighbours 100 (21:00) and 120 (23:00).
    /// - 25:00 minimum "1 (150)": the page explains it ("After a while, the enemy minimum is increased to 150"), tied to the
    ///   Avatar of Gaea, which clears the screen when it arrives. Soul Hunter keeps its own Level 7 boss and does not clear
    ///   the screen, so the minimum is the 150 it settles at.
    /// </summary>
    public static class LakeFoscariEnemies
    {
        public static readonly StageEnemyTable Table = new StageEnemyTable
        {
            Level = 7, Source = "Lake_Foscari", Folder = "Level07", AllFloaty = true, NoKnockback = true,
            Types = new[]
            {
                T("TidalImp",              "Tidal Imp",                "Chompo",               8,   false, 5,  140, 1f,   3f, 1f,   0.50f),
                T("FoamMote",              "Foam Mote",                "Brownie",              1,   false, 2,  160, 1f,   3f, 1f,   0.48f),
                T("MournfulShade",         "Mournful Shade",           "Ceffoose",             15,  false, 3,  140, 1f,   3f, 2f,   0.62f),
                T("ShoreWraith",           "Shore Wraith",             "Hill Trow",            10,  false, 5,  140, 1f,   3f, 1f,   0.45f),
                T("DeepWraith",            "Deep Wraith",              "Sea Trow",             70,  false, 8,  100, 0.3f, 3f, 2f,   0.58f),
                T("SoulMinnow",            "Soul Minnow",              "Wiseparke",            10,  false, 5,  200, 0f,   3f, 1.5f, 0.52f),
                T("GreatSoulMinnow",       "Great Soul Minnow",        "Big Wiseparke",        20,  false, 5,  200, 0f,   3f, 1.5f, 0.54f),
                T("PaleSoulMinnow",        "Pale Soul Minnow",         "Pink Wiseparke",       30,  false, 5,  200, 0f,   3f, 1.5f, 0.88f),
                T("SealskinSpirit",        "Sealskin Spirit",          "Nutmeg",               4,   false, 3,  200, 1f,   3f, 2f,   0.60f),
                T("TideHowler",            "Tide Howler",              "Vulvio",               150, false, 8,  100, 0.2f, 3f, 2f,   0.65f),
                T("WeepingLamb",           "Weeping Lamb",             "Lammuga",              200, false, 1,  100, 0f,   1f, 2f,   0.15f),
                T("KelpSteed",             "Kelp Steed",               "Njuggles",             70,  false, 5,  160, 0.3f, 3f, 2f,   0.40f),
                T("GrinningMourner",       "Grinning Mourner",         "Sam the Sundown Clown", 220, false, 14, 130, 0.8f, 3f, 3f,  0.80f),
                T("HollowSailor",          "Hollow Sailor",            "Notadam",              13,  false, 4,  160, 1f,   3f, 4f,   0.56f),
                T("DrownedPenitent",       "Drowned Penitent",         "Crocifriggitore",      220, false, 14, 130, 0.4f, 3f, 3f,   0.68f),
                T("SunkenBell",            "Sunken Bell",              "Missing Church",       130, false, 8,  200, 1f,   3f, 4f,   0.47f),
                T("GreatSunkenBell",       "Great Sunken Bell",        "Missing Church 2",     200, false, 9,  200, 1f,   3f, 4f,   0.46f),
                T("BloodTideImp",          "Blood Tide Imp",           "Red Chompo",           4,   true,  3,  200, 1f,   3f, 2f,   0.97f),
                T("CrimsonDrownedKnight",  "Crimson Drowned Knight",   "Red Knight",           500, false, 20, 100, 1f,   3f, 3f,   0.99f),
                T("KelpKnight",            "Kelp Knight",              "Green Knight",         500, false, 20, 180, 0.5f, 3f, 3f,   0.38f),
                T("GrievingApparition",    "Grieving Apparition",      "Ghostly Apparition",   5,   true,  5,  160, 0f,   1f, 1f,   0.55f).Resists(2f, debuff: true),
                T("WailingApparition",     "Wailing Apparition",       "Ghostly Apparition 2", 5,   true,  5,  200, 0f,   1f, 1f,   0.57f).Resists(2f, debuff: true),
                T("BrineCrone",            "Brine Crone",              "Average Hag",          700, false, 15, 140, 0f,   2f, 3f,   0.42f),
                T("GreatCrimsonDrownedKnight", "Great Crimson Drowned Knight", "Big Red Knight", 80, true, 20, 120, 0f, 1f, 25f, 0.98f).Resists(1f),
                T("GreatKelpKnight",       "Great Kelp Knight",        "Big Green Knight",     80,  true,  20, 120, 0f,   1f, 25f,  0.37f).Resists(1f),
                T("MotherOfTides",         "Mother of Tides",          "Maronna",              900, false, 15, 140, 0.1f, 2f, 0f,   0.63f),
                T("DrownedHand",           "Drowned Hand",             "Hand of Glory",        900, false, 15, 190, 0.1f, 2f, 0f,   0.51f),
                T("TideFairy", "Tide Fairy", "Foscaritrice", 40f, true, 10, 170, 0.8f, 3f, 3f, 0.48f),
                T("ColossalTideFairy", "Colossal Tide Fairy", "Colossal Foscaritrice", 40f, true, 10, 170, 0.8f, 3f, 3f, 0.46f),
                T("ArcanaTideFairy", "Arcana Tide Fairy", "Arcana Foscaritrice", 40f, true, 10, 170, 0.8f, 3f, 3f, 0.76f),
                T("GreatSealskinSpirit", "Great Sealskin Spirit", "Nutmeg Boss", 50f, true, 10, 170, 1f, 3f, 30f, 0.52f),
                T("GreatKelpSteed", "Great Kelp Steed", "Njuggles Boss", 50f, true, 10, 170, 0.3f, 3f, 30f, 0.4f),
                T("GreatGrinningMourner", "Great Grinning Mourner", "Sam the Boss Clown", 40f, true, 10, 170, 0.8f, 3f, 30f, 0.9f),
                T("GreatTideHowler", "Great Tide Howler", "Vulvio Boss", 150f, true, 20, 160, 0.5f, 1f, 30f, 0.6f).Resists(1.1f),
                T("GreatWeepingLamb", "Great Weeping Lamb", "Lammuga Boss", 150f, true, 30, 200, 0f, 1f, 30f, 0.15f),
                T("GreatDrownedBell", "Great Drowned Bell", "Missing Church Boss", 200f, true, 30, 160, 0f, 1f, 30f, 0.65f).Resists(1.0f),
                T("DeepKelpKnight", "Deep Kelp Knight", "Deep Green Knight", 200f, true, 20, 180, 0f, 1f, 30f, 0.35f),
                // Map event units (MapEvents; statboxes on Sammy_the_Caterpillar, Wiseparke, Sam_the_Sundown_Clown and Snek, raw
                // 2026-10-28). Level 7's owner signatures (spectral, no knockback) apply to them as to every Level 7 type.
                T("DrownedCaterpillar", "Drowned Caterpillar", "Sammy the Caterpillar", 10f,  true,  10, 100, 1f,   3f, 1f, 0.40f).Fixed(),
                T("SwimmingWisp",       "Swimming Wisp",       "Swimming Wiseparke",    1f,   false, 1,  240, 1f,   3f, 1f, 0.48f).FloatyFixed(),
                T("StampedeMourner",    "Stampede Mourner",    "Sam the Swarm Clown",   220f, false, 8,  600, 0.1f, 1f, 3f, 0.92f).Fixed(),
                T("SwarmEel",           "Swarm Eel",           "Swarm Snek",            25f,  false, 6,  700, 0f,   3f, 2f, 0.30f).Fixed(),
            },
            // Chest bosses (StageChestBosses): their statboxes, sent only by the boss schedule.
            BossOnly = new[] { "TideFairy", "ColossalTideFairy", "ArcanaTideFairy", "GreatSealskinSpirit", "GreatKelpSteed", "GreatGrinningMourner", "GreatTideHowler", "GreatWeepingLamb", "GreatDrownedBell", "DeepKelpKnight" ,
                // Map event units, sent only by MapEvents.
                "DrownedCaterpillar", "SwimmingWisp", "StampedeMourner", "SwarmEel" },
            ByMinute = new[]
            {
                /* 0 */ new[] { "TidalImp" },
                /* 1 */ new[] { "TidalImp" },
                /* 2 */ new[] { "TidalImp", "FoamMote" },
                /* 3 */ new[] { "MournfulShade" },
                /* 4 */ new[] { "TidalImp", "MournfulShade" },
                /* 5 */ new[] { "FoamMote", "ShoreWraith", "DeepWraith" },
                /* 6 */ new[] { "SoulMinnow", "ShoreWraith", "DeepWraith" },
                /* 7 */ new[] { "SoulMinnow", "ShoreWraith", "DeepWraith" },
                /* 8 */ new[] { "GreatSoulMinnow", "PaleSoulMinnow", "SoulMinnow" },
                /* 9 */ new[] { "MournfulShade", "SoulMinnow", "PaleSoulMinnow" },
                /* 10 */ new[] { "SealskinSpirit", "SoulMinnow", "PaleSoulMinnow" },
                /* 11 */ new[] { "TideHowler", "SealskinSpirit" },
                /* 12 */ new[] { "TideHowler", "WeepingLamb" },
                /* 13 */ new[] { "TideHowler", "KelpSteed" },
                /* 14 */ new[] { "TideHowler", "KelpSteed", "WeepingLamb" },
                /* 15 */ new[] { "GrinningMourner", "KelpSteed" },
                /* 16 */ new[] { "GrinningMourner", "WeepingLamb" },
                /* 17 */ new[] { "GrinningMourner", "HollowSailor" },
                /* 18 */ new[] { "DrownedPenitent", "HollowSailor", "FoamMote" },
                /* 19 */ new[] { "DrownedPenitent", "HollowSailor" },
                /* 20 */ new[] { "GreatSunkenBell", "SunkenBell" },
                /* 21 */ new[] { "BloodTideImp" },
                /* 22 */ new[] { "CrimsonDrownedKnight", "BloodTideImp" },
                /* 23 */ new[] { "CrimsonDrownedKnight", "KelpKnight" },
                /* 24 */ new[] { "KelpKnight", "TidalImp" },
                /* 25 */ new[] { "GrievingApparition", "WailingApparition" },
                /* 26 */ new[] { "KelpKnight" },
                /* 27 */ new[] { "KelpKnight", "BrineCrone" },
                /* 28 */ new[] { "GreatCrimsonDrownedKnight", "GreatKelpKnight" },
                /* 29 */ new[] { "MotherOfTides", "DrownedHand" },
            },
            Minimum = new[] { 50, 80, 100, 60, 110, 100, 30, 80, 80, 200, 100, 120, 80, 120, 300, 100, 100, 200, 60, 120, 100, 100, 80, 120, 200, 150, 100, 100, 100, 200 },
            Interval = new[] { 3f, 3f, 4f, 4f, 4f, 4f, 2f, 2f, 1f, 0.5f, 0.5f, 2f, 1f, 0.5f, 0.1f, 1f, 0.1f, 1f, 0.5f, 0.5f, 0.1f, 0.1f, 1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f },
        };
    }
}
