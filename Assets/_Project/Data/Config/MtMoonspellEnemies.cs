using static SoulHunter.Gameplay.Data.StageEnemyType;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// Level 6 (The Frozen Peaks): VS Mt.Moonspell's waves (vampire.survivors.wiki/w/Mt.Moonspell "Waves", raw, fetched
    /// 2026-10-09) and each enemy's statbox (Spiritello, Pipeestrello, Zombie, Tsuchinoko, Kamaitachi, Raiju, Yamamba,
    /// Mikoshi-nyudo, Tengu, Thunderous_Oni, Windy_Oni, Tsuchigumo). Owner (2026-10-09): Level 6's source stage is
    /// Mt.Moonspell (a mountain with a frozen lake). Names are Soul Hunter's own (the icy descent).
    /// - "0:00 | Spiritello | 50 | 3.0" ... "29:00 | Thunderous Oni Boss, Windy Oni Boss | 200 | 1.0".
    /// - 1:00 "Pipeestrello, Zombie"; 2:00 the sprites Pipeestrello, Pipeestrello-2 and Pipeestrello-3: the three bat
    ///   statboxes 1, 2 and 3 (2 and 3 have the same numbers).
    /// - Sprite variants by their link: Spiritello-2 = Dark Spiritello, Yamamba-2 = Elite Yamamba, 28:00 Raiju-2 / Kamaitachi-2
    ///   / Tsuchigumo-2 = their Boss statboxes, 29:00 Thunderous Oni-2 / Windy Oni-2 = their Boss statboxes.
    /// - The Frozen Peaks' top/bottom spawning stays the owner's (WaveTables.SideFor).
    /// </summary>
    public static class MtMoonspellEnemies
    {
        public static readonly StageEnemyTable Table = new StageEnemyTable
        {
            Level = 6, Source = "Mt.Moonspell", Folder = "Level06",
            Types = new[]
            {
                T("FrostSprite",          "Frost Sprite",            "Spiritello",          8,   false, 5,  140, 1f,   3f, 1f,   0.53f),
                T("BlackFrostSprite",     "Black Frost Sprite",      "Dark Spiritello",     13,  false, 5,  200, 0f,   3f, 1.5f, 0.66f),
                T("IceBat",               "Ice Bat",                 "Pipeestrello 1",      1,   false, 5,  140, 1f,   3f, 1f,   0.56f),
                T("RimeBat",              "Rime Bat",                "Pipeestrello 2",      5,   false, 5,  140, 1f,   3f, 1f,   0.60f),
                T("FrostEyeBat",          "Frost-Eye Bat",           "Pipeestrello 3",      5,   false, 5,  140, 1f,   3f, 1f,   0.63f),
                T("FrozenCorpse",         "Frozen Corpse",           "Zombie",              10,  false, 10, 100, 0.8f, 3f, 1f,   0.50f),
                T("SnowSerpent",          "Snow Serpent",            "Tsuchinoko",          18,  false, 10, 100, 1f,   3f, 2f,   0.47f),
                T("BlizzardWeasel",       "Blizzard Weasel",         "Kamaitachi",          150, false, 10, 140, 0.3f, 3f, 2.5f, 0.58f),
                T("StormHound",           "Storm Hound",             "Raiju",               100, false, 10, 140, 0.3f, 3f, 2.5f, 0.15f),
                T("PeakCrone",            "Peak Crone",              "Yamamba",             270, false, 10, 140, 0.1f, 3f, 2.5f, 0.70f),
                T("ElderPeakCrone",       "Elder Peak Crone",        "Elite Yamamba",       180, false, 14, 130, 0.8f, 3f, 2f,   0.73f),
                T("LoomingDrift",         "Looming Drift",           "Mikoshi-nyudo",       150, false, 10, 140, 0.3f, 3f, 2.5f, 0.52f),
                T("SummitCrowman",        "Summit Crowman",          "Tengu",               500, false, 20, 140, 0f,   3f, 3f,   0.98f),
                T("ThunderOgre",          "Thunder Ogre",            "Thunderous Oni",      500, false, 20, 70,  0f,   3f, 2.5f, 0.13f),
                T("GaleOgre",             "Gale Ogre",               "Windy Oni",           500, false, 20, 70,  0f,   3f, 2.5f, 0.45f),
                T("GlacierSpider",        "Glacier Spider",          "Tsuchigumo",          800, false, 20, 100, 0f,   3f, 3f,   0.57f),
                T("GreatStormHound",      "Great Storm Hound",       "Raiju Boss",          50,  true,  10, 170, 1f,   1f, 30f,  0.14f),
                T("GreatBlizzardWeasel",  "Great Blizzard Weasel",   "Kamaitachi Boss",     50,  true,  10, 170, 1f,   1f, 30f,  0.59f),
                T("GreatGlacierSpider",   "Great Glacier Spider",    "Tsuchigumo Boss",     100, true,  20, 180, 1f,   1f, 30f,  0.55f).Resists(1f, kill: true, debuff: true),
                T("GreatThunderOgre",     "Great Thunder Ogre",      "Thunderous Oni Boss", 70,  true,  20, 180, 1f,   1f, 30f,  0.12f).Resists(1f, kill: true),
                T("GreatGaleOgre",        "Great Gale Ogre",         "Windy Oni Boss",      70,  true,  20, 180, 1f,   1f, 30f,  0.44f).Resists(1f, kill: true),
                T("FrostSpriteLord", "Frost Sprite Lord", "Spiritello Boss 1", 50f, true, 10, 140, 1f, 3f, 30f, 0.55f),
                T("GreatTricksterBadger", "Great Trickster Badger", "Tanuki Boss", 60f, true, 20, 200, 1f, 1f, 30f, 0.08f).Resists(0.0f, kill: true),
                T("GreatSummitCrowman", "Great Summit Crowman", "Tengu Boss", 60f, true, 20, 200, 1f, 1f, 30f, 0.99f).Resists(0.0f, kill: true),
                T("ColossalFrostSprite", "Colossal Frost Sprite", "Spiritello Boss 2", 200f, true, 10, 140, 1f, 3f, 30f, 0.57f),
                // Map event units (MapEvents; statboxes on Spiritello and Flower_Wall, raw 2026-10-28). Health 7.5 and 12.5 round to
                // whole numbers as the wave table's own Frost Sprites do (7.5 -> 8, 12.5 -> 13).
                T("SwarmFrostSprite",      "Swarm Frost Sprite",       "Swarm Spiritello",       7.5f,  false, 5, 700, 1f, 3f, 1f,   0.52f).Fixed(),
                T("SwarmBlackFrostSprite", "Swarm Black Frost Sprite", "Swarm Dusty Spiritello", 12.5f, false, 5, 700, 0f, 3f, 1.5f, 0.65f).Fixed(),
                T("MoonThornWall",         "Moon Thorn Wall",          "Moonspell Flower Wall",  30f,   true,  5, 0,   0f, 3f, 4f,   0.35f),
                // The stage boss's part (BossParts; Orochimario raw 2026-10-28, "Orochimario head": health 500 HP x Level,
                // movespeed 0, damage 20, knockback 0, max 1, xp 30, res-freeze 1.1, res-kill 1, res-debuffs 1).
                T("HydraHead",             "Hydra Head",               "Orochimario head",       500f,  true,  20, 0,  0f, 1f, 30f,  0.62f).Resists(1.1f, kill: true, debuff: true),
            },
            // Chest bosses (StageChestBosses): their statboxes, sent only by the boss schedule.
            BossOnly = new[] { "FrostSpriteLord", "GreatTricksterBadger", "GreatSummitCrowman", "ColossalFrostSprite",
                // Map event units, sent only by MapEvents.
                "SwarmFrostSprite", "SwarmBlackFrostSprite", "MoonThornWall",
                // The stage boss's parts, sent only by BossParts.
                "HydraHead" },
            ByMinute = new[]
            {
                /* 0 */ new[] { "FrostSprite" },
                /* 1 */ new[] { "IceBat", "FrozenCorpse" },
                /* 2 */ new[] { "IceBat", "RimeBat", "FrostEyeBat" },
                /* 3 */ new[] { "SnowSerpent" },
                /* 4 */ new[] { "SnowSerpent", "BlackFrostSprite" },
                /* 5 */ new[] { "BlizzardWeasel" },
                /* 6 */ new[] { "BlizzardWeasel", "BlackFrostSprite" },
                /* 7 */ new[] { "StormHound", "FrostSprite" },
                /* 8 */ new[] { "StormHound" },
                /* 9 */ new[] { "PeakCrone", "FrozenCorpse" },
                /* 10 */ new[] { "BlizzardWeasel", "StormHound" },
                /* 11 */ new[] { "SnowSerpent" },
                /* 12 */ new[] { "ElderPeakCrone", "BlackFrostSprite", "LoomingDrift" },
                /* 13 */ new[] { "ElderPeakCrone", "BlackFrostSprite" },
                /* 14 */ new[] { "PeakCrone", "ElderPeakCrone" },
                /* 15 */ new[] { "ElderPeakCrone", "PeakCrone", "LoomingDrift" },
                /* 16 */ new[] { "ElderPeakCrone", "PeakCrone", "LoomingDrift" },
                /* 17 */ new[] { "SummitCrowman" },
                /* 18 */ new[] { "SummitCrowman", "StormHound" },
                /* 19 */ new[] { "SummitCrowman", "LoomingDrift" },
                /* 20 */ new[] { "SummitCrowman", "BlizzardWeasel", "PeakCrone" },
                /* 21 */ new[] { "ThunderOgre" },
                /* 22 */ new[] { "GaleOgre" },
                /* 23 */ new[] { "GaleOgre", "SummitCrowman" },
                /* 24 */ new[] { "ThunderOgre", "SummitCrowman" },
                /* 25 */ new[] { "GlacierSpider" },
                /* 26 */ new[] { "GlacierSpider", "GaleOgre" },
                /* 27 */ new[] { "SummitCrowman", "ThunderOgre", "GaleOgre" },
                /* 28 */ new[] { "GreatStormHound", "GreatBlizzardWeasel", "GreatGlacierSpider" },
                /* 29 */ new[] { "GreatThunderOgre", "GreatGaleOgre" },
            },
            Minimum = new[] { 50, 80, 50, 40, 30, 100, 20, 80, 100, 40, 80, 300, 40, 150, 20, 50, 100, 100, 20, 100, 100, 150, 150, 200, 200, 50, 150, 300, 200, 200 },
            Interval = new[] { 3f, 3f, 0.5f, 0.25f, 0.5f, 4f, 0.5f, 0.5f, 1.5f, 0.5f, 0.5f, 0.1f, 0.25f, 0.5f, 0.1f, 0.1f, 1f, 0.8f, 1f, 0.5f, 0.1f, 0.1f, 1f, 0.1f, 0.1f, 0.1f, 0.1f, 1f, 1f, 1f },
        };
    }
}
