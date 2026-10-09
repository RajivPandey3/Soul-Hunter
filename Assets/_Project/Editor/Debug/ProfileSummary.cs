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
            Summarize(args[i + 1]);
        }

        [MenuItem("Soul Hunter/Debug/Summarize Profile")]
        private static void FromMenu()
        {
            string path = EditorUtility.OpenFilePanel("Profiler capture", "", "raw");
            if (!string.IsNullOrEmpty(path)) Summarize(path);
        }

        private sealed class Totals { public double Self, Total, Calls; }

        public static string Summarize(string rawPath)
        {
            if (!ProfilerDriver.LoadProfile(rawPath, false)) { Debug.LogError("[ProfileSummary] Could not load " + rawPath); return null; }
            int first = ProfilerDriver.firstFrameIndex, last = ProfilerDriver.lastFrameIndex;
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
                    frames++;
                    frameMs += view.frameTimeMs;
                    var seen = new HashSet<string>();
                    var frameTotals = view.frameTimeMs >= 100f ? new Dictionary<string, double>() : null;
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
            sb.AppendLine($"Frames: {frames}, average frame {(frames > 0 ? frameMs / frames : 0):F2} ms");
            sb.AppendLine();
            sb.AppendLine("Top 45 by self time (ms per frame | calls per frame):");
            foreach (var kv in byName.OrderByDescending(k => k.Value.Self).Take(45))
                sb.AppendLine($"  {kv.Value.Self / frames,8:F3}  {kv.Value.Calls / frames,8:F1}  {kv.Key}");
            sb.AppendLine();
            sb.AppendLine("Top 60 by total time (ms per frame):");
            foreach (var kv in byName.OrderByDescending(k => k.Value.Total).Take(60))
                sb.AppendLine($"  {kv.Value.Total / frames,8:F3}  {kv.Key}");
            sb.AppendLine();
            sb.AppendLine($"Slowest frames (100 ms or more: {slow.Count}); top markers by total time in each:");
            foreach (var s in slow.OrderByDescending(x => x.ms).Take(8))
            {
                sb.AppendLine($"  frame {s.frame}: {s.ms:F1} ms");
                foreach (var kv in s.totals.OrderByDescending(k => k.Value).Take(25))
                    sb.AppendLine($"      {kv.Value,9:F2}  {kv.Key}");
            }
            string output = Path.ChangeExtension(rawPath, ".summary.txt");
            File.WriteAllText(output, sb.ToString());
            Debug.Log($"[ProfileSummary] Wrote {output}");
            return output;
        }
    }
}
#endif
