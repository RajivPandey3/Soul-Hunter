using UnityEngine;
using UnityEditor;

namespace SoulHunter.EditorScripts.Validator
{
    /// <summary>
    /// LAYER 3: Performance Validation
    /// Checks Physics configurations and pooling suggestions for high-frequency objects.
    /// </summary>
    public class ProjectValidatorLayer3 : EditorWindow
    {
        public static void RunPerformanceValidation()
        {
            Debug.Log("<color=cyan><b>--- SOUL-HUNTER VALIDATOR: LAYER 3 STARTED ---</b></color>");
            int warningCount = 0;
            int infoCount = 0;

            // 1. Check Enemy Physics Configuration
            string[] enemyGuids = AssetDatabase.FindAssets("Enemy t:Prefab");
            foreach (var guid in enemyGuids)
            {
                GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (enemyPrefab != null)
                {
                    Rigidbody2D rb = enemyPrefab.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        if (rb.bodyType == RigidbodyType2D.Dynamic)
                        {
                            LogWarning($"[Physics Performance] Enemy Prefab '{enemyPrefab.name}' uses Dynamic Rigidbody2D. If movement design allows, consider Kinematic to save CPU.");
                            warningCount++;
                        }
                    }
                }
            }

            // 2. Check Texture Settings (Basic)
            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D");
            foreach (var guid in textureGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    if (importer.maxTextureSize > 2048)
                    {
                        LogInfo($"[Memory] Texture '{path}' is very large ({importer.maxTextureSize}). Ensure this is necessary.");
                        infoCount++;
                    }
                }
            }

            Debug.Log($"<color=cyan><b>--- LAYER 3 COMPLETE ---</b></color>\n<color=yellow>Warnings: {warningCount}</color> | <color=lightblue>Info: {infoCount}</color>");
        }

        private static void LogWarning(string msg) => Debug.LogWarning($"🟡 WARNING: {msg}");
        private static void LogInfo(string msg) => Debug.Log($"🔵 INFO: {msg}");
    }
}
