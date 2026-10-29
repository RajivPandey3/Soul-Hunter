using System.Diagnostics;
using System.Text;

namespace SoulHunter.Gameplay.Combat
{
    /// <summary>
    /// Measuring only (AutoplayBot -sh-autoplay-hit-timing): the time weapon hits take in a release build, summed per frame.
    /// A packed Level 3 horde took 400-700 trigger hits in one physics step (2026-10-17); a hit that dealt damage cost about
    /// 55 us in the release build, an immune one under 1 us, so the parts of a damaging hit are timed too.
    /// Off (one bool test per hit and part) unless the bot turns it on.
    /// </summary>
    public static class HitTiming
    {
        public static bool Enabled;
        public static long FindTicks, DamagingTicks, EmptyTicks;
        public static int FindCalls, DamagingCalls, EmptyCalls;

        /// <summary>The parts of a damaging hit inside HealthController.TakeDamage.</summary>
        public enum Part { Multiplier, Stats, Observer, Number, OnDamaged, Knockback, HealthChanged, Die,
            DeathOnDied, DeathDeactivate, DeathPuff, DeathBlood, DeathCorpse, DeathKillStats, DeathGem,
            GemActivate, GemReset, GemTier, Count }
        private static readonly long[] PartTicks = new long[(int)Part.Count];
        private static readonly int[] PartCalls = new int[(int)Part.Count];

        public static long Now => Stopwatch.GetTimestamp();

        /// <summary>Adds the time since <paramref name="started"/> to <paramref name="part"/> (when timing is on).</summary>
        public static void Add(Part part, long started)
        {
            if (!Enabled) return;
            PartTicks[(int)part] += Stopwatch.GetTimestamp() - started;
            PartCalls[(int)part]++;
        }

        // Each OnDied listener, by its class and method (2026-10-29: OnDied's timed parts covered 2.1 of its 5.2 ms).
        private static readonly System.Collections.Generic.Dictionary<string, long> ListenerTicks = new System.Collections.Generic.Dictionary<string, long>();
        private static readonly System.Collections.Generic.Dictionary<string, int> ListenerCalls = new System.Collections.Generic.Dictionary<string, int>();
        private static readonly System.Collections.Generic.Dictionary<System.Reflection.MethodInfo, string> ListenerNames = new System.Collections.Generic.Dictionary<System.Reflection.MethodInfo, string>();

        /// <summary>Runs every listener of <paramref name="died"/> in turn, timing each (when timing is on).</summary>
        public static void RunTimed(System.Action died)
        {
            foreach (var d in died.GetInvocationList())
            {
                long t = Stopwatch.GetTimestamp();
                ((System.Action)d).Invoke();
                t = Stopwatch.GetTimestamp() - t;
                if (!ListenerNames.TryGetValue(d.Method, out var name))
                    ListenerNames[d.Method] = name = (d.Method.DeclaringType != null ? d.Method.DeclaringType.Name : "?") + "." + d.Method.Name;
                ListenerTicks.TryGetValue(name, out long sum); ListenerTicks[name] = sum + t;
                ListenerCalls.TryGetValue(name, out int n); ListenerCalls[name] = n + 1;
            }
        }

        public static void Clear()
        {
            ListenerTicks.Clear();
            ListenerCalls.Clear();
            FindTicks = DamagingTicks = EmptyTicks = 0;
            FindCalls = DamagingCalls = EmptyCalls = 0;
            System.Array.Clear(PartTicks, 0, PartTicks.Length);
            System.Array.Clear(PartCalls, 0, PartCalls.Length);
        }

        public static double Ms(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

        /// <summary>"find 0.4 ms x600; damaging 3.1 ms x37; empty 0.1 ms x564 | Number 1.2 ms x37; Die 0.8 ms x5".</summary>
        public static string Describe()
        {
            var sb = new StringBuilder();
            sb.Append($"find {Ms(FindTicks):0.00} ms x{FindCalls}; damaging {Ms(DamagingTicks):0.00} ms x{DamagingCalls}; empty {Ms(EmptyTicks):0.00} ms x{EmptyCalls} |");
            for (int i = 0; i < PartTicks.Length; i++)
                if (PartCalls[i] > 0) sb.Append($" {(Part)i} {Ms(PartTicks[i]):0.00} ms x{PartCalls[i]};");
            if (ListenerTicks.Count > 0)
            {
                sb.Append(" | died:");
                foreach (var entry in ListenerTicks) sb.Append($" {entry.Key} {Ms(entry.Value):0.00} ms x{ListenerCalls[entry.Key]};");
            }
            return sb.ToString();
        }
    }
}
