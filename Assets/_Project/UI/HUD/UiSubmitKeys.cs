using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace SoulHunter.Gameplay.UI
{
    /// <summary>
    /// VS wiki (Options, patch 0.2.4): "Spacebar can be used just like Enter to interact with the UI". Each scene's UI input
    /// module (InputSystemUIInputModule with its DefaultInputActions) submits on Enter only, so Space did nothing on the
    /// level-up cards or the chest screen (owner report 2026-10-18). Space is added to every module's Submit action when a
    /// scene loads; the scenes themselves are not edited. Dash, which Space also is, does not run while a menu pauses the game
    /// (PlayerController).
    /// </summary>
    public static class UiSubmitKeys
    {
        public const string SpacePath = "<Keyboard>/space";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplyToAll();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplyToAll();

        /// <summary>Adds Space to the Submit action of every UI input module in the loaded scenes (once per action).</summary>
        public static void ApplyToAll()
        {
            foreach (var module in Object.FindObjectsByType<InputSystemUIInputModule>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                AddSpace(module.submit != null ? module.submit.action : null);
        }

        /// <summary>True when the action already answers Space.</summary>
        public static bool HasSpace(InputAction action)
        {
            if (action == null) return false;
            foreach (var binding in action.bindings)
                if (binding.path == SpacePath || binding.effectivePath == SpacePath) return true;
            return false;
        }

        private static void AddSpace(InputAction action)
        {
            if (action == null || HasSpace(action)) return;
            bool wasEnabled = action.enabled;
            if (wasEnabled) action.Disable(); // bindings cannot be added to an enabled action
            action.AddBinding(SpacePath);
            if (wasEnabled) action.Enable();
        }
    }
}
