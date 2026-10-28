using static SoulHunter.Gameplay.Data.StageEnemyType;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// Level 5 (The Crimson Swamp): VS Abyss Foscari's waves (vampire.survivors.wiki/w/Abyss_Foscari "Waves", raw, fetched
    /// 2026-10-09) and each enemy's statbox (Cauld, Redusa_Head, Blood_Moss, Rotting_Ghoul, Burning_Skull, Missing_Church,
    /// Snek, Meatball, Lost_Head, Followa, Cold_Cauld, Edna, Well_Dweller, Wet_Njuggles, Crocifiggitone, Still_Notadam,
    /// Fomorians, Glove_of_Glory, Maronna_Meea). Owner (2026-10-09): Level 5's source stage is Abyss Foscari ("Sealed by
    /// Luminaire in ancient times to contain the darkness within"). Names are Soul Hunter's own (a rotting swamp).
    /// - "0:00 | Cauld | 100 | 1.0" ... "29:00 | Spicy Meatball, Giant Followa | 30 | 0.1".
    /// - Sprite variants by their link: Blood Moss-2 = Swarm Blood Moss; Lost Head (#Lost Head 2) = Lost Head 2;
    ///   Missing Church-2 = Missing Church 2; 28:00 Burning Skull (#Flaming Skull) = Flaming Skull Boss and Redusa Head-2 =
    ///   Colossal Redusa Head; 29:00 Meatball-2 = Spicy Meatball and Followa-2 = Giant Followa; 25:00 the three Fomorians
    ///   Conand, Balor and Indech.
    /// - Stationary shooters (Lost_Head): Lost Head "Shoots a bullet every 2 seconds" (Bullet 1: speed 140, power 20);
    ///   Lost Head 2 "every 1.5 seconds" (Bullet 2: 250, 25); both "immune to knockback".
    /// - Great Bog Mother (Maronna Meea Boss) is never in a minute's wave: it is the 24:00 chest-less wave boss
    ///   (ChestlessWaveBosses). Its 27:00 appearance carries a {{treasure}}, which Soul Hunter's 27:00 chest elite covers.
    /// </summary>
    public static class AbyssFoscariEnemies
    {
        public static readonly StageEnemyTable Table = new StageEnemyTable
        {
            Level = 5, Source = "Abyss_Foscari", Folder = "Level05",
            Types = new[]
            {
                T("BogLeech",            "Bog Leech",              "Cauld",                 5,   false, 4,  140, 1f,   3f, 1f,    0.95f),
                T("RotHead",             "Rot Head",               "Redusa Head",           15,  false, 6,  100, 1f,   3f, 2.2f,  0.02f),
                T("CrimsonSpore",        "Crimson Spore",          "Blood Moss",            5,   false, 5,  140, 1f,   3f, 1f,    0.98f),
                T("CrimsonSporeCluster", "Crimson Spore Cluster",  "Swarm Blood Moss",      10,  false, 5,  140, 1f,   3f, 2f,    0.00f),
                T("BogGhoul",            "Bog Ghoul",              "Rotting Ghoul",         120, false, 7,  100, 0f,   1f, 4f,    0.22f),
                T("MarshfireSkull",      "Marshfire Skull",        "Burning Skull",         60,  false, 8,  100, 1f,   3f, 2.5f,  0.07f),
                T("MireWorm",            "Mire Worm",              "Missing Church",        130, false, 8,  200, 1f,   3f, 4f,    0.90f),
                T("GreatMireWorm",       "Great Mire Worm",        "Missing Church 2",      200, false, 9,  200, 1f,   3f, 4f,    0.88f),
                T("SwampAdder",          "Swamp Adder",            "Snek",                  15,  false, 10, 100, 1f,   3f, 2f,    0.27f),
                T("FleshClot",           "Flesh Clot",             "Meatball",              250, false, 10, 130, 0.5f, 3f, 2.75f, 0.97f),
                T("WhisperingHead",      "Whispering Head",        "Lost Head",             25,  false, 10, 0,   0f,   0f, 1.5f,  0.85f).Shooting(2f, 140, 20),
                T("WailingHead",         "Wailing Head",           "Lost Head 2",           150, false, 10, 0,   0f,   0f, 1.75f, 0.82f).Shooting(1.5f, 250, 25),
                T("TrailingRot",         "Trailing Rot",           "Followa",               150, false, 8,  100, 1f,   3f, 3f,    0.20f),
                T("ChillBogLeech",       "Chill Bog Leech",        "Cold Cauld",            300, false, 15, 140, 0f,   2f, 2.5f,  0.55f),
                T("MireCrone",           "Mire Crone",             "Edna",                  300, false, 11, 100, 0f,   1f, 3.5f,  0.24f),
                T("PitLurker",           "Pit Lurker",             "Well Dweller",          300, false, 15, 160, 1f,   2f, 3f,    0.92f),
                T("BogKelpie",           "Bog Kelpie",             "Wet Njuggles",          250, false, 10, 280, 0.5f, 3f, 2.75f, 0.45f),
                T("RotwoodPenitent",     "Rotwood Penitent",       "Crocifiggitone",        500, false, 15, 150, 0f,   2f, 3f,    0.08f),
                T("SilentDrowned",       "Silent Drowned",         "Still Notadam",         180, false, 8,  150, 1f,   3f, 2.5f,  0.50f),
                T("SwampGiantHorn",      "Swamp Giant of the Horn", "Conand",               700, false, 20, 140, 0f,   2f, 3f,    0.03f),
                T("SwampGiantEye",       "Swamp Giant of the Eye", "Balor",                 700, false, 20, 140, 0f,   2f, 3f,    0.96f),
                T("SwampGiantMaw",       "Swamp Giant of the Maw", "Indech",                700, false, 20, 140, 0f,   2f, 3f,    0.18f),
                T("HangedHand",          "Hanged Hand",            "Glove of Glory",        700, false, 15, 140, 0f,   2f, 3f,    0.15f),
                T("BogMother",           "Bog Mother",             "Maronna Meea",          700, false, 20, 140, 0f,   2f, 3f,    0.93f),
                T("GreatMarshfireSkull", "Great Marshfire Skull",  "Flaming Skull Boss",    100, true,  20, 180, 0f,   1f, 30f,   0.06f),
                T("ColossalRotHead",     "Colossal Rot Head",      "Colossal Redusa Head",  100, true,  20, 180, 0f,   1f, 30f,   0.01f),
                T("BurstingFleshClot",   "Bursting Flesh Clot",    "Spicy Meatball",        80,  true,  20, 120, 0f,   1f, 0f,    0.99f).Resists(0f, kill: true),
                T("GreatTrailingRot",    "Great Trailing Rot",     "Giant Followa",         80,  true,  20, 120, 0f,   1f, 25f,   0.21f).Resists(1f),
                // Maronna_Meea, "Unlisted variants": "Maronna Meea Boss ... skills = HP x Level | health = 200 | movespeed = 180
                // | damage = 20 | knockback = 0 | knockback-max = 1 | xp = 30 | res-freeze = 1.1 | res-kill = 1 | res-debuff = 1".
                T("GreatBogMother",      "Great Bog Mother",       "Maronna Meea Boss",     200, true,  20, 180, 0f,   1f, 30f,   0.94f).Resists(1.1f, kill: true, debuff: true),
                T("ColossalFleshClot", "Colossal Flesh Clot", "Colossal Meatball", 100f, true, 20, 180, 0f, 1f, 30f, 0.98f).Resists(1.0f),
                T("WarmBogLeech", "Warm Bog Leech", "Warm Cauld", 200f, true, 20, 180, 0f, 1f, 30f, 0.08f).Resists(1.1f, kill: true),
                T("GreatMireCrone", "Great Mire Crone", "Edna Boss", 100f, true, 20, 180, 0f, 1f, 30f, 0.8f).Resists(0.0f, kill: true),
                T("GreatPitLurker", "Great Pit Lurker", "Well Well Well Dweller", 200f, true, 20, 180, 0f, 1f, 30f, 0.66f).Resists(1.1f, kill: true),
                // Map event units (MapEvents; statboxes on Rotting_Ghoul and Rellyfish, raw 2026-10-28). The Pile Assault and homing
                // Swarm units are the wave types above (Lost Head 1/2, Blood Moss, Swarm Blood Moss, Snek).
                T("StampedeGhoul", "Stampede Ghoul", "Swarm Rotting Ghoul", 250, false, 9,  600, 0.5f, 3f, 2.25f, 0.30f).Fixed(),
                T("RingMire",      "Ring Mire",      "Rellyfish",           140, false, 6,  200, 0.2f, 3f, 2.5f,  0.52f).Fixed(),
                T("DriftMire",     "Drift Mire",     "Swarm Rellyfish",     180, false, 10, 150, 0.2f, 3f, 2.5f,  0.55f).Fixed(),
            },
            // "24:00 | Edna, Well Dweller, Cold Cauld | 200 | 0.1 | Maronna Meea Boss": only in the boss column.
            BossOnly = new[] { "GreatBogMother", "ColossalFleshClot", "WarmBogLeech", "GreatMireCrone", "GreatPitLurker",
                // Map event units, sent only by MapEvents.
                "StampedeGhoul", "RingMire", "DriftMire" },
            ByMinute = new[]
            {
                /* 0 */ new[] { "BogLeech" },
                /* 1 */ new[] { "BogLeech" },
                /* 2 */ new[] { "BogLeech", "RotHead" },
                /* 3 */ new[] { "RotHead", "CrimsonSpore", "BogLeech" },
                /* 4 */ new[] { "RotHead", "BogLeech", "CrimsonSpore" },
                /* 5 */ new[] { "BogGhoul", "CrimsonSporeCluster" },
                /* 6 */ new[] { "BogLeech", "BogGhoul" },
                /* 7 */ new[] { "MarshfireSkull", "RotHead", "BogLeech" },
                /* 8 */ new[] { "MireWorm", "SwampAdder" },
                /* 9 */ new[] { "FleshClot", "CrimsonSporeCluster" },
                /* 10 */ new[] { "FleshClot", "WailingHead", "CrimsonSporeCluster" },
                /* 11 */ new[] { "MireWorm", "SwampAdder" },
                /* 12 */ new[] { "GreatMireWorm", "WhisperingHead", "WailingHead" },
                /* 13 */ new[] { "TrailingRot", "BogGhoul", "RotHead" },
                /* 14 */ new[] { "TrailingRot", "ChillBogLeech" },
                /* 15 */ new[] { "MireCrone", "ChillBogLeech", "WailingHead" },
                /* 16 */ new[] { "ChillBogLeech", "PitLurker" },
                /* 17 */ new[] { "PitLurker", "BogKelpie" },
                /* 18 */ new[] { "MireCrone", "BogKelpie" },
                /* 19 */ new[] { "PitLurker", "RotwoodPenitent" },
                /* 20 */ new[] { "RotwoodPenitent", "SilentDrowned" },
                /* 21 */ new[] { "MireWorm", "GreatMireWorm", "WailingHead" },
                /* 22 */ new[] { "ChillBogLeech" },
                /* 23 */ new[] { "FleshClot", "BogKelpie" },
                /* 24 */ new[] { "MireCrone", "PitLurker", "ChillBogLeech" },
                /* 25 */ new[] { "SwampGiantHorn", "SwampGiantEye", "SwampGiantMaw" },
                /* 26 */ new[] { "HangedHand", "BogKelpie" },
                /* 27 */ new[] { "HangedHand", "BogMother" },
                /* 28 */ new[] { "GreatMarshfireSkull", "ColossalRotHead" },
                /* 29 */ new[] { "BurstingFleshClot", "GreatTrailingRot" },
            },
            Minimum = new[] { 100, 30, 40, 40, 25, 10, 30, 60, 50, 30, 20, 100, 20, 200, 150, 50, 80, 40, 90, 80, 100, 200, 200, 200, 200, 100, 200, 150, 50, 30 },
            Interval = new[] { 1f, 1f, 0.5f, 0.25f, 0.25f, 1f, 0.5f, 0.5f, 1.5f, 0.5f, 0.5f, 0.5f, 1f, 0.1f, 0.5f, 0.5f, 0.5f, 0.1f, 0.5f, 0.5f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f },
        };
    }
}
