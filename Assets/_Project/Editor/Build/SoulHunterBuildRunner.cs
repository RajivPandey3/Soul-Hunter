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
                // SOULHUNTER_BUILD_DEVELOPMENT=1 makes a development build (profiler captures; slower than a release build).
                options = Environment.GetEnvironmentVariable("SOULHUNTER_BUILD_DEVELOPMENT") == "1" ? BuildOptions.Development : BuildOptions.None
            };

            ApplyAppIcon();
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

        /// <summary>
        /// Android test build (2026-10-10, T-01 device test): an arm64 IL2CPP APK at SOULHUNTER_BUILD_OUTPUT_APK, signed
        /// with the debug key. The project has no Android application id yet (Standalone's "com.DefaultCompany.2D-URP" is
        /// not valid on Android), so a provisional id is used for this build only; the release id is the owner's choice.
        /// </summary>
        public const string ProvisionalAndroidId = "com.NimritaGames.SoulHunter";

        public const string AppIconPath = "Assets/_Project/Art/AppIcon/SH10_AppIcon.png";

        /// <summary>
        /// Owner (2026-10-12): the installed app showed Unity's icon. Every build uses Soul Hunter's own icon
        /// (ArtSource/Generators/build_app_icon.py), set as the default icon for all platforms.
        /// </summary>
        public static void ApplyAppIcon()
        {
            // A batch build can reach this before a newly added picture is loadable, so it is imported first.
            AssetDatabase.ImportAsset(AppIconPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath)
                       ?? AssetDatabase.LoadAllAssetsAtPath(AppIconPath).OfType<Texture2D>().FirstOrDefault();
            if (icon == null)
            {
                string found = string.Join(", ", AssetDatabase.LoadAllAssetsAtPath(AppIconPath).Select(a => a == null ? "null" : a.GetType().Name));
                Debug.LogWarning($"[SoulHunterBuildRunner] App icon not found at {AppIconPath} (file exists: {File.Exists(AppIconPath)}; assets: {found}); the build keeps the default icon.");
                return;
            }
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        }

        public static void BuildAndroid()
        {
            string outputApk = Environment.GetEnvironmentVariable("SOULHUNTER_BUILD_OUTPUT_APK");
            if (string.IsNullOrWhiteSpace(outputApk))
            {
                Debug.LogError("[SoulHunterBuildRunner] Set SOULHUNTER_BUILD_OUTPUT_APK. Build abort.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }
            string buildDir = Path.GetDirectoryName(outputApk);
            if (!string.IsNullOrEmpty(buildDir) && !Directory.Exists(buildDir)) Directory.CreateDirectory(buildDir);

            string[] enabledScenes = EditorBuildSettings.scenes.Where(s => s.enabled && File.Exists(s.path)).Select(s => s.path).ToArray();
            if (enabledScenes.Length == 0)
            {
                Debug.LogError("[SoulHunterBuildRunner] No enabled scenes. Build abort.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            ApplyAppIcon();
            string id = PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android);
            if (string.IsNullOrWhiteSpace(id) || id.Contains("-") || id == "com.DefaultCompany.2D-URP")
                PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, ProvisionalAndroidId);
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.Android.useCustomKeystore = false; // the debug key

            Debug.Log($"[SoulHunterBuildRunner] Android build ({PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android)}) -> {outputApk}");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = enabledScenes,
                locationPathName = outputApk,
                target = BuildTarget.Android,
                options = Environment.GetEnvironmentVariable("SOULHUNTER_BUILD_DEVELOPMENT") == "1" ? BuildOptions.Development : BuildOptions.None
            });
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[SoulHunterBuildRunner] ANDROID BUILD SUCCEEDED in {report.summary.totalTime.TotalSeconds:F0}s, {report.summary.totalSize / (1024 * 1024)} MB");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[SoulHunterBuildRunner] ANDROID BUILD FAILED: {report.summary.result}, errors {report.summary.totalErrors}");
                if (Application.isBatchMode) EditorApplication.Exit(2);
            }
        }
    }
}
#endif
