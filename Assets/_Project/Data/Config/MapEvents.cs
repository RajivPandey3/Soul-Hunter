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
        private const StageEventType Swarm = StageEventType.Swarm, TrapRing = StageEventType.TrapRing;

        private static StageEventEntry E(int minute, StageEventType type, string typeId, int amount, float delay = 0f,
            int repeat = 0, float chance = 100f, float duration = 0f) => new StageEventEntry
        {
            TimeSeconds = minute * 60f + delay,
            Type = type,
            EnemyTypeId = typeId,
            Count = Mathf.Max(1, amount),
            Repeat = 1 + Mathf.Max(0, repeat),
            RepeatDelaySeconds = Mathf.Max(0.1f, delay),
            ChancePercent = chance,
            DurationSeconds = duration,
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

        /// <summary>The VS map events of <paramref name="stage"/>, sorted by time; null when the level has no table yet.</summary>
        public static List<StageEventEntry> For(int stage)
        {
            List<StageEventEntry> events = null;
            switch (Mathf.Clamp(stage, 1, 10))
            {
                case 1: events = MadForest(); break;
            }
            events?.Sort((a, b) => a.TimeSeconds.CompareTo(b.TimeSeconds));
            return events;
        }
    }
}
