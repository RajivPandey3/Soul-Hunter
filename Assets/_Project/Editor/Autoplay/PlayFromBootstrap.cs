#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SoulHunter.Editor.Autoplay
{
    /// <summary>
    /// Owner report (2026-09-30): "game play button click karte hi direct level 1 start ho raha hai, boot kyun nahi ho
    /// raha". Unity's Play button starts whichever scene is open; with Main_Gameplay open it skipped Bootstrap, Loading and
    /// the main menu. While "Soul Hunter/Play From Bootstrap" is on (the default), pressing Play on any game scene (one in
    /// the build settings) starts from Bootstrap, as the built game does. Test runs (their own scene) and batch mode are
    /// left alone; switch the menu item off to play one scene directly.
    ///   CLI check: Unity.exe -projectPath &lt;copy&gt; -executeMethod SoulHunter.Editor.Autoplay.PlayFromBootstrap.CheckFromCommandLine
    ///              -sh-bootcheck &lt;result.txt&gt;   (opens Main_Gameplay, presses Play, writes the scenes that loaded, quits)
    /// </summary>
    [InitializeOnLoad]
    public static class PlayFromBootstrap
    {
        public const string BootstrapScene = "Assets/Scenes/Bootstrap/Bootstrap.unity";
        private const string PrefKey = "SH_PlayFromBootstrap";
        private const string MenuPath = "Soul Hunter/Play From Bootstrap";
        private const string CheckKey = "SH_BootCheck_File";

        static PlayFromBootstrap()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorSceneManager.activeSceneChangedInEditMode += (_, __) => Refresh();
            EditorApplication.delayCall += Refresh;
            if (!string.IsNullOrEmpty(SessionState.GetString(CheckKey, ""))) EditorApplication.update += WatchCheck;
        }

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set { EditorPrefs.SetBool(PrefKey, value); Refresh(); }
        }

        [MenuItem(MenuPath)]
        private static void Toggle() => Enabled = !Enabled;

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        /// <summary>Bootstrap when the open scene is one of the game's own scenes (other than Bootstrap); null otherwise.</summary>
        public static SceneAsset StartSceneFor(string activeScenePath, bool enabled, bool batchMode)
        {
            if (!enabled || batchMode || string.IsNullOrEmpty(activeScenePath) || activeScenePath == BootstrapScene) return null;
            bool gameScene = EditorBuildSettings.scenes.Any(s => s.enabled && s.path == activeScenePath);
            return gameScene ? AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScene) : null;
        }

        private static void Refresh()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorSceneManager.playModeStartScene = StartSceneFor(SceneManager.GetActiveScene().path, Enabled, Application.isBatchMode);
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode)
                EditorSceneManager.playModeStartScene = StartSceneFor(SceneManager.GetActiveScene().path, Enabled, Application.isBatchMode);
        }

        // ---------- command-line check ----------

        public static void CheckFromCommandLine()
        {
            var args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-sh-bootcheck");
            string file = i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.GetFullPath("bootcheck.txt");
            SessionState.SetString(CheckKey, file);
            SessionState.SetFloat(CheckKey + "_start", 0f);
            EditorSceneManager.OpenScene("Assets/Scenes/Gameplay/Main_Gameplay.unity");
            Refresh();
            File.WriteAllText(file, $"opened {SceneManager.GetActiveScene().path}; start scene {(EditorSceneManager.playModeStartScene != null ? AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) : "none")}\n");
            EditorApplication.update += WatchCheck;
            EditorApplication.EnterPlaymode();
        }

        private static void WatchCheck()
        {
            string file = SessionState.GetString(CheckKey, "");
            if (string.IsNullOrEmpty(file) || !EditorApplication.isPlaying) return;
            float start = SessionState.GetFloat(CheckKey + "_start", 0f);
            if (start <= 0f) { SessionState.SetFloat(CheckKey + "_start", Time.realtimeSinceStartup); return; }
            // Record every scene seen, then stop after 12 s of play.
            string seen = string.Join(",", Enumerable.Range(0, SceneManager.sceneCount).Select(k => SceneManager.GetSceneAt(k).name));
            string last = SessionState.GetString(CheckKey + "_last", "");
            if (seen != last)
            {
                SessionState.SetString(CheckKey + "_last", seen);
                File.AppendAllText(file, $"{Time.realtimeSinceStartup - start:0.0}s: {seen}\n");
            }
            if (Time.realtimeSinceStartup - start < 12f) return;
            EditorApplication.update -= WatchCheck;
            SessionState.EraseString(CheckKey);
            File.AppendAllText(file, "done\n");
            EditorApplication.Exit(0);
        }
    }
}
#endif
