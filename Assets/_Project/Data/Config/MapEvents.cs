using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// Each level's VS map events, row by row from its source stage's "Waves" table (raw pages fetched 2026-10-27; parsed
    /// rows and event rules in Docs/VS_Audit_20261027). A {{map event|...}} row reads (Template:Map_event and its /doc):
    /// - it belongs to its minute and fires "delay" seconds in ("0:ss"); delay defaults to 0;
    /// - "repeat" is the number of extra firings (the template shows "x(repeat+1)"); the wiki never states the spacing,
    ///   and its rows read as "delay" apart (Flower Wall "repeat=5|delay=10" is listed from 25:10), so that is used;
    /// - "chance" defaults to 100 and is rolled for every firing, divided by Luck (Enemies page);
    /// - amount defaults come from the event pages (Bat Swarm 50, Ghost Swarm 20).
    /// Levels without a table here still use the older Soul Hunter schedule (WaveTables.Events).
    /// </summary>
    public static class MapEvents
    {
        private const StageEventType Swarm = StageEventType.Swarm, Wall = StageEventType.Wall, TrapRing = StageEventType.TrapRing,
            Chasers = StageEventType.Chasers, PulsingRing = StageEventType.PulsingRing, PileRing = StageEventType.PileRing,
            StarRain = StageEventType.StarRain, Stream = StageEventType.Stream;

        private static StageEventEntry E(int minute, StageEventType type, string typeId, int amount, float delay = 0f,
            int repeat = 0, float chance = 100f, float duration = 0f, bool horizontal = false, float interval = 0f, float range = 0f) => new StageEventEntry
        {
            TimeSeconds = minute * 60f + delay,
            Type = type,
            EnemyTypeId = typeId,
            Count = Mathf.Max(1, amount),
            Repeat = 1 + Mathf.Max(0, repeat),
            RepeatDelaySeconds = Mathf.Max(0.1f, delay),
            ChancePercent = chance,
            DurationSeconds = duration,
            Horizontal = horizontal,
            IntervalSeconds = interval,
            Range = range,
        };

        // Mad Forest "Waves", the Map events column. Bat Swarm: "summons 50 Pipeestrellos that move in a straight line
        // across the map"; Ghost Swarm: "summons 20 Swarm Ghost"; Flower Wall: "a ring of durable Flower Wall units around
        // the player, which slowly creeps inward". Its rows give no amount; Soul Hunter's Thorn Circle has 30 (balance).
        private const int Bats = 50, Ghosts = 20, Walls = 30;
        private static List<StageEventEntry> MadForest() => new List<StageEventEntry>
        {
            E(2, Swarm, "SwarmBat", Bats, delay: 5, repeat: 2),
            E(3, Swarm, "SwarmBat", Bats, delay: 5, repeat: 1, chance: 10),
            E(4, Swarm, "SwarmBat", Bats, delay: 5, repeat: 1, chance: 10),
            E(5, TrapRing, "ThornWall", Walls, duration: 30),
            E(6, Swarm, "SwarmBat", Bats, delay: 5, repeat: 1, chance: 10),
            E(7, Swarm, "SwarmBat", Bats, delay: 5, repeat: 5, chance: 80),
            E(8, Swarm, "SwarmBat", Bats, delay: 15, repeat: 2, chance: 80),
            E(9, Swarm, "SwarmBat", Bats, delay: 15, repeat: 2, chance: 70),
            E(10, TrapRing, "ThornWall", Walls, duration: 60),
            E(11, Swarm, "SwarmBat", 50, delay: 5, repeat: 1, chance: 10),
            E(12, Swarm, "SwarmBat", Bats, delay: 5, repeat: 1, chance: 10),
            E(13, Swarm, "SwarmWisp", Ghosts, delay: 1.2f, repeat: 20, chance: 70),
            E(13, Swarm, "SwarmWisp", Ghosts, delay: 2.3f, repeat: 20, chance: 70),
            E(15, TrapRing, "ThornWall", Walls, chance: 80, duration: 60),
            E(20, Swarm, "SwarmBat", Bats, delay: 1.2f, repeat: 20, chance: 70),
            E(20, Swarm, "SwarmBat", Bats, delay: 2.3f, repeat: 20, chance: 70),
            E(25, TrapRing, "ThornWall", Walls, delay: 10, repeat: 5, duration: 10),
            E(27, Swarm, "SwarmWisp", Ghosts),
            E(27, Swarm, "SwarmWisp", Ghosts, delay: 3, repeat: 19),
            E(29, Swarm, "SwarmBat", Bats),
            E(29, Swarm, "SwarmBat", Bats, delay: 3, repeat: 19),
        };

        // Inlaid Library "Waves", the Map events column (Level 2). Shade Bomb: "summons Red Shades that chases the player
        // and self-destruct after a couple of seconds" (Sig.ra Rossi: "exploding 2 seconds later for 50 damage"). Medusa
        // Wall: "a line of Medusa Heads or Small Medusa Heads that randomly appears from either the left or right side and
        // travels across the screen in a wavy pattern"; Medusa Swarm: "a group of enemies that travel horizontally".
        // Skull Swarm: "a group of enemies that beeline through the center of the screen at high speed"; 20:05 / 25:05
        // "Every 10 seconds a new wave spawns, each double in size starting from 1 to 32, and one more 32". Bat Swarm 50.
        private static List<StageEventEntry> InlaidLibrary() => new List<StageEventEntry>
        {
            E(0, Chasers, "CinderShade", 1, delay: 0.1f), // Shade Bomb
            E(2, Wall, "GloomMote", 6, horizontal: true), // Medusa Wall
            E(2, Swarm, "GloomMote", 12, delay: 5, repeat: 4, chance: 70, horizontal: true), // Medusa Swarm
            E(5, Wall, "SwiftGloomMote", 12, horizontal: true), // Medusa Wall
            E(5, Swarm, "GloomMote", 12, delay: 5, repeat: 5, chance: 70, horizontal: true), // Medusa Swarm
            E(6, Chasers, "CinderShade", 1, delay: 10, repeat: 4, chance: 50), // Shade Bomb
            E(7, Chasers, "CinderShade", 1, delay: 5, repeat: 8, chance: 80), // Shade Bomb
            E(9, Chasers, "CinderShade", 2, delay: 20, repeat: 2, chance: 50), // Shade Bomb
            E(10, Chasers, "CinderShade", 1), // Shade Bomb
            E(10, Chasers, "CinderShade", 2, delay: 10, repeat: 3, chance: 80), // Shade Bomb
            E(12, Swarm, "SwiftGloomMote", 12, horizontal: true), // Medusa Swarm
            E(12, Wall, "GloomMote", 12, delay: 5, repeat: 5, chance: 70, horizontal: true), // Medusa Wall
            E(15, Wall, "SwiftGloomMote", 12, horizontal: true), // Medusa Wall
            E(15, Wall, "GloomMoth", 6, delay: 13, horizontal: true), // Medusa Wall
            E(15, Swarm, "SwiftGloomMote", 12, delay: 2, repeat: 25, chance: 90, horizontal: true), // Medusa Swarm
            E(17, Chasers, "CinderShade", 3, delay: 5, repeat: 4, chance: 50), // Shade Bomb
            E(20, Chasers, "CinderShade", 2, delay: 4, repeat: 25, chance: 50), // Shade Bomb
            E(20, Swarm, "SwarmSkull", 1), // Skull Swarm
            E(20, Swarm, "SwarmSkull", 2, delay: 5), // Skull Swarm
            E(20, Swarm, "SwarmSkull", 4, delay: 15), // Skull Swarm
            E(20, Swarm, "SwarmSkull", 8, delay: 25), // Skull Swarm
            E(20, Swarm, "SwarmSkull", 16, delay: 35), // Skull Swarm
            E(20, Swarm, "SwarmSkull", 32, delay: 45), // Skull Swarm
            E(20, Swarm, "SwarmSkull", 32, delay: 55), // Skull Swarm
            E(22, Chasers, "CinderShade", 3, delay: 2, repeat: 25, chance: 50), // Shade Bomb
            E(25, Swarm, "SwarmNightBat", 50), // Bat Swarm
            E(25, Swarm, "SwarmSkull", 1), // Skull Swarm
            E(25, Swarm, "SwarmSkull", 2, delay: 5), // Skull Swarm
            E(25, Swarm, "SwarmSkull", 4, delay: 15), // Skull Swarm
            E(25, Swarm, "SwarmSkull", 8, delay: 25), // Skull Swarm
            E(25, Swarm, "SwarmSkull", 16, delay: 35), // Skull Swarm
            E(25, Swarm, "SwarmSkull", 32, delay: 45), // Skull Swarm
            E(25, Swarm, "SwarmSkull", 32, delay: 55), // Skull Swarm
            E(27, Swarm, "SwiftGloomMoth", 12, horizontal: true), // Medusa Swarm
            E(27, Wall, "SwiftGloomMoth", 12, delay: 2, repeat: 25, chance: 90, horizontal: true), // Medusa Wall
            E(29, Chasers, "CinderShade", 4, delay: 1, repeat: 50, chance: 60), // Shade Bomb
        };

        // Dairy Plant "Waves", the Map events column (Level 3). Pile Assault: "a ring of enemies that surrounds the player
        // and attacks from a distance, shooting bullets" (the Twin Snakes, Twin Demons and Lost Twin statboxes: they cannot
        // move and shoot every 2 / 1.5 / 1 s). Swarm: "a large group of enemies that set off to attack the player" (Milk
        // Elementals and Skeletons home). Jellyfish Wall: "an ellipsoid ring around the player ... periodically closes in
        // on and moves away". Jellyfish Swarm: "slowly travels across the screen". Minotaur Rush: "travels across the
        // screen at high speed". The Stalker rows (0:00 1%, 8:00 30%, 12:00 30%) are Death's Shadow (StageHazardTable).
        private static List<StageEventEntry> DairyPlant() => new List<StageEventEntry>
        {
            E(1, PileRing, "WellSerpents", 20, duration: 5), // Pile Assault
            E(1, Swarm, "SmokeWisp", 25, delay: 15, repeat: 3, duration: 30), // Swarm
            E(2, Swarm, "SmokeWisp", 25, delay: 15, repeat: 2, duration: 30), // Swarm
            E(3, PulsingRing, "RingEmber", 80, duration: 30), // Jellyfish Wall
            E(4, PileRing, "WellSerpents", 20, chance: 50, duration: 5), // Pile Assault
            E(5, PileRing, "WellSerpents", 20, duration: 60), // Pile Assault
            E(5, PileRing, "WellSerpents", 20, delay: 30, duration: 30), // Pile Assault
            E(6, Swarm, "ThickSmokeWisp", 50, delay: 30, duration: 30), // Swarm
            E(7, PulsingRing, "RingEmber", 40, duration: 30), // Jellyfish Wall
            E(7, PulsingRing, "RingEmber", 40, delay: 2, chance: 80, duration: 30), // Jellyfish Wall
            E(8, Swarm, "BurntBones", 50, delay: 30, duration: 30), // Swarm
            E(9, Swarm, "StampedeOxDemon", 50), // Minotaur Rush
            E(9, Swarm, "StampedeOxDemon", 50, delay: 16, chance: 60, duration: 15), // Minotaur Rush
            E(10, Swarm, "StampedeOxDemon", 25, delay: 5, repeat: 6, chance: 50, duration: 15), // Minotaur Rush
            E(10, PileRing, "BellImps", 10, delay: 10, repeat: 4, chance: 50, duration: 20), // Pile Assault
            E(11, Swarm, "BurntBones", 50, delay: 10, repeat: 4, duration: 30), // Swarm
            E(12, PileRing, "ShrineIdol", 10, delay: 10, repeat: 3, chance: 50, duration: 20), // Pile Assault
            E(12, Swarm, "BurntBones", 50, delay: 5, repeat: 10, duration: 30), // Swarm
            E(18, PulsingRing, "RingEmber", 40, delay: 12, chance: 80, duration: 30), // Jellyfish Wall
            E(18, PulsingRing, "RingEmber", 40, delay: 14, chance: 80, duration: 30), // Jellyfish Wall
            E(19, Swarm, "DriftEmber", 50, chance: 80, duration: 10), // Jellyfish Swarm
            E(19, Swarm, "DriftEmber", 50, delay: 10, repeat: 2, chance: 80, duration: 10), // Jellyfish Swarm
            E(20, PileRing, "BellImps", 20, delay: 5, repeat: 6, chance: 80, duration: 30), // Pile Assault
            E(23, Swarm, "StampedeOxDemon", 25, chance: 60, duration: 10), // Minotaur Rush
            E(23, Swarm, "StampedeOxDemon", 25, delay: 5, repeat: 5, chance: 60, duration: 10), // Minotaur Rush
        };

        // Gallo Tower "Waves", the Map events column (Level 4). Shooting Star: amount stars per repeat, interval apart,
        // within range of the player (Soul Meteors). Dragon Swarm: "groups of Serpentine Dragon Shrimps ... which travel
        // horizontally accross the map". Dragon Stream: "periodically at set location until the end of its duration. Spawns
        // alternate between left and right". Pile Assault: Undead Mages (Gate Sorcerers) round the player. The two 30:00
        // Shooting Star rows (one "repeat=Infinite") are not played: Soul Hunter's stage ends at 30:00 and the next begins.
        private static List<StageEventEntry> GalloTower() => new List<StageEventEntry>
        {
            E(0, StarRain, null, 2, delay: 5, interval: 1.0f, range: 1f), // Shooting Star
            E(2, Swarm, "EmberSerpent", 1, duration: 30, horizontal: true), // Dragon Swarm
            E(2, Swarm, "EmberSerpent", 3, delay: 5, repeat: 4, chance: 70, duration: 30, horizontal: true), // Dragon Swarm
            E(4, StarRain, null, 2, delay: 5, chance: 50, interval: 1.0f, range: 1f), // Shooting Star
            E(4, StarRain, null, 2, delay: 10, repeat: 2, chance: 50, interval: 1.0f, range: 1f), // Shooting Star
            E(5, Swarm, "EmberSerpent", 12, duration: 30, horizontal: true), // Dragon Swarm
            E(5, Swarm, "EmberSerpent", 12, delay: 5, repeat: 5, chance: 70, duration: 30, horizontal: true), // Dragon Swarm
            E(6, Stream, "EmberSerpent", 5, delay: 10, chance: 70, duration: 60, interval: 1.33f), // Dragon Stream
            E(7, Stream, "EmberSerpent", 5, delay: 5, chance: 70, duration: 60, interval: 1.33f), // Dragon Stream
            E(9, PileRing, "GateSorcerer", 12, delay: 20, chance: 80, duration: 30, range: 0.4f), // Pile Assault
            E(10, PileRing, "GateSorcerer", 6, delay: 10, duration: 30), // Pile Assault
            E(10, PileRing, "GateSorcerer", 6, delay: 20, chance: 60, duration: 30), // Pile Assault
            E(12, Swarm, "EmberSerpent", 12, duration: 30, horizontal: true), // Dragon Swarm
            E(12, Swarm, "EmberSerpent", 12, delay: 5, repeat: 5, chance: 70, duration: 20, horizontal: true), // Dragon Swarm
            E(15, Stream, "EmberSerpent", 5, duration: 60, interval: 1.0f), // Dragon Stream
            E(15, StarRain, null, 12, interval: 0.1f, range: 0.5f), // Shooting Star
            E(15, StarRain, null, 3, delay: 5, repeat: 6, chance: 50, interval: 0.1f, range: 1f), // Shooting Star
            E(16, Swarm, "BlazeSerpent", 12, duration: 30, horizontal: true), // Dragon Swarm
            E(16, Swarm, "BlazeSerpent", 6, delay: 13, duration: 30, horizontal: true), // Dragon Swarm
            E(16, Swarm, "BlazeSerpent", 3, delay: 2, repeat: 12, chance: 90, duration: 20, horizontal: true), // Dragon Swarm
            E(22, StarRain, null, 3, delay: 2, repeat: 25, interval: 0.1f, range: 1f), // Shooting Star
            E(27, Swarm, "BlazeSerpent", 12, horizontal: true), // Dragon Swarm
            E(27, Swarm, "BlazeSerpent", 6, delay: 2, repeat: 10, chance: 90, duration: 20, horizontal: true), // Dragon Swarm
            E(29, Swarm, "BlazeSerpent", 12, duration: 30, horizontal: true), // Dragon Swarm
            E(29, Swarm, "BlazeSerpent", 2, delay: 1.1f, repeat: 50, chance: 90, duration: 20, horizontal: true), // Dragon Swarm
            E(29, Swarm, "EmberSerpent", 2, delay: 1.0f, repeat: 50, duration: 20, horizontal: true), // Dragon Swarm
        };

        /// <summary>The VS map events of <paramref name="stage"/>, sorted by time; null when the level has no table yet.</summary>
        public static List<StageEventEntry> For(int stage)
        {
            List<StageEventEntry> events = null;
            switch (Mathf.Clamp(stage, 1, 10))
            {
                case 1: events = MadForest(); break;
                case 2: events = InlaidLibrary(); break;
                case 3: events = DairyPlant(); break;
                case 4: events = GalloTower(); break;
            }
            events?.Sort((a, b) => a.TimeSeconds.CompareTo(b.TimeSeconds));
            return events;
        }
    }
}
