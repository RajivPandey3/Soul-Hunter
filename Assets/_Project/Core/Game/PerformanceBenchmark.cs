using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using SoulHunter.Core.Events;
using SoulHunter.Core.Scenes;
using SoulHunter.Core.Services;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.Gameplay.Core
{
    /// <summary>
    /// Learning Comment: Performance smoke test for player builds (AGENTS.md section 6). Only runs when the game is
    /// launched with <c>-sh-benchmark &lt;results.json&gt;</c>; a normal launch never creates it.
    /// It switches to Medium quality with V-Sync off, enters a run as Kael (invincible, walking in changing
    /// directions, taking the first level-up choice and closing chests), measures frame times in three segments
    /// (Stage 1 opening, Stage 1 at 20:30 with the largest enemy minimum, Stage 10 at 20:30), writes the results
    /// and quits. SaveService uses a throw-away save while benchmarking, so the player's save is never touched.
    /// Example: SoulHunter.exe -sh-benchmark D:\bench.json -screen-width 1920 -screen-height 1080 -screen-fullscreen 0
    /// </summary>
    public sealed class PerformanceBenchmark : MonoBehaviour
    {
        public const string Argument = "-sh-benchmark";
        private const float WarmupSeconds = 8f;
        private const float MeasureSeconds = 30f;
        private const float LateStageTime = 20.5f * 60f;

        private string _outputPath;
        private readonly StringBuilder _json = new StringBuilder();
        private readonly List<string> _errors = new List<string>();
        private readonly List<string> _stalls = new List<string>();
        private List<UpgradeData> _lastChoices;
        private const float StallSeconds = 15f; // a 5-item chest legitimately takes 6-7 s to reveal

        /// <summary>The results path given after -sh-benchmark, or null when not benchmarking.</summary>
        public static string RequestedOutput()
        {
            var args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, Argument);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfRequested()
        {
            string output = RequestedOutput();
            if (string.IsNullOrEmpty(output)) return;
            var go = new GameObject("PerformanceBenchmark");
            DontDestroyOnLoad(go);
            go.AddComponent<PerformanceBenchmark>()._outputPath = output;
        }

        private void OnEnable() => Application.logMessageReceived += OnLog;
        private void OnDisable() => Application.logMessageReceived -= OnLog;

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if ((type == LogType.Error || type == LogType.Exception) && _errors.Count < 20) _errors.Add(condition);
        }

        /// <summary>
        /// Options: -sh-benchmark-level N starts the hero at level N (a strong hero kills fast, which loads kill effects such
        /// as the blood spray); -sh-benchmark-no-blood turns Disable Blood on, for A/B checks of the spray's cost;
        /// -sh-benchmark-blood-ab measures each late segment with blood, without, then with it again, in one run.
        /// </summary>
        /// <summary>-sh-benchmark-blood-ab: each late segment is measured with blood, without, then with it again.</summary>
        private static bool BloodAB => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-sh-benchmark-blood-ab") >= 0;
        private const float AbWarmupSeconds = 3f;

        private static void SetBlood(bool on)
        {
            var save = GameServices.Instance.Get<SoulHunter.Core.Persistence.SaveService>();
            if (save != null && save.CurrentData != null) save.CurrentData.DisableBlood = !on;
        }

        private static bool NoBlood => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-sh-benchmark-no-blood") >= 0;

        private static int StartLevel()
        {
            var args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-sh-benchmark-level");
            return i >= 0 && i + 1 < args.Length && int.TryParse(args[i + 1], out int level) ? Mathf.Clamp(level, 1, 400) : 1;
        }

        private IEnumerator Start()
        {
            while (SceneManager.GetActiveScene().name != "MainMenu") yield return null;
            yield return new WaitForSecondsRealtime(1f);
            // The save is a throw-away one while benchmarking (SaveService).
            var save = GameServices.Instance.Get<SoulHunter.Core.Persistence.SaveService>();
            if (save != null && save.CurrentData != null) save.CurrentData.DisableBlood = NoBlood;
            GameServices.Instance.Get<SceneService>().LoadSceneAsync("Main_Gameplay");
            while (SceneManager.GetActiveScene().name != "Main_Gameplay" || PlayerController.Instance == null ||
                   LevelProgressionManager.Instance == null) yield return null;
            yield return new WaitForSecondsRealtime(1f);

            ApplyBenchmarkSettings();
            var player = PlayerController.Instance;
            player.GetComponent<HealthController>().IsInvincible = true;
            var levelUp = FindFirstObjectByType<LevelUpManager>();
            if (levelUp != null) levelUp.OnLevelUpUIRound += choices => { _lastChoices = choices; if (choices.Count > 0) StartCoroutine(Pick(levelUp, choices[0])); };
            StartCoroutine(Walk());
            StartCoroutine(CloseChests());
            StartCoroutine(StallWatchdog(levelUp));
            var experience = player.GetComponent<PlayerExperience>();
            int startLevel = StartLevel();
            while (experience != null && experience.CurrentLevel < startLevel)
                experience.AddXP(experience.XPToNextLevel - experience.CurrentXP);

            _json.Append("{\n");
            _json.Append($"  \"device\": \"{Escape(SystemInfo.processorType)} | {Escape(SystemInfo.graphicsDeviceName)} | {SystemInfo.systemMemorySize} MB\",\n");
            _json.Append($"  \"quality\": \"{QualitySettings.names[QualitySettings.GetQualityLevel()]}\",\n");
            _json.Append($"  \"startLevel\": {startLevel},\n");
            _json.Append($"  \"blood\": \"{(BloodAB ? "A/B" : NoBlood ? "off" : "on")}\",\n");
            _json.Append("  \"segments\": [\n");

            if (BloodAB)
            {
                // The same hero and loadout, so the kill rate stays comparable: blood, no blood, blood again.
                foreach (var (label, stage, last) in new[] { ("Stage 1 at 20:30", (int?)null, false), ("Stage 10 at 20:30", (int?)10, true) })
                {
                    SetBlood(true);
                    yield return Measure(label + " / blood", stage, LateStageTime, true);
                    SetBlood(false);
                    yield return Measure(label + " / no blood", null, 0f, true, AbWarmupSeconds);
                    SetBlood(true);
                    yield return Measure(label + " / blood again", null, 0f, !last, AbWarmupSeconds);
                }
            }
            else
            {
                yield return Measure("Stage 1 opening", null, 0f, true);
                yield return Measure("Stage 1 at 20:30", null, LateStageTime, true);
                yield return Measure("Stage 10 at 20:30", 10, LateStageTime, false);
            }

            _json.Append("  ],\n");
            _json.Append($"  \"stalls\": [{string.Join(", ", _stalls.Select(e => "\"" + Escape(e) + "\""))}],\n");
            _json.Append($"  \"errors\": [{string.Join(", ", _errors.Select(e => "\"" + Escape(e) + "\""))}]\n}}\n");
            System.IO.File.WriteAllText(_outputPath, _json.ToString());
            Debug.Log($"[PerformanceBenchmark] Results written to {_outputPath}");
            Application.Quit();
        }

        private static void ApplyBenchmarkSettings()
        {
            int medium = System.Array.IndexOf(QualitySettings.names, "Medium");
            if (medium >= 0) QualitySettings.SetQualityLevel(medium, true);
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            // The contract baseline is 1920x1080. On a smaller display the 3D view is rendered at a URP render scale
            // that gives 1080p's pixel count (reflection: Gameplay does not reference the URP assembly).
            var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            var renderScale = pipeline != null ? pipeline.GetType().GetProperty("renderScale") : null;
            if (renderScale != null) renderScale.SetValue(pipeline, BaselineRenderScale());
        }

        /// <summary>Render scale that makes the current window render as many pixels as 1920x1080 (1 or more, at most 2).</summary>
        public static float BaselineRenderScale() =>
            Mathf.Clamp(Mathf.Sqrt(1920f * 1080f / Mathf.Max(1f, Screen.width * (float)Screen.height)), 1f, 2f);

        private static string RenderedResolution()
        {
            float scale = BaselineRenderScale();
            return $"{Mathf.RoundToInt(Screen.width * scale)}x{Mathf.RoundToInt(Screen.height * scale)}";
        }

        private IEnumerator Measure(string label, int? stage, float stageTime, bool more, float warmupSeconds = WarmupSeconds)
        {
            var progression = LevelProgressionManager.Instance;
            if (stage.HasValue) progression.StartStage(stage.Value);
            yield return null;
            if (stageTime > 0f)
                typeof(LevelProgressionManager).GetProperty(nameof(LevelProgressionManager.StageElapsedTime))
                    .GetSetMethod(true).Invoke(progression, new object[] { stageTime });
            ApplyBenchmarkSettings(); // stage changes and options may have reapplied V-Sync

            yield return WaitRealtime(warmupSeconds);
            var frames = new List<float>();
            long enemies = 0;
            int killsBefore = RunStatsTracker.Instance != null ? RunStatsTracker.Instance.TotalKills : 0;
            float end = Time.realtimeSinceStartup + MeasureSeconds;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
                frames.Add(Time.unscaledDeltaTime * 1000f);
                enemies += EnemyController.ActiveEnemies.Count;
            }

            int kills = (RunStatsTracker.Instance != null ? RunStatsTracker.Instance.TotalKills : 0) - killsBefore;
            frames.Sort();
            float avg = frames.Average();
            float p99 = frames[Mathf.Min(frames.Count - 1, (int)(frames.Count * 0.99f))];
            var inv = CultureInfo.InvariantCulture;
            _json.Append(string.Format(inv,
                "    {{\"segment\": \"{0}\", \"window\": \"{1}x{2}\", \"rendered3D\": \"{15}\", \"frames\": {3}, \"avgFps\": {4:F1}, \"avgMs\": {5:F2}, " +
                "\"p99Ms\": {6:F2}, \"onePercentLowFps\": {7:F1}, \"maxMs\": {8:F2}, \"framesOver33ms\": {9}, \"avgEnemies\": {10:F0}, " +
                "\"kills\": {16}, \"monoMB\": {11}, \"allocatedMB\": {12}, \"timeScale\": {13}}}{14}\n",
                label, Screen.width, Screen.height, frames.Count, 1000f / avg, avg, p99, 1000f / p99, frames[frames.Count - 1],
                frames.Count(f => f > 33.4f), enemies / (double)Mathf.Max(1, frames.Count),
                UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / (1024 * 1024),
                UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024), Time.timeScale, more ? "," : "", RenderedResolution(), kills));
        }

        /// <summary>Takes the first level-up choice a frame later, after the level-up screen has opened.</summary>
        private static IEnumerator Pick(LevelUpManager levelUp, UpgradeData choice)
        {
            yield return null;
            if (levelUp != null && levelUp.IsLevelUpActive) levelUp.SelectUpgrade(choice);
        }

        private static IEnumerator WaitRealtime(float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end) yield return null;
        }

        /// <summary>Walks like a player: a new direction every 2 s, so new tiles and spawns keep coming.</summary>
        private IEnumerator Walk()
        {
            var bus = GameServices.Instance.Get<EventBus>();
            int step = 0;
            while (true)
            {
                float angle = (step++ * 135f) * Mathf.Deg2Rad;
                bus.Raise(new PlayerMoveEvent(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle))));
                yield return WaitRealtime(2f);
            }
        }

        /// <summary>Chest rewards pause the game until closed; the benchmark closes them like a player would.</summary>
        /// <summary>
        /// A player never leaves the game paused for long. If it stays paused for StallSeconds (found 2026-09-29: a run
        /// sat on an open chest screen for four segments), this records who holds the pause and what is open, then
        /// closes the chest screen or takes a level-up card, so the measurement goes on and the stall shows in the results.
        /// </summary>
        private IEnumerator StallWatchdog(LevelUpManager levelUp)
        {
            float pausedSince = -1f;
            while (true)
            {
                yield return WaitRealtime(0.5f);
                if (Time.timeScale > 0f) { pausedSince = -1f; continue; }
                if (pausedSince < 0f) { pausedSince = Time.realtimeSinceStartup; continue; }
                if (Time.realtimeSinceStartup - pausedSince < StallSeconds) continue;

                var chests = FindObjectsByType<ChestUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                string chestState = string.Join(" ", chests.Select(c => c.Screen != null ? $"{c.name}:{c.Screen.Current}" : $"{c.name}:no screen"));
                string stall = $"{Time.realtimeSinceStartup:F0}s: paused by {SoulHunter.Core.Services.GameTime.PauserNames()}; " +
                               $"level-up active {(levelUp != null && levelUp.IsLevelUpActive)}; chests {chestState}";
                Debug.LogWarning("[PerformanceBenchmark] Stall: " + stall);
                if (_stalls.Count < 20) _stalls.Add(stall);

                foreach (var chest in chests)
                    if (chest != null && chest.IsOpen) yield return chest.OpenAndClose();
                if (levelUp != null && levelUp.IsLevelUpActive && _lastChoices != null && _lastChoices.Count > 0)
                    levelUp.SelectUpgrade(_lastChoices[0]);
                pausedSince = -1f;
            }
        }

        private IEnumerator CloseChests()
        {
            while (true)
            {
                yield return WaitRealtime(0.5f);
                foreach (var chest in FindObjectsByType<ChestUI>(FindObjectsSortMode.None))
                    if (chest != null && chest.IsOpen) yield return chest.OpenAndClose();
            }
        }

        private static string Escape(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ");
    }
}
