using UnityEngine;
using UnityEditor;

namespace SoulHunter.EditorScripts.Setup
{
    public class UISetupBuilder : EditorWindow
    {
        public static void ExecutePhase4()
        {
            // 1. Fetch HUD Canvas without duplicating
            GameObject hud = GameObject.Find("HUD_Canvas") ?? GameObject.Find("GameHUD");
            if (hud == null)
            {
                string prefabPath = "Assets/_Project/Prefabs/HUD_Canvas.prefab";
                GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (hudPrefab != null)
                {
                    hud = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab);
                    hud.name = "HUD_Canvas";
                }
            }
            if (hud == null) return;

            hud.tag = "UI";
            hud.layer = LayerMask.NameToLayer("UI");

            // 2. Ensure UIManager script exists (either on root or child)
            var hudController = hud.GetComponentInChildren<SoulHunter.Gameplay.UI.GameHUDController>();
            if (hudController == null)
            {
                hudController = hud.AddComponent<SoulHunter.Gameplay.UI.GameHUDController>();
            }

            // CLEANUP RULE ENFORCED: No deletion allowed! 
            // Agar component do (2) dafa laga hua hai, tou Watchdog error dega aur
            // developer khud manually usay remove karega. Hum code se DestroyImmediate nahi chalayenge.

            // 3. Panel State Management (Single Responsibility)
            // Ensure Gameplay Panel is ON, and popup panels (LevelUp, Pause, GameOver) are OFF by default.
            Transform gameplayPanel = hud.transform.Find("Gameplay_Panel");
            if (gameplayPanel != null) gameplayPanel.gameObject.SetActive(true);

            Transform levelUpPanel = hud.transform.Find("LevelUp_Panel");
            if (levelUpPanel != null) levelUpPanel.gameObject.SetActive(false);

            Transform pausePanel = hud.transform.Find("Pause_Panel");
            if (pausePanel != null) pausePanel.gameObject.SetActive(false);

            Transform gameOverPanel = hud.transform.Find("GameOver_Panel");
            if (gameOverPanel != null) gameOverPanel.gameObject.SetActive(false);

            if (PrefabUtility.IsPartOfPrefabInstance(hud))
            {
                PrefabUtility.ApplyPrefabInstance(hud, InteractionMode.AutomatedAction);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("<color=green>Phase 4 Complete: UI Panels managed and set to proper Active/Inactive states.</color>");
        }
    }
}
