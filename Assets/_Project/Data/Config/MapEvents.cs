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
            Chasers = StageEventType.Chasers;

        private static StageEventEntry E(int minute, StageEventType type, string typeId, int amount, float delay = 0f,
            int repeat = 0, float chance = 100f, float duration = 0f, bool horizontal = false) => new StageEventEntry
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

        /// <summary>The VS map events of <paramref name="stage"/>, sorted by time; null when the level has no table yet.</summary>
        public static List<StageEventEntry> For(int stage)
        {
            List<StageEventEntry> events = null;
            switch (Mathf.Clamp(stage, 1, 10))
            {
                case 1: events = MadForest(); break;
                case 2: events = InlaidLibrary(); break;
            }
            events?.Sort((a, b) => a.TimeSeconds.CompareTo(b.TimeSeconds));
            return events;
        }
    }
}
