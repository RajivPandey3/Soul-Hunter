using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using UnityEngine.LowLevel;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// Measuring tool (AutoplayBot -sh-autoplay-loop-timing): the main thread's time per engine step of a frame (physics,
    /// scripts' Update, animation, rendering ...) in a release build, without the profiler. A marker runs before every
    /// step of Unity's player loop and charges the time since the last marker to the step that was running. A long frame
    /// whose script sections were all short can then be put down to one engine step (2026-10-29: Level 4's crab fight
    /// had 25-48 ms frames spent on the main thread with no timed script section of 1 ms).
    /// </summary>
    public static class LoopTimer
    {
        private static readonly List<string> Names = new List<string>();
        private static double[] _ms = new double[0], _lastFrame = new double[0];
        private static long _last;
        private static int _current = -1;
        private static bool _installed;

        public static void Install()
        {
            if (_installed) return;
            _installed = true;
            var loop = PlayerLoop.GetCurrentPlayerLoop();
            var phases = loop.subSystemList;
            for (int p = 0; p < phases.Length; p++)
            {
                var phase = phases[p];
                var steps = phase.subSystemList ?? new PlayerLoopSystem[0];
                var wrapped = new List<PlayerLoopSystem>(steps.Length * 2 + 1);
                string phaseName = phase.type != null ? phase.type.Name : "Phase" + p;
                for (int s = 0; s < steps.Length; s++)
                {
                    wrapped.Add(Marker(Add(phaseName + "." + (steps[s].type != null ? steps[s].type.Name : "Step" + s)), p == 0 && s == 0));
                    wrapped.Add(steps[s]);
                }
                wrapped.Add(Marker(Add(phaseName + ".(after)"), false));
                phase.subSystemList = wrapped.ToArray();
                phases[p] = phase;
            }
            loop.subSystemList = phases;
            _ms = new double[Names.Count];
            _lastFrame = new double[Names.Count];
            PlayerLoop.SetPlayerLoop(loop);
        }

        private static int Add(string name) { Names.Add(name); return Names.Count - 1; }

        private static PlayerLoopSystem Marker(int index, bool frameStart) => new PlayerLoopSystem
        {
            type = typeof(LoopTimer),
            updateDelegate = () => Mark(index, frameStart),
        };

        private static void Mark(int index, bool frameStart)
        {
            long now = Stopwatch.GetTimestamp();
            if (_current >= 0) _ms[_current] += (now - _last) * 1000.0 / Stopwatch.Frequency;
            if (frameStart)
            {
                // The first marker of a frame: the frame before is complete (its wait for the next frame included).
                var done = _lastFrame;
                _lastFrame = _ms;
                _ms = done;
                System.Array.Clear(_ms, 0, _ms.Length);
            }
            _current = index;
            _last = now;
        }

        /// <summary>The last complete frame's slowest steps of at least <paramref name="fromMs"/>, e.g. "FixedUpdate.PhysicsFixedUpdate 18.2 ms".</summary>
        public static string DescribeLastFrame(double fromMs = 1.0, int top = 6)
        {
            if (!_installed) return "";
            var order = new List<int>();
            for (int i = 0; i < _lastFrame.Length; i++) if (_lastFrame[i] >= fromMs) order.Add(i);
            order.Sort((a, b) => _lastFrame[b].CompareTo(_lastFrame[a]));
            var text = new StringBuilder();
            for (int k = 0; k < order.Count && k < top; k++)
            {
                if (k > 0) text.Append("; ");
                text.Append(Names[order[k]]).Append(' ').Append(_lastFrame[order[k]].ToString("0.0", CultureInfo.InvariantCulture)).Append(" ms");
            }
            return text.ToString();
        }
    }
}
