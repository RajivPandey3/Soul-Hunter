using UnityEngine;
using UnityEditor;

namespace SoulHunter.EditorSetup
{
    public class GlobalCombatBalancer : EditorWindow
    {
        public static void ShowWindow()
        {
            GetWindow<GlobalCombatBalancer>("Combat Balancer").Show();
        }

        private Vector2 scrollPos;

        private void OnGUI()
        {
            GUILayout.Label("Weapon Prefab Balancer", EditorStyles.boldLabel);
            GUILayout.Label("Warning: This directly modifies Prefabs!", EditorStyles.helpBox);
            GUILayout.Space(10);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // Find all weapon scripts that might be attached to prefabs
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs" });
            
            int weaponsFound = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                if (prefab == null) continue;

                // Check for MagicWandWeapon as a representative. In a real scenario, an interface IWeapon is better.
                var magicWand = prefab.GetComponent<SoulHunter.Gameplay.Weapons.MagicWandWeapon>();
                if (magicWand != null)
                {
                    weaponsFound++;
                    EditorGUILayout.BeginVertical("box");
                    GUILayout.Label(prefab.name, EditorStyles.boldLabel);
                    
                    SerializedObject so = new SerializedObject(magicWand);
                    so.Update();
                    
                    EditorGUILayout.PropertyField(so.FindProperty("_damage"));
                    EditorGUILayout.PropertyField(so.FindProperty("_cooldown"));
                    EditorGUILayout.PropertyField(so.FindProperty("_projectileSpeed"));
                    
                    if (so.ApplyModifiedProperties())
                    {
                        EditorUtility.SetDirty(prefab);
                    }
                    EditorGUILayout.EndVertical();
                    GUILayout.Space(5);
                }
            }

            if (weaponsFound == 0)
            {
                GUILayout.Label("No MagicWandWeapon components found on prefabs in Assets/_Project/Prefabs.");
            }

            EditorGUILayout.EndScrollView();
            
            if (GUILayout.Button("Save All Changes", GUILayout.Height(30)))
            {
                AssetDatabase.SaveAssets();
                Debug.Log("[Combat Balancer] All weapon modifications saved!");
            }
        }
    }
}
