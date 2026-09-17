using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace SoulHunter.EditorScripts.Validator
{
    /// <summary>
    /// LAYER 4: Build Validation (Hard Gate)
    /// Runs before every build. It opens all enabled scenes in the Build Settings, 
    /// scans them for critical errors (Missing scripts, duplicates), and blocks build if any exist.
    /// </summary>
    public class ProjectValidatorLayer4 : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log("<color=magenta><b>--- SOUL-HUNTER VALIDATOR: LAYER 4 (BUILD GATE) ---</b></color>");
            
            int totalErrors = 0;
            
            // 1. Save current active scene path to restore later
            string originalScenePath = EditorSceneManager.GetActiveScene().path;
            if (string.IsNullOrEmpty(originalScenePath))
            {
                // If scene is not saved, we can't safely switch and return
                Debug.LogWarning("Current scene is unsaved. Save the scene before building.");
                throw new BuildFailedException("[VALIDATOR FAILED] Cannot run Build Gate on an unsaved scene. Please save your scene first.");
            }
            
            EditorSceneManager.SaveOpenScenes();

            // 2. Iterate over all enabled scenes in Build Settings
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled) continue;

                Debug.Log($"<color=cyan>Validating Scene: {scene.path}</color>");
                
                // Open the scene
                Scene openedScene = EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
                
                // Run Validation on this scene
                totalErrors += RunBuildValidation(openedScene);
            }
            
            // 3. Restore original scene
            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);

            if (totalErrors > 0)
            {
                throw new BuildFailedException($"[VALIDATOR FAILED] Found {totalErrors} critical error(s) across Build Scenes. Build blocked. Please check Console for exact scenes and errors.");
            }
            
            Debug.Log("<color=green>Build Validation Passed! All Build Scenes are clean.</color>");
        }

        private int RunBuildValidation(Scene scene)
        {
            int errorCount = 0;
            GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            // 1. Missing Scripts Check
            foreach (var obj in allObjects)
            {
                Component[] components = obj.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        Debug.LogError($"🔴 ERROR in Scene '{scene.name}': Missing MonoBehaviour detected on GameObject '{obj.name}'!");
                        errorCount++;
                    }
                }
            }

            // 2. Duplicate Singleton/Service Check (Now restricted to per-scene to support additive scenes)
            errorCount += CheckDuplicate<SoulHunter.Gameplay.Player.PlayerController>(scene.name);
            errorCount += CheckDuplicate<SoulHunter.Gameplay.AI.EnemySpawner>(scene.name);
            errorCount += CheckDuplicate<SoulHunter.Gameplay.Combat.WeaponManager>(scene.name);
            errorCount += CheckDuplicate<SoulHunter.Gameplay.Core.GameSystemManager>(scene.name);

            return errorCount;
        }

        private int CheckDuplicate<T>(string sceneName) where T : Component
        {
            T[] instances = GameObject.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (instances.Length > 1)
            {
                Debug.LogError($"🔴 ERROR in Scene '{sceneName}': Duplicate Manager detected! Found {instances.Length} instances of {typeof(T).Name}.");
                return 1;
            }
            return 0;
        }
    }
}
