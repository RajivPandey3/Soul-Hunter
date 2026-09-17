using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace SoulHunter.EditorScripts.Setup
{
    public class EnvironmentSetupBuilder : EditorWindow
    {
        public static void ExecutePhase5()
        {
            // 1. Create Parent Container (For organization only)
            GameObject envContainer = GameObject.Find("--- ENVIRONMENT ---");
            if (envContainer == null)
            {
                envContainer = new GameObject("--- ENVIRONMENT ---");
            }

            // 2. Create Single Responsibility Environment Manager
            var mapManager = GetOrCreateManager<SoulHunter.Gameplay.Environment.InfiniteMap>(envContainer, "InfiniteMap_Manager");

            // 3. Link Chunk Prefab (Ground.prefab) using SerializedObject (Fragile-proof)
            string[] guids = AssetDatabase.FindAssets("Ground t:Prefab");
            GameObject groundPrefab = null;
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileName(path) == "Ground.prefab")
                {
                    groundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    break; // Exact match found
                }
            }

            if (groundPrefab != null)
            {
                SerializedObject so = new SerializedObject(mapManager);
                SerializedProperty chunkProp = so.FindProperty("_chunkPrefab");
                if (chunkProp != null)
                {
                    chunkProp.objectReferenceValue = groundPrefab;
                    so.ApplyModifiedProperties();
                }
                else
                {
                    Debug.LogWarning("[PHASE 5] '_chunkPrefab' property not found. Maybe it was renamed?");
                }
            }
            else
            {
                Debug.LogWarning("[PHASE 5] Exact 'Ground.prefab' project mein nahi mila. Zameen ka prefab manually set karna parega.");
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("<color=green>Phase 5 Complete: Environment safely wired (Inactive Checked & SerializedObject Used)!</color>");
        }

        private static T GetOrCreateManager<T>(GameObject defaultParent, string defaultName) where T : Component
        {
            // Read First: Check if the component already exists ANYWHERE in the scene (including Inactive)
            T[] existingComps = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (existingComps != null && existingComps.Length > 0)
            {
                return existingComps[0];
            }

            // If it doesn't exist, create it under the default parent
            GameObject obj = new GameObject(defaultName);
            obj.transform.SetParent(defaultParent.transform);
            return obj.AddComponent<T>();
        }
    }
}
