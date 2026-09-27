using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// Soul Hunter's per-stage VS minute tables (Part 3 of the owner-approved crowd plan, 2026-09-26). VS gives every
    /// minute its own minimum and interval, rising and falling (vampire.survivors.wiki, Mad Forest: 15, 30, 50, 40 ...,
    /// then 10 at 5:00 with tougher enemies). Each stage here gets its own curve: its own opening crowd, peak, minute-to-
    /// minute swell, and "dip" minutes with few enemies but many elites, at stage-specific times. All numbers are
    /// provisional balance; the builder writes them into each stage's WaveData.
    /// </summary>
    public static class WaveTables
    {
        /// <summary>Stage 1-10 wave assets (the EnemySpawner._waves order in Main_Gameplay).</summary>
        public static readonly string[] StageWaveAssets =
        {
            "Assets/_Project/Data/Config/Graveyard Wave.asset",
            "Assets/_Project/Data/Config/Forest Swarm.asset",
            "Assets/_Project/Data/Config/Fire Demons.asset",
            "Assets/_Project/Data/Config/Castle Gargoyles.asset",
            "Assets/_Project/Data/Config/Poison Slimes.asset",
            "Assets/_Project/Data/Config/Ice Wraiths.asset",
            "Assets/_Project/Data/Config/Spectral Swarm.asset",
            "Assets/_Project/Data/Config/Elite Guards.asset",
            "Assets/_Project/Data/Config/Reaper Swarm.asset",
            "Assets/_Project/Data/Config/Shadow Kael Clones.asset",
        };

        /// <summary>The boss arrives at 25:00, so the table covers minutes 0-24.</summary>
        public const int Minutes = 25;

        // Stage 1 follows VS's Mad Forest opening closely (15 at 0:00, about 30 at 1:00, 50 by 2:00-3:00, about 100
        // around 8:00-12:00); later stages open bigger and peak higher, below the 300-enemy cap.
        public static int OpeningMinimum(int stage) => 15 + 5 * (Mathf.Clamp(stage, 1, 10) - 1);
        public static int PeakMinimum(int stage) => Mathf.Min(290, 150 + 16 * (Mathf.Clamp(stage, 1, 10) - 1));

        /// <summary>Dip minutes: every fifth minute, shifted per stage, never the opening minute.</summary>
        public static bool IsDip(int stage, int minute) => minute > 0 && (minute + Mathf.Clamp(stage, 1, 10)) % 5 == 0;

        // ---------------------------------------------------------------- Part 4: stage patterns
        // Owner-approved table (2026-09-26): L1 all sides, swarms; L2 all sides, closing rings; L3 all sides (houses
        // block spawns), walls; L4 mostly left/right, walls; L5 all sides, slow heavy crowd, fast straight swarms;
        // L6 mostly top/bottom, fast; L7 ghost swarms; L8 elites, rings; L9 walls and rings; L10 everything, densest.

        public static SpawnSide SideFor(int stage)
        {
            switch (Mathf.Clamp(stage, 1, 10))
            {
                case 4: return SpawnSide.LeftRight;
                case 6: return SpawnSide.TopBottom;
                default: return SpawnSide.AllSides;
            }
        }

        /// <summary>Corridor half-width per stage: the Ruined Castle is a walled corridor (owner, 2026-09-26); 0 = open.</summary>
        public static float CorridorHalfWidth(int stage) => Mathf.Clamp(stage, 1, 10) == 4 ? 9f : 0f;

        /// <summary>Share of spawns kept to the stage's sides: all of them in a corridor (VS: only from the ends).</summary>
        public static float SideBiasFor(int stage) => CorridorHalfWidth(stage) > 0f ? 1f : 0.8f;

        /// <summary>
        /// Each stage's map events over its 30 minutes, VS-dense (vampire.survivors.wiki/w/Mad_Forest and /w/Enemies:
        /// swarms most minutes from 2:00 to 12:00 with an 80% chance and repeats, a ring or wall at 5, 10, 15 and 25,
        /// occasional events mid-stage, heavy repeated swarms at 20, 27 and 29; each chance is divided by Luck).
        /// The kinds per stage follow the owner-approved table (2026-09-26). Counts and chances are provisional.
        /// </summary>
        public static List<StageEventEntry> Events(int stage)
        {
            stage = Mathf.Clamp(stage, 1, 10);
            StageEventType S = StageEventType.Swarm, W = StageEventType.Wall, R = StageEventType.ClosingRing;
            StageEventType small, big;
            float speed = 1f;
            int baseCount = 12;
            switch (stage)
            {
                case 1: small = S; big = S; break;                            // swarms
                case 2: small = R; big = R; break;                            // mostly closing rings
                case 3: small = W; big = S; break;                            // mostly walls
                case 4: small = W; big = W; break;                            // corridor walls
                case 5: small = S; big = S; speed = 1.8f; break;              // fast straight swarms
                case 6: small = S; big = W; break;                            // mostly swarms
                case 7: small = S; big = R; break;                            // (ghost) swarms
                case 8: small = R; big = R; baseCount = 6; break;             // elite rings
                case 9: small = W; big = R; break;                            // walls and rings
                default: small = S; big = R; baseCount = 16; break;          // L10: everything
            }
            var events = new List<StageEventEntry>();
            void Add(int minute, StageEventType type, int count, float chance, int repeat, float delay)
            {
                if (stage == 10) type = (StageEventType)(minute % 3); // L10 cycles Swarm, Wall, ClosingRing
                else if (stage == 9 && type == small) type = minute % 2 == 0 ? W : R; // L9 alternates
                else if (stage == 2 && type == small && minute % 4 == 0) type = S; // a few swarms among the rings
                events.Add(new StageEventEntry
                {
                    TimeSeconds = minute * 60f, Type = type, Count = count, Speed = speed,
                    ChancePercent = chance, Repeat = repeat, RepeatDelaySeconds = delay,
                });
            }
            foreach (int m in new[] { 2, 3, 4, 6, 7, 8, 9, 11, 12 })
                Add(m, small, baseCount + m / 2, m == 2 ? 100f : 80f, 2, 5f);   // VS: most early minutes, 80%, twice
            foreach (int m in new[] { 5, 10, 15, 25 })
                Add(m, big, baseCount + 8 + m / 2, 100f, 1, 5f);                // VS: the wall/ring minutes
            foreach (int m in new[] { 13, 17, 22 })
                Add(m, small, baseCount + m / 2, 70f, 3, 4f);                   // occasional mid-stage events
            Add(20, small, baseCount + 10, 100f, 5, 3f);                        // VS: heavy at 20
            Add(27, small, baseCount + 12, 100f, 6, 3f);                        // VS: guaranteed, repeated, at 27 and 29
            Add(29, small, baseCount + 14, 100f, 6, 3f);
            if (stage == 10)                                                    // L10: the busiest schedule
                foreach (int m in new[] { 14, 16, 18, 24, 26, 28 })
                    Add(m, small, baseCount + m / 2, 80f, 2, 5f);
            events.Sort((a, b) => a.TimeSeconds.CompareTo(b.TimeSeconds));
            return events;
        }

        /// <summary>
        /// A flat spawn direction for a stage's side rule. roll01s are uniform [0, 1): whether this spawn keeps to
        /// the stage's sides (bias), which side, and the angle within a 70-degree fan around it.
        /// </summary>
        public static Vector3 SpawnDirection(SpawnSide side, float bias, float sideRoll, float whichRoll, float angleRoll)
        {
            float angle;
            if (side == SpawnSide.AllSides || sideRoll >= bias)
                angle = angleRoll * 360f;
            else
            {
                float centre = side == SpawnSide.LeftRight ? (whichRoll < 0.5f ? 90f : 270f) : (whichRoll < 0.5f ? 0f : 180f);
                angle = centre + Mathf.Lerp(-35f, 35f, angleRoll);
            }
            return Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
        }

        public static List<MinuteWave> Build(int stage, float baseInterval)
        {
            stage = Mathf.Clamp(stage, 1, 10);
            int opening = OpeningMinimum(stage), peak = PeakMinimum(stage);
            var table = new List<MinuteWave>();
            for (int m = 0; m < Minutes; m++)
            {
                float t = m / (float)(Minutes - 1);
                float target = opening + (peak - opening) * Mathf.Pow(t, 0.6f); // a fast VS-like build-up, then a slower climb
                target *= 1f + 0.12f * Mathf.Sin(0.9f * m + stage);         // the swell differs per stage
                float interval = baseInterval * Mathf.Lerp(1f, 0.4f, t);
                float elite = 0f;
                if (IsDip(stage, m))
                {
                    target = Mathf.Max(8f, target * 0.3f);                  // few enemies ...
                    interval *= 1.5f;
                    elite = Mathf.Min(0.6f, 0.3f + 0.02f * stage);         // ... but tough ones
                }
                table.Add(new MinuteWave
                {
                    Minute = m,
                    Minimum = Mathf.RoundToInt(target),
                    Interval = Mathf.Max(0.1f, (float)System.Math.Round(interval, 3)),
                    EliteBonus = (float)System.Math.Round(elite, 3),
                });
            }
            return table;
        }
    }
}
