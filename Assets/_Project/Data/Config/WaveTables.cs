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
