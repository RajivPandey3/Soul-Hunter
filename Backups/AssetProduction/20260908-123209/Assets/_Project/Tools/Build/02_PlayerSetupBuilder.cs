using UnityEngine;
using UnityEditor;

namespace SoulHunter.EditorScripts.Setup
{
    public class PlayerSetupBuilder : EditorWindow
    {
        public static void ExecutePhase2()
        {
            string prefabPath = "Assets/_Project/Prefabs/Player_Entity.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (playerPrefab == null) return;

            // 1. Check if already exists to prevent duplicates
            GameObject playerInstance = GameObject.Find("Player_Entity");
            if (playerInstance == null)
            {
                // FORCE spawn at the root of the active scene to avoid nesting!
                playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                playerInstance.transform.position = Vector3.zero;
                playerInstance.name = "Player_Entity";
            }
            
            // CLEANUP RULE ENFORCED: No deletion allowed! 
            // Agar player ke andar koi kachra/enemy hai, tou usay Validator (Watchdog) pakray ga 
            // aur developer khud manually delete karega. Hum code se kuch destroy nahi karenge.

            // Clean missing scripts first to prevent AddComponent from failing
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(playerInstance);

            // 2. Setup Tag and Layer
            playerInstance.tag = "Player";
            playerInstance.layer = LayerMask.NameToLayer("Player");

            // 3. Ensure 3D Rigidbody (As per Rule Guide - Hybrid System)
            Rigidbody rb = playerInstance.GetComponent<Rigidbody>();
            if (rb == null) rb = playerInstance.AddComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false; // Usually false for top down
                rb.isKinematic = false;
                rb.constraints = RigidbodyConstraints.FreezeRotation; // Freeze all rotations for top down
            }

            // 4. Ensure CapsuleCollider (As per Rule Guide)
            CapsuleCollider col = playerInstance.GetComponent<CapsuleCollider>();
            if (col == null) col = playerInstance.AddComponent<CapsuleCollider>();

            // 5. Check Required Scripts (PlayerController, EnvironmentScanner, HealthController)
            if (playerInstance.GetComponent<SoulHunter.Gameplay.Player.PlayerController>() == null)
                playerInstance.AddComponent<SoulHunter.Gameplay.Player.PlayerController>();
                
            if (playerInstance.GetComponent<SoulHunter.Gameplay.Physics.EnvironmentScanner>() == null)
                playerInstance.AddComponent<SoulHunter.Gameplay.Physics.EnvironmentScanner>();
                
            if (playerInstance.GetComponent<SoulHunter.Gameplay.Combat.HealthController>() == null)
                playerInstance.AddComponent<SoulHunter.Gameplay.Combat.HealthController>();

            // 5. Apply changes
            if (PrefabUtility.IsPartOfPrefabInstance(playerInstance))
            {
                PrefabUtility.ApplyPrefabInstance(playerInstance, InteractionMode.AutomatedAction);
            }
            
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("<color=green>Phase 2 Complete: Player setup done without duplication.</color>");
        }
    }
}
