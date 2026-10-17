using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SoulHunter.Core.Services
{
    /// <summary>
    /// Spike hunting (owner, 2026-10-16: "timer lagao aur L5 chalao"): a release-build run on Level 5 had one 9,050 ms
    /// frame starting the moment a chest elite died and its chest was picked up, and a 348 ms frame when a chest opened.
    /// <c>using (SlowSpan.Measure("ChestUI.OpenChest")) { ... }</c> times a suspect section. Sections of
    /// <see cref="RecordMs"/> or more are kept until <see cref="TakeRecent"/> (AutoplayBot adds them to its hitch event),
    /// and sections of <see cref="WarnMs"/> or more are also logged, so a release Player.log shows where a long frame went.
    /// The sections open on the main thread are also kept as a stack, so <see cref="StartWatchdog"/> can tell where the
    /// main thread is stuck when frames stop (a run on 2026-10-16 froze for 8 minutes after a chest was picked up).
    /// </summary>
    public readonly struct SlowSpan : IDisposable
    {
        /// <summary>Sections at least this long are recorded (5 ms; a measuring run may lower it, -sh-autoplay-span-ms).</summary>
        public static double RecordMs = 5.0;
        public const double WarnMs = 50.0;
        private static readonly List<(int Frame, string Entry)> Recent = new List<(int, string)>();

        private const int MaxDepth = 32;
        private static readonly string[] Open = new string[MaxDepth];
        private static volatile int _depth;
        private static int _mainThread = -1;
        private static volatile int _beat;
        private static Thread _watchdog;

        private readonly string _name;
        private readonly long _start;
        private readonly bool _pushed;

        private SlowSpan(string name)
        {
            _name = name;
            _start = Stopwatch.GetTimestamp();
            _pushed = _mainThread == Thread.CurrentThread.ManagedThreadId && _depth < MaxDepth;
            if (_pushed) { Open[_depth] = name; _depth++; }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            // The section also shows in the Unity profiler, which does not see inside script code (found 2026-10-17:
            // an 18 ms ProjectileDamage.OnTriggerEnter with nothing under it).
            _sampled = _mainThread == Thread.CurrentThread.ManagedThreadId;
            if (_sampled) UnityEngine.Profiling.Profiler.BeginSample(name);
#endif
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private readonly bool _sampled;
#endif

        public static SlowSpan Measure(string name) => new SlowSpan(name);

        public void Dispose()
        {
            if (_name == null) return;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (_sampled) UnityEngine.Profiling.Profiler.EndSample();
#endif
            if (_pushed && _depth > 0) _depth--;
            double ms = (Stopwatch.GetTimestamp() - _start) * 1000.0 / Stopwatch.Frequency;
            if (ms < RecordMs) return;
            string entry = $"{_name} {ms:0} ms";
            if (Thread.CurrentThread.ManagedThreadId != _mainThread) { if (ms >= WarnMs) Debug.LogWarning($"[SlowSpan] {entry} (worker)"); return; }
            if (Recent.Count < 256) Recent.Add((Time.frameCount, entry));
            if (ms >= WarnMs) Debug.LogWarning($"[SlowSpan] {entry} (frame {Time.frameCount}, t {Time.realtimeSinceStartup:0.0} s)");
        }

        /// <summary>The sections recorded in the last two frames (a long frame may come before or after the caller's
        /// Update), then clears them all.</summary>
        public static string TakeRecent()
        {
            int from = Time.frameCount - 2;
            var parts = new List<string>();
            foreach (var r in Recent) if (r.Frame >= from) parts.Add(r.Entry);
            Recent.Clear();
            return string.Join("; ", parts);
        }

        /// <summary>Drops sections older than two frames and tells the watchdog a frame went by (every ordinary frame).</summary>
        public static void Prune()
        {
            _beat++;
            Recent.RemoveAll(r => r.Frame < Time.frameCount - 2);
        }

        /// <summary>The sections open on the main thread right now, outermost first.</summary>
        public static string OpenSections()
        {
            int depth = _depth;
            var parts = new List<string>();
            for (int i = 0; i < depth && i < MaxDepth; i++) parts.Add(Open[i] ?? "?");
            return parts.Count > 0 ? string.Join(" > ", parts) : "(none)";
        }

        /// <summary>
        /// Called on the main thread (AutoplayBot). A worker thread watches <see cref="Prune"/>'s frame beat; once it has
        /// not moved for <paramref name="stallSeconds"/>, it writes the open sections to <paramref name="reportFile"/> and
        /// the log every second while the stall lasts (several samples show what a loop keeps entering).
        /// </summary>
        public static void StartWatchdog(string reportFile, float stallSeconds = 3f)
        {
            _mainThread = Thread.CurrentThread.ManagedThreadId;
            if (_watchdog != null) return;
            _watchdog = new Thread(() =>
            {
                int last = _beat;
                var since = Stopwatch.StartNew();
                int samples = 0;
                while (true)
                {
                    Thread.Sleep(250);
                    int now = _beat;
                    if (now != last) { last = now; since.Restart(); samples = 0; continue; }
                    if (now < 100) continue; // still loading: the beat starts with the run's first frames
                    if (since.Elapsed.TotalSeconds < stallSeconds || samples >= 30) continue;
                    if (since.Elapsed.TotalSeconds < stallSeconds + samples) continue;
                    samples++;
                    // CPU time tells a busy loop (about one core's worth per second) from a thread that waits.
                    double cpu = 0; int threads = 0;
                    try { var process = Process.GetCurrentProcess(); cpu = process.TotalProcessorTime.TotalSeconds; threads = process.Threads.Count; } catch (Exception) { }
                    string line = $"[SlowSpan] main thread stuck {since.Elapsed.TotalSeconds:0.0} s; process CPU {cpu:0.0} s; threads {threads}; open: {OpenSections()}";
                    try { File.AppendAllText(reportFile, line + Environment.NewLine); } catch (Exception) { }
                    Debug.LogWarning(line);
                }
            }) { IsBackground = true, Name = "SlowSpan watchdog" };
            _watchdog.Start();
        }
    }
}
