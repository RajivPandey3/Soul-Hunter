#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SoulHunter.Editor.Build
{
    /// <summary>
    /// Learning Comment:
    /// Single Responsibility: StandaloneWindows64 production player build trigger karna.
    /// Yeh script Unity Editor menu ("Soul Hunter/Build Standalone Windows") se bhi
    /// aur Unity CLI (-executeMethod SoulHunter.Editor.Build.SoulHunterBuildRunner.BuildStandaloneWindows)
    /// se bhi execute ki ja sakti hai.
    /// Build output hamesha "D:\Unity Builds\SoulHunter\SoulHunter.exe" par save hoti hai.
    /// </summary>
    public static class SoulHunterBuildRunner
    {
        private const string DefaultBuildFolder = @"D:\Unity Builds\SoulHunter";
        private const string ExeName = "SoulHunter.exe";

        [MenuItem("Soul Hunter/Build/Build Standalone Windows (D:\\Unity Builds)")]
        public static void BuildStandaloneWindows()
        {
            string outputExePath = Environment.GetEnvironmentVariable("SOULHUNTER_BUILD_OUTPUT_EXE");
            if (string.IsNullOrWhiteSpace(outputExePath))
            {
                outputExePath = Path.Combine(DefaultBuildFolder, ExeName);
            }

            string buildDir = Path.GetDirectoryName(outputExePath);
            if (!string.IsNullOrEmpty(buildDir) && !Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }

            // Read enabled scenes from EditorBuildSettings
            string[] enabledScenes = EditorBuildSettings.scenes
                .Where(s => s.enabled && File.Exists(s.path))
                .Select(s => s.path)
                .ToArray();

            if (enabledScenes.Length == 0)
            {
                Debug.LogError("[SoulHunterBuildRunner] Koi enabled scene nahi mila EditorBuildSettings mein! Build abort.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"[SoulHunterBuildRunner] Starting build for {enabledScenes.Length} scenes -> {outputExePath}");

            var buildOptions = new BuildPlayerOptions
            {
                scenes = enabledScenes,
                locationPathName = outputExePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[SoulHunterBuildRunner] BUILD SUCCEEDED! Total time: {summary.totalTime.TotalSeconds:F1}s, Size: {summary.totalSize / (1024 * 1024)} MB");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[SoulHunterBuildRunner] BUILD FAILED! Result: {summary.result}, Errors: {summary.totalErrors}");
                if (Application.isBatchMode) EditorApplication.Exit(2);
            }
        }
    }
}
#endif
