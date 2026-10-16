using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// Hang hunt (2026-10-16): AutoplayBot rewrote its whole events.jsonl and timeline.jsonl on the main thread at every
    /// event, and the 1.5 s frames of a Level 5 run came in the frames it wrote the elite-killed events, with no script
    /// section of 5 ms or more. The disk (D:, 98% full) can stall a write, and the bot must not stall the game it measures.
    /// Reports go through one worker thread here: the newest content of each file wins, and <see cref="Flush"/> waits for
    /// them. <see cref="SlowestWriteMs"/> keeps the longest write, so a stalled disk shows in the timeline.
    /// </summary>
    public static class ReportWriter
    {
        private static readonly object Lock = new object();
        private static readonly Dictionary<string, string> Pending = new Dictionary<string, string>();
        private static readonly List<string> Order = new List<string>();
        private static Thread _worker;
        private static int _busy;

        /// <summary>The longest single write since the last <see cref="TakeSlowestWriteMs"/>.</summary>
        public static double SlowestWriteMs { get; private set; }

        public static double TakeSlowestWriteMs()
        {
            lock (Lock) { double ms = SlowestWriteMs; SlowestWriteMs = 0; return ms; }
        }

        /// <summary>Writes <paramref name="content"/> to <paramref name="path"/> soon; a newer content for the same file
        /// replaces one not yet written.</summary>
        public static void Write(string path, string content)
        {
            lock (Lock)
            {
                if (!Pending.ContainsKey(path)) Order.Add(path);
                Pending[path] = content;
                if (_worker == null)
                {
                    _worker = new Thread(Run) { IsBackground = true, Name = "AutoplayBot reports" };
                    _worker.Start();
                }
                Monitor.PulseAll(Lock);
            }
        }

        /// <summary>Waits until every queued file is written (the run's end).</summary>
        public static void Flush(int timeoutMs = 10000)
        {
            var clock = Stopwatch.StartNew();
            while (clock.ElapsedMilliseconds < timeoutMs)
            {
                lock (Lock) if (Pending.Count == 0 && _busy == 0) return;
                Thread.Sleep(10);
            }
        }

        private static void Run()
        {
            while (true)
            {
                string path, content;
                lock (Lock)
                {
                    while (Order.Count == 0) Monitor.Wait(Lock);
                    path = Order[0];
                    Order.RemoveAt(0);
                    content = Pending[path];
                    Pending.Remove(path);
                    _busy = 1;
                }
                var clock = Stopwatch.StartNew();
                try { File.WriteAllText(path, content); } catch (System.Exception) { }
                double ms = clock.Elapsed.TotalMilliseconds;
                lock (Lock)
                {
                    if (ms > SlowestWriteMs) SlowestWriteMs = ms;
                    _busy = 0;
                }
            }
        }
    }
}
