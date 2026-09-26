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

        /// <summary>Each stage's scripted formations (between the 5-minute chest elites, before the boss). Provisional.</summary>
        public static List<StageEventEntry> Events(int stage)
        {
            stage = Mathf.Clamp(stage, 1, 10);
            StageEventType S = StageEventType.Swarm, W = StageEventType.Wall, R = StageEventType.ClosingRing;
            StageEventType[] kinds;
            float speed = 1f;
            switch (stage)
            {
                case 1: kinds = new[] { S, S, S, S, S }; break;
                case 2: kinds = new[] { R, S, R, R, R }; break;
                case 3: kinds = new[] { W, W, S, W, W }; break;
                case 4: kinds = new[] { W, W, W, W, W }; break;
                case 5: kinds = new[] { S, S, S, S, S }; speed = 1.8f; break; // fast straight swarms
                case 6: kinds = new[] { S, S, W, S, S }; break;
                case 7: kinds = new[] { S, S, S, R, S }; break;               // ghost swarms
                case 8: kinds = new[] { R, R, R, R, R }; break;               // elite rings
                case 9: kinds = new[] { W, R, W, R, W }; break;
                default: kinds = new[] { S, W, R, S, W, R, S, W, R }; break;  // L10: everything, every 2.5 minutes
            }
            var events = new List<StageEventEntry>();
            float spacing = stage == 10 ? 150f : 300f;
            for (int i = 0; i < kinds.Length; i++)
            {
                int count = (stage == 8 ? 10 : 16) + 4 * i + (stage == 10 ? 8 : 0);
                events.Add(new StageEventEntry { TimeSeconds = 150f + spacing * i, Type = kinds[i], Count = count, Speed = speed });
            }
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
