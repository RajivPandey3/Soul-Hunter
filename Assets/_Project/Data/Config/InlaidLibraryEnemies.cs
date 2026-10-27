using static SoulHunter.Gameplay.Data.StageEnemyType;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// Level 2 (The Dark Forest): VS Inlaid Library's waves (vampire.survivors.wiki/w/Inlaid_Library "Waves", raw, fetched
    /// 2026-10-09) and each enemy's statbox (Dust_Elemental, Musc_Musc, Mummy, Ghost, Testa_di_Mano, Lionhead, Sneaky_Head,
    /// Undead_Witch, Undead_Sassy_Witch, Skullone, Giant_Bat, Merdusa). Names are Soul Hunter's own.
    /// - "0:00 | Dust Elemental | 50 | 3.0" ... "29:00 | Colossal Dust Elemental, Colossal Musc Musc, Colossal Lionhead,
    ///   Colossal Sneaky Head | 250 | 0.1".
    /// - Testa di Mano 2 / 3: the row's "Testa di Mano-2" sprite (linked "Elite Dullahan") is Testa di Mano 3, as the
    ///   Testa_di_Mano Appearances tables list (variant 3 at 16, 19, 20 and 23:00; variant 2 at 6, 7, 12, 13, 16, 23).
    /// - 14:00 "Medusa Head" uses the sprite "Sneaky Head-2" (Big Sneaky Head); the Medusa_Head page says it "has no
    ///   spawns", so the Big Sneaky Head statbox is used.
    /// - Owner signature (AGENTS.md Part 2): Level 2's normal enemies are Floaty; the Sneaky Heads keep their own
    ///   "[[Floaty]], [[Fixed Direction]]".
    /// - Lantern Skull is VS "Skullone (scaling)", health 6.5 x Level (EnemyData.HealthPerPlayerLevelFraction).
    /// </summary>
    public static class InlaidLibraryEnemies
    {
        public static readonly StageEnemyTable Table = new StageEnemyTable
        {
            Level = 2, Source = "Inlaid_Library", Folder = "Level02", AllFloaty = true,
            Types = new[]
            {
                T("AshSprite",        "Ash Sprite",          "Dust Elemental",              5,   false, 5,  140, 1f,   3f, 1f,   0.08f),
                T("SporeMote",        "Spore Mote",          "Musc Musc",                   1,   false, 2,  160, 1f,   3f, 1f,   0.25f),
                T("SporeClump",       "Spore Clump",         "Big Musc Musc",               13,  false, 4,  160, 1f,   3f, 1f,   0.28f),
                T("VineBoundDead",    "Vine-Bound Dead",     "Mummy",                       15,  false, 3,  140, 1f,   3f, 2f,   0.18f),
                T("ForestWisp",       "Forest Wisp",         "Ghost 1",                     10,  false, 5,  200, 0f,   3f, 1.5f, 0.50f),
                T("BarkHusk",         "Bark Husk",           "Testa di Mano 2",             70,  false, 8,  100, 0.3f, 3f, 2f,   0.07f),
                T("ElderBarkHusk",    "Elder Bark Husk",     "Testa di Mano 3",             150, false, 8,  100, 0.2f, 3f, 2f,   0.05f),
                T("ThornLion",        "Thorn Lion",          "Lionhead",                    3,   true,  3,  200, 1f,   3f, 2f,   0.12f),
                T("GloomMoth",        "Gloom Moth",          "Big Sneaky Head",             25,  false, 1,  240, 1f,   3f, 2f,   0.72f).FloatyFixed(),
                T("HuntingGloomMoth", "Hunting Gloom Moth",  "Aggressive Big Sneaky Head",  25,  false, 1,  240, 1f,   3f, 2f,   0.78f),
                T("BriarWitch",       "Briar Witch",         "Undead Witch",                220, false, 14, 130, 0.8f, 3f, 3f,   0.33f),
                T("BriarMatron",      "Briar Matron",        "Undead Sassy Witch",          500, false, 20, 200, 1.5f, 3f, 3f,   0.38f),
                T("LanternSkull",     "Lantern Skull",       "Skullone (scaling)",          6.5f, true, 14, 140, 0f,   3f, 2f,   0.14f),
                T("GreatNightBat",    "Great Night Bat",     "Giant Bat",                   270, false, 10, 140, 0.1f, 3f, 2.5f, 0.68f),
                T("GorgonWillow",     "Gorgon Willow",       "Merdusa",                     800, false, 15, 150, 1f,   3f, 3f,   0.42f),
                T("AshColossus",      "Ash Colossus",        "Dust Elemental (boss)",       50,  true,  10, 170, 1f,   1f, 30f,  0.09f),
                T("SporeColossus",    "Spore Colossus",      "Colossal Musc Musc",          50,  true,  10, 170, 1f,   1f, 30f,  0.27f),
                T("ThornLionKing",    "Thorn Lion King",     "Lionhead (boss)",             50,  true,  10, 170, 1f,   1f, 30f,  0.11f),
                T("GloomMothQueen",   "Gloom Moth Queen",    "Colossal Sneaky Head",        40,  true,  10, 170, 1f,   1f, 30f,  0.75f),
                T("GreatVineBoundDead", "Great Vine-Bound Dead", "Big Mummy (boss)", 250f, true, 20, 80, 0f, 1f, 25f, 0.17f).Resists(1.1f),
                T("EmberNightBat", "Ember Night Bat", "Pipeestrello 4", 50f, true, 10, 140, 1f, 3f, 30f, 0.1f),
                T("AshenNightBat", "Ashen Night Bat", "Pipeestrello 5", 50f, true, 10, 140, 1f, 3f, 30f, 0.62f),
                T("GorgonWillowQueen", "Gorgon Willow Queen", "Merdusa (boss)", 150f, true, 20, 160, 0.5f, 1f, 30f, 0.44f).Resists(1.1f),
                T("BriarMatriarch", "Briar Matriarch", "Undead Sassy Witch (boss)", 150f, false, 30, 200, 0.6f, 1f, 3f, 0.36f).Resists(1.1f),
                T("AshenWyrm", "Ashen Wyrm", "Nesufritto", 200f, true, 30, 160, 0f, 1f, 50f, 0.03f).Resists(1.0f),
                // Map event units (MapEvents; statboxes on Sig.ra_Rossi, Sneaky_Head, Skullone and Pipeestrello, raw
                // 2026-10-27). Big Sneaky Head is the Gloom Moth above. The Sneaky Heads' "maximum speed of 340 / 700" is
                // a cap on speed bonuses, which Soul Hunter's events do not give.
                T("CinderShade",    "Cinder Shade",     "Sig.ra Rossi",         11f, true,  3, 330, 1f, 3f, 2f, 0.02f).SelfDestructing().OwnMovement(),
                T("GloomMote",      "Gloom Mote",       "Sneaky Head",          1,   false, 1, 240, 1f, 3f, 1f, 0.70f).FloatyFixed(),
                T("SwiftGloomMote", "Swift Gloom Mote", "Fast Sneaky Head",     1,   false, 1, 600, 1f, 3f, 1f, 0.66f).FloatyFixed(),
                T("SwiftGloomMoth", "Swift Gloom Moth", "Fast Big Sneaky Head", 25,  false, 1, 600, 1f, 3f, 2f, 0.80f).FloatyFixed(),
                T("SwarmSkull",     "Swarm Skull",      "Skullone (swarm)",     25,  false, 3, 700, 0f, 3f, 2f, 0.15f).Fixed(),
                T("SwarmNightBat",  "Swarm Night Bat",  "Pipeestrello (swarm)", 1,   false, 1, 700, 1f, 3f, 1f, 0.60f).Fixed().NoDefang(),
            },
            // Chest bosses (StageChestBosses): their statboxes, sent only by the boss schedule.
            BossOnly = new[] { "GreatVineBoundDead", "EmberNightBat", "AshenNightBat", "GorgonWillowQueen", "BriarMatriarch", "AshenWyrm",
                // Map event units, sent only by MapEvents.
                "CinderShade", "GloomMote", "SwiftGloomMote", "SwiftGloomMoth", "SwarmSkull", "SwarmNightBat" },
            ByMinute = new[]
            {
                /* 0 */ new[] { "AshSprite" },
                /* 1 */ new[] { "AshSprite" },
                /* 2 */ new[] { "AshSprite", "SporeMote" },
                /* 3 */ new[] { "VineBoundDead" },
                /* 4 */ new[] { "AshSprite", "VineBoundDead" },
                /* 5 */ new[] { "VineBoundDead" },
                /* 6 */ new[] { "BarkHusk", "SporeMote" },
                /* 7 */ new[] { "BarkHusk", "SporeMote", "SporeClump" },
                /* 8 */ new[] { "SporeClump", "ForestWisp" },
                /* 9 */ new[] { "VineBoundDead", "ForestWisp" },
                /* 10 */ new[] { "SporeClump" },
                /* 11 */ new[] { "GloomMoth", "ThornLion" },
                /* 12 */ new[] { "GloomMoth", "BarkHusk" },
                /* 13 */ new[] { "VineBoundDead", "BarkHusk" },
                /* 14 */ new[] { "VineBoundDead", "GloomMoth", "SporeClump" },
                /* 15 */ new[] { "HuntingGloomMoth" },
                /* 16 */ new[] { "BriarWitch", "BarkHusk", "ElderBarkHusk" },
                /* 17 */ new[] { "BriarWitch", "SporeClump", "SporeMote" },
                /* 18 */ new[] { "BriarWitch", "ThornLion" },
                /* 19 */ new[] { "BriarWitch", "ElderBarkHusk" },
                /* 20 */ new[] { "ThornLion", "ElderBarkHusk" },
                /* 21 */ new[] { "ThornLion" },
                /* 22 */ new[] { "BriarMatron", "ThornLion" },
                /* 23 */ new[] { "BarkHusk", "ElderBarkHusk", "BriarMatron" },
                /* 24 */ new[] { "BriarMatron" },
                /* 25 */ new[] { "LanternSkull", "BriarMatron", "GreatNightBat" },
                /* 26 */ new[] { "GorgonWillow" },
                /* 27 */ new[] { "GloomMoth", "GorgonWillow" },
                /* 28 */ new[] { "BriarWitch", "GloomMothQueen" },
                /* 29 */ new[] { "AshColossus", "SporeColossus", "ThornLionKing", "GloomMothQueen" },
            },
            Minimum = new[] { 50, 80, 100, 60, 110, 100, 30, 80, 80, 200, 100, 120, 80, 120, 300, 100, 100, 200, 60, 120, 100, 100, 80, 120, 300, 300, 100, 300, 250, 250 },
            Interval = new[] { 3f, 3f, 4f, 4f, 4f, 4f, 2f, 2f, 1f, 0.5f, 0.5f, 2f, 1f, 0.5f, 0.1f, 0.1f, 1f, 1f, 0.5f, 0.5f, 0.1f, 0.1f, 1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f },
        };
    }
}
