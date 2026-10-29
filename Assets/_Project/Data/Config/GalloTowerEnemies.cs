using static SoulHunter.Gameplay.Data.StageEnemyType;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// Level 4 (The Ruined Castle): VS Gallo Tower's waves (vampire.survivors.wiki/w/Gallo_Tower "Waves", raw, fetched
    /// 2026-10-09) and each enemy's statbox (Skullino, Skulorosso, Pipeestrello, Scarleton, Skeleton, Skullone,
    /// Testa_di_Mano, Ghiavolo, Bloodbath, Ghost, Impefinger, Harzia, Dragon_Shrimp, Undead_Mage, Skelewing, Archon_Spada,
    /// Archon_Disco, Gallotrice, Manticore, Meat_Golem). Names are Soul Hunter's own (the castle's gargoyles and gate).
    /// - "0:00 | Skullino | 50 | 3.0" ... "29:00 | Colossal Bone Dragon, Colossal Flame Dragon | 50 | 1.0".
    /// - 11, 12 and 14:00 "Wandering Harzia" is Harzia V ("skills = [[Fixed Direction]]"); 13, 15 and 23:00 plain Harzia.
    /// - 5:00 "Lesser Skullone" (sprite Skullone-2) is Skullone (easy); 18:00 "Aggressive Skelewing" is Skelewing (ino).
    /// - The 29:00 colossi are the Dragon Shrimp boss statboxes (50 and 60 x Level).
    /// - Thrice-Risen (Scarleton): "Has 3 lives" (EnemyData.Lives, EnemyLives).
    /// - Gate Sorcerer (Undead Mage): "Shoots a bullet every 2 seconds" (Bullet 2: speed 250, power 25) and "takes no
    ///   knockback" (knockback 0 in its statbox); unlike the stationary shooters it moves (speed 130).
    /// - The corridor and spawn sides stay the owner's (WaveTables.CorridorHalfWidth, SideFor).
    /// </summary>
    public static class GalloTowerEnemies
    {
        public static readonly StageEnemyTable Table = new StageEnemyTable
        {
            Level = 4, Source = "Gallo_Tower", Folder = "Level04",
            Types = new[]
            {
                T("GateSkull",         "Gate Skull",           "Skullino",              6,   false, 5,  140, 1f,   3f, 1f,   0.10f),
                T("RedGateSkull",      "Red Gate Skull",       "Skulorosso",            10,  false, 5,  140, 1f,   3f, 1f,   0.00f),
                T("RampartBat",        "Rampart Bat",          "Pipeestrello 3",        5,   false, 5,  140, 1f,   3f, 1f,   0.75f),
                T("ThriceRisen",       "Thrice-Risen",         "Scarleton",             15,  false, 5,  100, 1f,   3f, 1f,   0.97f).WithLives(3),
                T("CastleBones",       "Castle Bones",         "Skeleton",              15,  false, 10, 100, 1f,   3f, 2f,   0.12f),
                T("StoneSkull",        "Stone Skull",          "Skullone (easy)",       20,  false, 10, 100, 1f,   3f, 2f,   0.55f),
                T("HeadlessWarden",    "Headless Warden",      "Testa di Mano 1",       35,  false, 8,  100, 0.3f, 3f, 1f,   0.60f),
                T("GargoyleImp",       "Gargoyle Imp",         "Ghiavolo",              70,  false, 8,  100, 0.3f, 3f, 2f,   0.58f),
                T("BloodBat",          "Blood Bat",            "Bloodbath",             11,  false, 5,  140, 1f,   3f, 1f,   0.99f),
                T("HallWraith",        "Hall Wraith",          "Ghost 2",               10,  false, 5,  200, 0f,   3f, 1.5f, 0.52f),
                T("CryptImp",          "Crypt Imp",            "Impefinger",            13,  false, 4,  160, 1f,   3f, 1f,   0.83f),
                T("HarpyGargoyle",     "Harpy Gargoyle",       "Harzia",                25,  false, 1,  240, 1f,   3f, 2f,   0.62f),
                T("GlidingGargoyle",   "Gliding Gargoyle",     "Harzia V",              25,  false, 1,  240, 1f,   3f, 2f,   0.65f).Fixed(),
                T("BoneDrake",         "Bone Drake",           "Dragon Shrimp 1",       4,   true,  3,  200, 0.6f, 3f, 2f,   0.14f),
                T("FlameDrake",        "Flame Drake",          "Dragon Shrimp 2",       20,  true,  8,  200, 0.6f, 3f, 2f,   0.04f),
                T("GateSorcerer",      "Gate Sorcerer",        "Undead Mage",           220, false, 14, 130, 0f,   0f, 3f,   0.80f).Shooting(2f, 250, 25),
                T("WingedBones",       "Winged Bones",         "Skelewing (ino)",       220, false, 14, 130, 0.1f, 3f, 3f,   0.15f),
                T("GatekeeperBlade",   "Gatekeeper Blade",     "Archon Spada",          500, false, 20, 200, 1.5f, 3f, 3f,   0.57f),
                T("GatekeeperShield",  "Gatekeeper Shield",    "Archon Disco",          500, false, 20, 200, 1.5f, 3f, 3f,   0.54f),
                T("Basilisk",          "Basilisk",             "Gallotrice",            700, false, 15, 140, 0f,   2f, 3f,   0.30f),
                T("CastleChimera",     "Castle Chimera",       "Manticore",             700, false, 15, 140, 0f,   2f, 3f,   0.07f),
                T("FleshGolem",        "Flesh Golem",          "Meat Golem 1",          900, false, 20, 200, 0f,   2f, 3f,   0.96f),
                T("BloatedFleshGolem", "Bloated Flesh Golem",  "Meat Golem 2",          900, false, 20, 200, 0f,   2f, 3f,   0.93f),
                T("GreatBoneDrake",    "Great Bone Drake",     "Dragon Shrimp 1 (boss)", 50, true,  10, 170, 1f,   1f, 30f,  0.13f),
                T("GreatFlameDrake",   "Great Flame Drake",    "Dragon Shrimp 2 (boss)", 60, true,  10, 170, 1f,   1f, 30f,  0.03f),
                T("CastleFiend", "Castle Fiend", "Elite Devil", 50f, true, 10, 140, 0.5f, 3f, 30f, 0.97f),
                T("GreatStoneSkull", "Great Stone Skull", "Skullone (boss)", 50f, true, 10, 140, 1f, 3f, 30f, 0.12f),
                T("GreatHarpyGargoyle", "Great Harpy Gargoyle", "Harzia (boss)", 50f, true, 10, 170, 1f, 1f, 30f, 0.7f),
                T("RampartEmberBat", "Rampart Ember Bat", "Pipeestrello 4", 50f, true, 10, 140, 1f, 3f, 30f, 0.08f),
                T("RampartLord", "Rampart Lord", "Gallo", 200f, true, 30, 160, 0f, 1f, 50f, 0.14f).Resists(1.1f, kill: true),
                T("ShieldSentinel", "Shield Sentinel", "Archon Disco (boss)", 150f, true, 20, 160, 0.5f, 1f, 30f, 0.58f).Resists(1.1f),
                T("ColossalBasilisk", "Colossal Basilisk", "Colossal Gallotrice", 200f, true, 20, 180, 0f, 1f, 50f, 0.29f).Resists(1.0f, kill: true),
                T("ColossalCastleChimera", "Colossal Castle Chimera", "Colossal Manticore", 200f, false, 20, 180, 0f, 1f, 50f, 0.04f).Resists(1.0f, kill: true),
                // Map event units (MapEvents; statboxes on Dragon_Shrimp, raw 2026-10-28). The page: "the flag variants tend to
                // make horizontal, zig-zag movements that do not follow the player's character", so they weave along their
                // line (Floaty + Fixed Direction). Their body is drawn only, so only the head hurts and is hurt (SerpentBody).
                T("EmberSerpent", "Ember Serpent", "Dragon Shrimp 1 (flag)", 5f,  true, 3, 400, 0f, 0f, 2f, 0.05f).FloatyFixed(),
                T("BlazeSerpent", "Blaze Serpent", "Dragon Shrimp 2 (flag)", 20f, true, 6, 400, 0f, 0f, 2f, 0.02f).FloatyFixed(),
                // Trap units (StageTraps; Poltergeist raw 2026-10-28): "Poltergeist" health 5 HP x Level, movespeed 50,
                // damage 3, knockback 0, xp 2, Self-destruct ("Explodes after two seconds dealing 50 damage"); "Poltergeist
                // (dest)" health 10 HP x Level, movespeed 0 ("Cannot move"). Both "spawn upon stepping on a pressure plate".
                T("RestlessCandle", "Restless Candle", "Poltergeist",        5f,  true, 3, 50, 0f, 0f, 2f, 0.11f).SelfDestructing(),
                T("StillCandle",    "Still Candle",    "Poltergeist (dest)", 10f, true, 3, 0,  0f, 0f, 2f, 0.13f).SelfDestructing(),
                // The stage boss's part (BossParts; Giant_Enemy_Crab raw 2026-10-28, "Pincer": health 5 HP x Level, movespeed 0,
                // damage 12, knockback 0, max 1, xp 10, res-debuff 1).
                T("CrabPincer",     "Crab Pincer",     "Pincer",             5f,  true, 12, 0, 0f, 1f, 10f, 0.03f).Resists(0f, debuff: true),
            },
            // Chest bosses (StageChestBosses): their statboxes, sent only by the boss schedule.
            BossOnly = new[] { "CastleFiend", "GreatStoneSkull", "GreatHarpyGargoyle", "RampartEmberBat", "RampartLord", "ShieldSentinel", "ColossalBasilisk", "ColossalCastleChimera",
                // Map event units, sent only by MapEvents.
                "EmberSerpent", "BlazeSerpent",
                // Trap units, sent only by StageTraps.
                "RestlessCandle", "StillCandle",
                // The stage boss's parts, sent only by BossParts.
                "CrabPincer" },
            ByMinute = new[]
            {
                /* 0 */ new[] { "GateSkull" },
                /* 1 */ new[] { "GateSkull", "RedGateSkull" },
                /* 2 */ new[] { "GateSkull", "RampartBat" },
                /* 3 */ new[] { "ThriceRisen" },
                /* 4 */ new[] { "GateSkull", "CastleBones" },
                /* 5 */ new[] { "StoneSkull", "ThriceRisen" },
                /* 6 */ new[] { "HeadlessWarden", "RedGateSkull" },
                /* 7 */ new[] { "GargoyleImp", "BloodBat", "RampartBat" },
                /* 8 */ new[] { "RedGateSkull", "HallWraith" },
                /* 9 */ new[] { "HeadlessWarden", "HallWraith" },
                /* 10 */ new[] { "CryptImp" },
                /* 11 */ new[] { "GlidingGargoyle", "BoneDrake" },
                /* 12 */ new[] { "GlidingGargoyle", "GargoyleImp" },
                /* 13 */ new[] { "HarpyGargoyle", "GargoyleImp" },
                /* 14 */ new[] { "HeadlessWarden", "GlidingGargoyle", "HallWraith" },
                /* 15 */ new[] { "HarpyGargoyle" },
                /* 16 */ new[] { "GateSorcerer" },
                /* 17 */ new[] { "GateSorcerer", "FlameDrake" },
                /* 18 */ new[] { "WingedBones", "FlameDrake" },
                /* 19 */ new[] { "ThriceRisen" },
                /* 20 */ new[] { "BoneDrake", "HeadlessWarden" },
                /* 21 */ new[] { "GatekeeperBlade" },
                /* 22 */ new[] { "GatekeeperBlade", "GatekeeperShield" },
                /* 23 */ new[] { "Basilisk", "HarpyGargoyle" },
                /* 24 */ new[] { "CastleChimera" },
                /* 25 */ new[] { "RedGateSkull" },
                /* 26 */ new[] { "FleshGolem" },
                /* 27 */ new[] { "BloatedFleshGolem", "CastleChimera" },
                /* 28 */ new[] { "FleshGolem", "BloatedFleshGolem" },
                /* 29 */ new[] { "GreatBoneDrake", "GreatFlameDrake" },
            },
            Minimum = new[] { 50, 80, 100, 60, 110, 100, 60, 80, 80, 200, 100, 60, 80, 120, 300, 100, 100, 100, 60, 200, 100, 100, 80, 100, 60, 300, 100, 100, 100, 50 },
            Interval = new[] { 3f, 3f, 4f, 4f, 4f, 4f, 2f, 2f, 1f, 0.5f, 0.5f, 2f, 1f, 0.5f, 0.1f, 0.1f, 1f, 0.8f, 0.1f, 0.5f, 0.5f, 0.1f, 1f, 0.1f, 0.1f, 0.1f, 0.1f, 1f, 1f, 1f },
        };
    }
}
