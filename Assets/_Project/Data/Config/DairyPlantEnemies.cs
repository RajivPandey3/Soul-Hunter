using static SoulHunter.Gameplay.Data.StageEnemyType;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// Level 3 (The Forgotten Village): VS Dairy Plant's waves (vampire.survivors.wiki/w/Dairy_Plant "Waves", raw, fetched
    /// 2026-10-09) and each enemy's statbox (Merman, Lizard_Pawn, Lizard_Rook, Milk_Elemental, Skeleton, Skeleton_Ninja,
    /// Minotaur, Mignotaur, Twin_Snakes, Twin_Demons, Lost_Twin, Melone, Archon_Ascia, Archon_Lancia, Tritont, Big_Golem,
    /// Jellyfish, Skelewing, Sword_Guardian, Gallotrice, Minotaur_Boss). Names are Soul Hunter's own (a village burned by
    /// demons, GDD).
    /// - "0:00 | Merman | 20 | 1.0" ... "28:00 | Merman Boss, Minotaur Boss | 10 | 0.1", "29:00 | Massive Golem | 10 | 0.1".
    /// - 29:00 "Massive Golem" is two types: the row's sprites Big Golem-3 and Big Golem-4, and Big_Golem's Appearances list
    ///   both "Big Golem 1 (LV29)" and "Big Golem 2 (LV29)" at Dairy Plant 29:00.
    /// - 25:00 and 27:00 "Giant Armored Knight" is the non-boss Sword Guardian statbox (700 HP).
    /// - Stationary shooters (statbox notes): Twin Snakes "Shoots a bullet every 2 seconds" (Bullet: speed 140, power 20);
    ///   Twin Demons "every 1.5 seconds" (Bullet 2: 250, 25); Lost Twin "every second" (Bullet: 300, 40), "immune to
    ///   knockback". Their speed is 0.
    /// - Minotaur Boss (large) has no HP x Level in its statbox ("health = 50"), so it is built with a flat 50.
    /// </summary>
    public static class DairyPlantEnemies
    {
        public static readonly StageEnemyTable Table = new StageEnemyTable
        {
            Level = 3, Source = "Dairy_Plant", Folder = "Level03",
            Types = new[]
            {
                T("CinderPeasant",        "Cinder Peasant",          "Merman 1",              5,   false, 4,  140, 1f,   3f, 1f,    0.06f),
                T("CharredVillager",      "Charred Villager",        "Merman 2",              70,  false, 8,  100, 1f,   3f, 2.5f,  0.04f),
                T("EmberElder",           "Ember Elder",             "Merman 3",              150, false, 8,  100, 1f,   3f, 2.5f,  0.02f),
                T("AshHound",             "Ash Hound",               "Lizard Pawn",           15,  false, 6,  100, 1f,   3f, 2f,    0.10f),
                T("SootBrute",            "Soot Brute",              "Lizard Rook",           120, false, 7,  100, 0f,   1f, 2.5f,  0.62f),
                T("SmokeWisp",            "Smoke Wisp",              "Milk Elemental",        5,   false, 5,  140, 1f,   3f, 1f,    0.58f),
                T("ThickSmokeWisp",       "Thick Smoke Wisp",        "Milk Elemental 2",      10,  false, 5,  140, 1f,   3f, 2f,    0.60f),
                T("BurntBones",           "Burnt Bones",             "Skeleton",              15,  false, 10, 100, 1f,   3f, 2f,    0.09f),
                T("RooftopStalker",       "Rooftop Stalker",         "Skeleton Ninja 1",      150, false, 8,  200, 1f,   3f, 2.5f,  0.70f),
                T("RooftopReaver",        "Rooftop Reaver",          "Skeleton Ninja 2",      250, false, 9,  200, 1f,   3f, 3f,    0.74f),
                T("OxDemon",              "Ox Demon",                "Minotaur",              250, false, 10, 130, 0.5f, 3f, 2.75f, 0.01f),
                T("ChargingOxDemon",      "Charging Ox Demon",       "Mignotaur",             250, false, 10, 280, 0.5f, 3f, 2.75f, 0.98f),
                T("WellSerpents",         "Well Serpents",           "Twin Snakes",           25,  false, 10, 0,   0f,   0f, 1.5f,  0.30f).Shooting(2f, 140, 20),
                T("BellImps",             "Bell Imps",               "Twin Demons",           150, false, 10, 0,   0f,   0f, 1.75f, 0.95f).Shooting(1.5f, 250, 25),
                T("ShrineIdol",           "Shrine Idol",             "Lost Twin",             180, false, 10, 0,   0f,   0f, 2f,    0.13f).Shooting(1f, 300, 40),
                T("ClayGolem",            "Clay Golem",              "Melone",                300, false, 11, 100, 0f,   1f, 3f,    0.07f),
                T("DemonAxeman",          "Demon Axeman",            "Archon Ascia",          300, false, 15, 140, 0f,   2f, 2.5f,  0.00f),
                T("DemonSpearman",        "Demon Spearman",          "Archon Lancia",         300, false, 15, 160, 1f,   2f, 3f,    0.03f),
                T("PitFiend",             "Pit Fiend",               "Tritont",               500, false, 15, 250, 0f,   2f, 3f,    0.97f),
                T("HearthColossus",       "Hearth Colossus",         "Big Golem 1",           700, false, 20, 140, 0f,   2f, 3f,    0.08f),
                T("FloatingEmber",        "Floating Ember",          "Jellyfish",             180, false, 8,  150, 1f,   3f, 2.5f,  0.11f),
                T("AshWing",              "Ash Wing",                "Skelewing",             350, false, 10, 160, 0f,   2f, 2.5f,  0.66f),
                T("RuinedKnight",         "Ruined Knight",           "Sword Guardian",        700, false, 20, 140, 0f,   2f, 3f,    0.63f),
                T("CinderCockatrice",     "Cinder Cockatrice",       "Gallotrice",            700, false, 15, 140, 0f,   2f, 3f,    0.12f),
                T("EmberChieftain",       "Ember Chieftain",         "Merman (boss)",         50,  true,  11, 140, 1f,   3f, 40f,   0.05f),
                T("OxDemonChampion",      "Ox Demon Champion",       "Minotaur Boss (large)", 50,  false, 11, 180, 1f,   3f, 30f,   0.99f),
                T("MassiveHearthColossus", "Massive Hearth Colossus", "Big Golem 1 (LV29)",   80,  true,  20, 120, 0f,   1f, 25f,   0.08f).Resists(1f),
                T("MassiveKilnColossus",  "Massive Kiln Colossus",   "Big Golem 2 (LV29)",    80,  true,  20, 120, 0f,   1f, 25f,   0.04f).Resists(1f),
                T("OxDemonCaptain", "Ox Demon Captain", "Minotaur Boss", 60f, false, 11, 200, 0.5f, 3f, 30f, 0.06f),
                T("CinderBat", "Cinder Bat", "Pipeestrello 4", 50f, true, 10, 140, 1f, 3f, 30f, 0.09f),
                T("TwinHornedGunner", "Twin-Horned Gunner", "Lost Twin (boss)", 70f, true, 10, 0, 0f, 0f, 30f, 0.85f),
                T("PitFiendKing", "Pit Fiend King", "Tritont (boss)", 200f, true, 20, 180, 0f, 1f, 50f, 0.55f).Resists(1.1f, kill: true, debuff: true),
                T("AxeSentinel", "Axe Sentinel", "Axe Guardian", 200f, false, 20, 180, 0f, 1f, 40f, 0.02f),
                T("GreatKilnColossus", "Great Kiln Colossus", "Big Golem 2 (boss)", 200f, true, 20, 180, 0f, 1f, 50f, 0.07f).Resists(1.0f),
                T("GreatHearthColossus", "Great Hearth Colossus", "Big Golem 1 (boss)", 200f, true, 20, 180, 0f, 1f, 50f, 0.05f).Resists(1.0f),
                T("ColossalCinderCockatrice", "Colossal Cinder Cockatrice", "Colossal Gallotrice", 200f, true, 20, 180, 0f, 1f, 50f, 0.31f).Resists(1.0f, kill: true),
                // Map event units (MapEvents; statboxes on Jellyfish and Mignotaur, raw 2026-10-27). The Pile Assault and
                // homing Swarm units are the wave types above (Twin Snakes, Twin Demons, Lost Twin, Milk Elemental, Skeleton).
                T("RingEmber",     "Ring Ember",     "Jellyfish (wall)",  180, false, 8,  200, 0.2f, 3f, 2.5f,  0.14f).Fixed(),
                T("DriftEmber",    "Drift Ember",    "Jellyfish (swarm)", 180, false, 10, 150, 0.2f, 3f, 2.5f,  0.16f).Fixed(),
                T("StampedeOxDemon", "Stampede Ox Demon", "Mignotaur (rush)", 250, false, 9, 600, 0.5f, 3f, 2.25f, 0.96f).Fixed(),
                // Trap unit (StageTraps; Musc_Musc raw 2026-10-28, "Big Musc Musc": health 13, movespeed 160, damage 4,
                // knockback 1, max 3, xp 1; stages Inlaid Library, Dairy Plant).
                T("SmokeClump",    "Smoke Clump",    "Big Musc Musc",     13,  false, 4,  160, 1f,   3f, 1f,    0.57f),
            },
            // Chest bosses (StageChestBosses): their statboxes, sent only by the boss schedule.
            BossOnly = new[] { "OxDemonCaptain", "CinderBat", "TwinHornedGunner", "PitFiendKing", "AxeSentinel", "GreatKilnColossus", "GreatHearthColossus", "ColossalCinderCockatrice",
                // Map event units, sent only by MapEvents.
                "RingEmber", "DriftEmber", "StampedeOxDemon",
                // Trap units, sent only by StageTraps.
                "SmokeClump" },
            ByMinute = new[]
            {
                /* 0 */ new[] { "CinderPeasant" },
                /* 1 */ new[] { "CinderPeasant" },
                /* 2 */ new[] { "CinderPeasant", "AshHound" },
                /* 3 */ new[] { "AshHound", "SmokeWisp", "CinderPeasant" },
                /* 4 */ new[] { "AshHound", "CinderPeasant", "SmokeWisp" },
                /* 5 */ new[] { "SootBrute", "ThickSmokeWisp" },
                /* 6 */ new[] { "CinderPeasant", "SootBrute" },
                /* 7 */ new[] { "CharredVillager", "AshHound", "CinderPeasant" },
                /* 8 */ new[] { "RooftopStalker", "BurntBones" },
                /* 9 */ new[] { "OxDemon", "ThickSmokeWisp" },
                /* 10 */ new[] { "OxDemon", "BellImps", "ThickSmokeWisp" },
                /* 11 */ new[] { "RooftopStalker", "BurntBones" },
                /* 12 */ new[] { "RooftopReaver", "WellSerpents", "BellImps" },
                /* 13 */ new[] { "EmberElder", "SootBrute", "AshHound" },
                /* 14 */ new[] { "EmberElder", "ClayGolem" },
                /* 15 */ new[] { "DemonAxeman", "ClayGolem", "ShrineIdol" },
                /* 16 */ new[] { "ClayGolem", "DemonSpearman" },
                /* 17 */ new[] { "DemonSpearman", "ChargingOxDemon" },
                /* 18 */ new[] { "DemonAxeman", "ChargingOxDemon" },
                /* 19 */ new[] { "DemonSpearman", "PitFiend" },
                /* 20 */ new[] { "HearthColossus", "PitFiend", "FloatingEmber" },
                /* 21 */ new[] { "RooftopStalker", "RooftopReaver", "AshWing", "ShrineIdol" },
                /* 22 */ new[] { "ClayGolem" },
                /* 23 */ new[] { "OxDemon", "ChargingOxDemon" },
                /* 24 */ new[] { "DemonAxeman", "DemonSpearman", "ClayGolem" },
                /* 25 */ new[] { "RuinedKnight" },
                /* 26 */ new[] { "CinderCockatrice", "ChargingOxDemon" },
                /* 27 */ new[] { "CinderCockatrice", "RuinedKnight" },
                /* 28 */ new[] { "EmberChieftain", "OxDemonChampion" },
                /* 29 */ new[] { "MassiveHearthColossus", "MassiveKilnColossus" },
            },
            Minimum = new[] { 20, 30, 40, 40, 25, 10, 30, 60, 50, 30, 20, 100, 20, 200, 150, 50, 80, 40, 80, 80, 100, 200, 200, 200, 200, 100, 150, 150, 10, 10 },
            Interval = new[] { 1f, 1f, 0.5f, 0.25f, 0.25f, 1f, 0.5f, 0.5f, 1.5f, 0.5f, 0.5f, 0.5f, 1f, 0.1f, 0.5f, 0.5f, 0.5f, 0.1f, 0.5f, 0.5f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f },
        };
    }
}
