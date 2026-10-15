#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

namespace SoulHunter.Editor.Debugging
{
    /// <summary>
    /// Heavy-kill performance pass (2026-09-29): reads a profiler capture (.raw) written by the benchmark's
    /// -sh-benchmark-profile option in a development build and writes where the main thread's time goes: per marker,
    /// the average self time and total time per frame and the calls per frame, sorted by time.
    /// CLI: -executeMethod SoulHunter.Editor.Debugging.ProfileSummary.RunFromCommandLine -sh-profile &lt;file.raw&gt; -quit
    /// (writes &lt;file&gt;.summary.txt next to it).
    /// </summary>
    public static class ProfileSummary
    {
        public static void RunFromCommandLine()
        {
            var args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-sh-profile");
            if (i < 0 || i + 1 >= args.Length) { Debug.LogError("[ProfileSummary] Pass -sh-profile <file.raw>."); return; }
            // Several captures in one editor launch: -sh-profile "a.raw;b.raw".
            foreach (string path in args[i + 1].Split(';'))
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path.Trim())) Summarize(path.Trim());
        }

        [MenuItem("Soul Hunter/Debug/Summarize Profile")]
        private static void FromMenu()
        {
            string path = EditorUtility.OpenFilePanel("Profiler capture", "", "raw");
            if (!string.IsNullOrEmpty(path)) Summarize(path);
        }

        // Frames slower than this get their own marker list: 25 ms catches the stutters that break 60 FPS (2026-10-14).
        private const float SlowFrameMs = 25f;

        // Frames slower than this (a profiler start or stop, a level load) are left out of every average: one 2.1 s frame
        // in 165 added 13 ms to each marker it held (2026-10-15). They are still listed under the slowest frames.
        private const float OutlierMs = 250f;

        private static HashSet<int> _outliers = new HashSet<int>();

        private sealed class Totals { public double Self, Total, Calls; }

        public static string Summarize(string rawPath)
        {
            if (!ProfilerDriver.LoadProfile(rawPath, false)) { Debug.LogError("[ProfileSummary] Could not load " + rawPath); return null; }
            int first = ProfilerDriver.firstFrameIndex, last = ProfilerDriver.lastFrameIndex;
            _outliers = new HashSet<int>();
            for (int f = first; f <= last; f++)
                using (var raw = ProfilerDriver.GetRawFrameDataView(f, 0))
                    if (raw != null && raw.valid && raw.frameTimeMs >= OutlierMs) _outliers.Add(f);
            var byName = new Dictionary<string, Totals>();
            double frameMs = 0;
            int frames = 0;
            var children = new List<int>();
            // The slowest frames, each with its own marker totals: averages hide spikes (2026-10-09, 0.3-0.7 s hitches).
            var slow = new List<(int frame, double ms, Dictionary<string, double> totals)>();
            var stack = new Stack<int>();
            for (int f = first; f <= last; f++)
            {
                using (var view = ProfilerDriver.GetHierarchyFrameDataView(f, 0, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
                           HierarchyFrameDataView.columnTotalTime, false))
                {
                    if (view == null || !view.valid) continue;
                    if (_outliers.Contains(f))
                    {
                        slow.Add((f, view.frameTimeMs, TopTotals(view)));
                        continue;
                    }
                    frames++;
                    frameMs += view.frameTimeMs;
                    var seen = new HashSet<string>();
                    var frameTotals = view.frameTimeMs >= SlowFrameMs ? new Dictionary<string, double>() : null;
                    stack.Clear();
                    stack.Push(view.GetRootItemID());
                    while (stack.Count > 0)
                    {
                        int id = stack.Pop();
                        children.Clear();
                        view.GetItemChildren(id, children);
                        foreach (int child in children)
                        {
                            string name = view.GetItemName(child);
                            if (!byName.TryGetValue(name, out var t)) byName[name] = t = new Totals();
                            t.Self += view.GetItemColumnDataAsFloat(child, HierarchyFrameDataView.columnSelfTime);
                            // Total time counted once per name per frame, so nested repeats do not add up twice.
                            if (seen.Add(name))
                            {
                                float total = view.GetItemColumnDataAsFloat(child, HierarchyFrameDataView.columnTotalTime);
                                t.Total += total;
                                if (frameTotals != null) frameTotals[name] = total;
                            }
                            t.Calls += view.GetItemColumnDataAsFloat(child, HierarchyFrameDataView.columnCalls);
                            stack.Push(child);
                        }
                    }
                    if (frameTotals != null) slow.Add((f, view.frameTimeMs, frameTotals));
                }
            }
            var sb = new StringBuilder();
            sb.AppendLine($"Profile: {rawPath}");
            sb.AppendLine($"Frames: {frames}, average frame {(frames > 0 ? frameMs / frames : 0):F2} ms" +
                          (_outliers.Count > 0 ? $" ({_outliers.Count} frame(s) of {OutlierMs:0} ms or more left out of the averages)" : ""));
            AppendFrameSpread(sb, first, last);
            AppendPhases(sb, first, last, frames);
            AppendThreads(sb, first, last, frames);
            sb.AppendLine();
            sb.AppendLine("Soul Hunter scripts by total time (ms per frame | calls per frame):");
            foreach (var kv in byName.Where(k => k.Key.Contains("SoulHunter")).OrderByDescending(k => k.Value.Total).Take(40))
                sb.AppendLine($"  {kv.Value.Total / frames,8:F3}  {kv.Value.Calls / frames,8:F1}  {kv.Key}");
            if (byName.TryGetValue("GC.Alloc", out var gc))
                sb.AppendLine($"  GC.Alloc calls per frame: {gc.Calls / frames:F1}");
            sb.AppendLine();
            sb.AppendLine("Top 45 by self time (ms per frame | calls per frame):");
            foreach (var kv in byName.OrderByDescending(k => k.Value.Self).Take(45))
                sb.AppendLine($"  {kv.Value.Self / frames,8:F3}  {kv.Value.Calls / frames,8:F1}  {kv.Key}");
            sb.AppendLine();
            sb.AppendLine("Top 60 by total time (ms per frame):");
            foreach (var kv in byName.OrderByDescending(k => k.Value.Total).Take(60))
                sb.AppendLine($"  {kv.Value.Total / frames,8:F3}  {kv.Key}");
            sb.AppendLine();
            sb.AppendLine($"Slowest frames ({SlowFrameMs:0} ms or more: {slow.Count}); top markers by total time in each:");
            foreach (var s in slow.OrderByDescending(x => x.ms).Take(8))
            {
                sb.AppendLine($"  frame {s.frame}: {s.ms:F1} ms");
                foreach (var kv in s.totals.OrderByDescending(k => k.Value).Take(25))
                    sb.AppendLine($"      {kv.Value,9:F2}  {kv.Key}");
            }
            ProfilerDriver.ClearAllFrames();
            string output = Path.ChangeExtension(rawPath, ".summary.txt");
            File.WriteAllText(output, sb.ToString());
            Debug.Log($"[ProfileSummary] Wrote {output}");
            return output;
        }

        /// <summary>Frame times of the capture: the average hides the frames that miss 16.7 ms.</summary>
        private static void AppendFrameSpread(StringBuilder sb, int first, int last)
        {
            var times = new List<float>();
            for (int f = first; f <= last; f++)
                using (var view = ProfilerDriver.GetRawFrameDataView(f, 0))
                    if (view != null && view.valid) times.Add(view.frameTimeMs);
            if (times.Count == 0) return;
            // Render counters: how much the render thread is asked to draw each frame.
            foreach (string counter in new[] { "Batches Count", "SetPass Calls Count", "Draw Calls Count", "Triangles Count", "Vertices Count", "Shadow Casters Count", "Visible Skinned Meshes Count", "GC Allocated In Frame" })
            {
                double sum = 0; int n = 0;
                for (int f = first; f <= last; f++)
                    using (var view = ProfilerDriver.GetRawFrameDataView(f, 0))
                    {
                        if (view == null || !view.valid || _outliers.Contains(f)) continue;
                        int id = view.GetMarkerId(counter);
                        if (id == FrameDataView.invalidMarkerId) continue;
                        sum += view.GetCounterValueAsLong(id); n++;
                    }
                if (n > 0) sb.AppendLine($"  {counter}: {sum / n:F0} per frame");
            }
            times.Sort();
            float P(float q) => times[Mathf.Clamp((int)(times.Count * q), 0, times.Count - 1)];
            sb.AppendLine($"Frame ms: median {P(0.5f):F2}, p90 {P(0.9f):F2}, p99 {P(0.99f):F2}, max {times[times.Count - 1]:F2}; over 16.7 ms: {times.Count(t => t > 16.7f)} of {times.Count}");
        }

        /// <summary>The main thread's PlayerLoop phases (rendering, scripts, physics, animation ...), ms per frame.</summary>
        private static void AppendPhases(StringBuilder sb, int first, int last, int frames)
        {
            var phases = new Dictionary<string, double>();
            var children = new List<int>();
            var inner = new List<int>();
            for (int f = first; f <= last; f++)
            {
                using (var view = ProfilerDriver.GetHierarchyFrameDataView(f, 0, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
                           HierarchyFrameDataView.columnTotalTime, false))
                {
                    if (view == null || !view.valid || _outliers.Contains(f)) continue;
                    children.Clear();
                    view.GetItemChildren(view.GetRootItemID(), children);
                    foreach (int top in children)
                    {
                        string topName = view.GetItemName(top);
                        if (topName != "PlayerLoop")
                        {
                            Add(phases, "(outside PlayerLoop) " + topName, view.GetItemColumnDataAsFloat(top, HierarchyFrameDataView.columnTotalTime));
                            continue;
                        }
                        inner.Clear();
                        view.GetItemChildren(top, inner);
                        foreach (int phase in inner)
                            Add(phases, view.GetItemName(phase), view.GetItemColumnDataAsFloat(phase, HierarchyFrameDataView.columnTotalTime));
                    }
                }
            }
            sb.AppendLine();
            sb.AppendLine("Main thread by phase (ms per frame):");
            foreach (var kv in phases.OrderByDescending(k => k.Value).Take(30))
                sb.AppendLine($"  {kv.Value / frames,8:F3}  {kv.Key}");
        }

        /// <summary>Every thread's busy time per frame and its top markers (render thread, job workers ...).</summary>
        private static void AppendThreads(StringBuilder sb, int first, int last, int frames)
        {
            sb.AppendLine();
            sb.AppendLine("Threads, busy ms per frame (waits and idle left out) and their top markers by self time:");
            var children = new List<int>();
            var stack = new Stack<int>();
            for (int t = 0; t < 128; t++)
            {
                string threadName = null;
                double busy = 0;
                var self = new Dictionary<string, double>();
                bool any = false;
                for (int f = first; f <= last; f++)
                {
                    using (var view = ProfilerDriver.GetHierarchyFrameDataView(f, t, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
                               HierarchyFrameDataView.columnTotalTime, false))
                    {
                        if (view == null || !view.valid || _outliers.Contains(f)) continue;
                        any = true;
                        threadName ??= view.threadGroupName + "/" + view.threadName;
                        stack.Clear();
                        stack.Push(view.GetRootItemID());
                        while (stack.Count > 0)
                        {
                            int id = stack.Pop();
                            children.Clear();
                            view.GetItemChildren(id, children);
                            foreach (int child in children)
                            {
                                string name = view.GetItemName(child);
                                float s = view.GetItemColumnDataAsFloat(child, HierarchyFrameDataView.columnSelfTime);
                                if (IsWait(name)) continue;
                                busy += s;
                                Add(self, name, s);
                                stack.Push(child);
                            }
                        }
                    }
                }
                if (!any) { if (t > 40) break; continue; }
                if (busy / frames < 0.05) continue;
                sb.AppendLine($"  {busy / frames,8:F3}  [{t}] {threadName}");
                foreach (var kv in self.OrderByDescending(k => k.Value).Take(6))
                    sb.AppendLine($"            {kv.Value / frames,8:F3}  {kv.Key}");
            }
        }

        /// <summary>Every marker's total time in one frame (for a left-out frame's own list).</summary>
        private static Dictionary<string, double> TopTotals(HierarchyFrameDataView view)
        {
            var totals = new Dictionary<string, double>();
            var children = new List<int>();
            var stack = new Stack<int>();
            stack.Push(view.GetRootItemID());
            while (stack.Count > 0)
            {
                int id = stack.Pop();
                children.Clear();
                view.GetItemChildren(id, children);
                foreach (int child in children)
                {
                    string name = view.GetItemName(child);
                    if (!totals.ContainsKey(name)) totals[name] = view.GetItemColumnDataAsFloat(child, HierarchyFrameDataView.columnTotalTime);
                    stack.Push(child);
                }
            }
            return totals;
        }

        private static bool IsWait(string name) =>
            name.Contains("Idle") || name.Contains("WaitFor") || name.Contains("Semaphore") || name.Contains("Sleep");

        private static void Add(Dictionary<string, double> map, string key, double value)
        {
            map.TryGetValue(key, out double v);
            map[key] = v + value;
        }
    }
}
#endif
