#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SoulHunter.Editor.Autoplay
{
    /// <summary>
    /// Plays Soul Hunter in the Unity Editor with AutoplayBot (owner request 2026-09-25: "play it yourself in the
    /// editor"). Opens Bootstrap and enters Play Mode; the bot plays a real run in the Game view, records screenshots
    /// (with the UI), a timeline, events and a summary, then leaves Play Mode.
    ///   Menu: Soul Hunter/Autoplay/Play 3 Minutes In Editor (report in &lt;project&gt;/../autoplay-editor-*)
    ///   CLI:  Unity.exe -projectPath &lt;copy&gt; -executeMethod SoulHunter.Editor.Autoplay.AutoplayEditorLauncher.RunFromCommandLine
    ///         -sh-autoplay &lt;folder&gt; [-sh-autoplay-minutes N] [-sh-autoplay-character Name] -sh-autoplay-exit
    /// The bot never changes the real save (throw-away save or SaveService.UseFile).
    /// </summary>
    public static class AutoplayEditorLauncher
    {
        public const string SessionKey = "SH_Autoplay_Request";
        private const string BootstrapScene = "Assets/Scenes/Bootstrap/Bootstrap.unity";

        [MenuItem("Soul Hunter/Autoplay/Play 3 Minutes In Editor")]
        public static void PlayFromMenu() => Play(3f, "Kael");

        [MenuItem("Soul Hunter/Autoplay/Play 10 Minutes In Editor")]
        public static void PlayLongFromMenu() => Play(10f, "Kael");

        private static void Play(float minutes, string character)
        {
            if (EditorApplication.isPlaying) return;
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..",
                "autoplay-editor-" + System.DateTime.Now.ToString("yyyyMMdd-HHmmss")));
            // Picked up by AutoplayBot when Play Mode starts (it survives the domain reload).
            SessionState.SetString(SessionKey, $"{output}|{minutes.ToString(System.Globalization.CultureInfo.InvariantCulture)}|{character}");
            Enter();
            Debug.Log($"[Autoplay] Playing {minutes} min as {character}; report in {output}");
        }

        /// <summary>Command line: the -sh-autoplay arguments on the editor's own command line configure the bot.</summary>
        public static void RunFromCommandLine() => Enter();

        private static void Enter()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(BootstrapScene);
            EditorApplication.EnterPlaymode();
        }
    }
}
#endif
