using UnityEngine;
using UnityEditor;

namespace SoulHunter.EditorScripts.Setup
{
    /// <summary>
    /// Learning Comment:
    /// Phase 1: Core Foundation. Ye script sirf aur sirf Tags, Layers aur Physics Matrix setup karegi.
    /// Ye ProjectSettings file ko modify karti hai taake project mein zaroori tags aur layers mojud hon.
    /// </summary>
    public class TagsAndLayersBuilder : EditorWindow
    {
        public static void ExecutePhase1()
        {
            // 1. Add Tags
            AddTag("Player");
            AddTag("Enemy");
            AddTag("Pickup");
            AddTag("Weapon");

            // 2. Add Layers
            AddLayer(6, "Player");
            AddLayer(7, "Enemy");
            AddLayer(8, "Projectile");
            AddLayer(9, "Pickup");
            AddLayer(10, "Ground");

            // 3. Setup Physics 3D Matrix (Ignore collisions) - Hybrid Architecture Rule!
            // Example: Player (6) should ignore Projectile (8)
            Physics.IgnoreLayerCollision(6, 8, true);
            // Player (6) ignores Ground (10) since ground is background
            Physics.IgnoreLayerCollision(6, 10, true);
            // Enemies ignore each other's projectils (if any)
            Physics.IgnoreLayerCollision(7, 8, true);
            // Projectiles ignore each other
            Physics.IgnoreLayerCollision(8, 8, true);

            // Also configure 2D just in case UI or pickups use it
            Physics2D.IgnoreLayerCollision(6, 8, true);
            Physics2D.IgnoreLayerCollision(6, 10, true);
            Physics2D.IgnoreLayerCollision(7, 8, true);
            Physics2D.IgnoreLayerCollision(8, 8, true);

            Debug.Log("<color=green><b>[PHASE 1 COMPLETE]</b> Tags, Layers, aur Physics 3D Collision Matrix (Hybrid System) kamyabi se setup ho gaye hain!</color>");
        }

        private static void AddTag(string tag)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            
            bool found = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue.Equals(tag))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                tagsProp.InsertArrayElementAtIndex(0);
                tagsProp.GetArrayElementAtIndex(0).stringValue = tag;
                tagManager.ApplyModifiedProperties();
            }
        }

        private static void AddLayer(int index, string layerName)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layersProp = tagManager.FindProperty("layers");
            
            SerializedProperty sp = layersProp.GetArrayElementAtIndex(index);
            if (sp != null)
            {
                sp.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
            }
        }
    }
}
