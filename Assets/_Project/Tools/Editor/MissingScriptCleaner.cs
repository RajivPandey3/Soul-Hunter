using UnityEngine;
using UnityEditor;

namespace SoulHunter.EditorScripts
{
    public class MissingScriptCleaner : Editor
    {
        public static void CleanupMissingScripts()
        {
            string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab");
            int totalRemoved = 0;

            foreach (string guid in prefabPaths)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                if (prefab != null)
                {
                    int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(prefab);
                    
                    // Also check children
                    foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
                    {
                        count += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
                    }

                    if (count > 0)
                    {
                        totalRemoved += count;
                        Debug.Log($"<color=yellow>Removed {count} missing scripts from: {prefab.name}</color>");
                        EditorUtility.SetDirty(prefab);
                        PrefabUtility.SavePrefabAsset(prefab);
                    }
                }
            }
            
            AssetDatabase.SaveAssets();
            
            if (totalRemoved > 0)
                Debug.Log($"<color=green><b>SUCCESS!</b> Cleaned up {totalRemoved} missing scripts.</color>");
            else
                Debug.Log("<color=green>No missing scripts found! Everything is clean.</color>");
        }
    }
}
