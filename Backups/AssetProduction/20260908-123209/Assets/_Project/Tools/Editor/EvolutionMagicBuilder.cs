using UnityEngine;
using UnityEditor;
using System.IO;
using SoulHunter.Gameplay.Combat;

namespace SoulHunter.EditorSetup
{
    public class EvolutionMagicBuilder
    {
        public static void RunMagic()
        {
            Debug.Log("== STARTING EVOLUTION MAGIC ==");

            string prefabsPath = "Assets/_Project/Prefabs/Weapon";
            string dataPath = "Assets/Resources/Data/Evolutions";

            if (!Directory.Exists(dataPath))
            {
                Debug.LogError($"Data folder nahi mila: {dataPath}. Kya aapne folder theek banaya tha?");
                return;
            }

            // 1. Dhoondo ke base prefabs kahan hain
            var basePrefabs = new System.Collections.Generic.Dictionary<UpgradeData.UpgradeType, string>
            {
                { UpgradeData.UpgradeType.MagicWand, "Wand_Weapon.prefab" },
                { UpgradeData.UpgradeType.Whip, "Whip_Weapon.prefab" },
                { UpgradeData.UpgradeType.Garlic, "Garlic_Weapon.prefab" },
                { UpgradeData.UpgradeType.Axe, "Axe_Weapon.prefab" },
                { UpgradeData.UpgradeType.Bible, "Bible_Weapon.prefab" },
                { UpgradeData.UpgradeType.Cross, "Cross_Weapon.prefab" }
            };

            // 2. Load all Evolution Data
            string[] evoGuids = AssetDatabase.FindAssets("t:WeaponEvolutionData", new[] { dataPath });
            foreach (string guid in evoGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                WeaponEvolutionData evoData = AssetDatabase.LoadAssetAtPath<WeaponEvolutionData>(path);

                if (evoData != null && evoData.EvolvedWeaponPrefab == null)
                {
                    if (basePrefabs.TryGetValue(evoData.BaseWeapon, out string basePrefabName))
                    {
                        string sourcePath = $"{prefabsPath}/{basePrefabName}";
                        string newPrefabPath = $"{prefabsPath}/{evoData.EvolvedName.Replace(" ", "")}_Weapon.prefab";

                        if (!File.Exists(newPrefabPath))
                        {
                            AssetDatabase.CopyAsset(sourcePath, newPrefabPath);
                            Debug.Log($"[Magic] Naya Prefab Banaya: {newPrefabPath}");
                        }

                        GameObject evolvedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(newPrefabPath);
                        if (evolvedPrefab != null)
                        {
                            evoData.EvolvedWeaponPrefab = evolvedPrefab;
                            EditorUtility.SetDirty(evoData);
                            Debug.Log($"[Magic] {evoData.name} mein Evolved Prefab assign kar diya!");
                        }
                    }
                }
            }

            // 3. Load all Union Data
            string[] unionGuids = AssetDatabase.FindAssets("t:WeaponUnionData", new[] { dataPath });
            foreach (string guid in unionGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                WeaponUnionData unionData = AssetDatabase.LoadAssetAtPath<WeaponUnionData>(path);

                if (unionData != null && unionData.UnionWeaponPrefab == null)
                {
                    // Fallback to Weapon A base prefab as the template
                    if (basePrefabs.TryGetValue(unionData.WeaponA, out string basePrefabName) || 
                        basePrefabs.TryGetValue(UpgradeData.UpgradeType.MagicWand, out basePrefabName)) // default
                    {
                        string sourcePath = $"{prefabsPath}/{basePrefabName}";
                        string newPrefabPath = $"{prefabsPath}/{unionData.UnionName.Replace(" ", "")}_Weapon.prefab";

                        if (!File.Exists(newPrefabPath))
                        {
                            AssetDatabase.CopyAsset(sourcePath, newPrefabPath);
                            Debug.Log($"[Magic] Naya Union Prefab Banaya: {newPrefabPath}");
                        }

                        GameObject unionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(newPrefabPath);
                        if (unionPrefab != null)
                        {
                            unionData.UnionWeaponPrefab = unionPrefab;
                            EditorUtility.SetDirty(unionData);
                            Debug.Log($"[Magic] {unionData.name} mein Union Prefab assign kar diya!");
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("== MAGIC COMPLETE! Jaa kar check karein! ==");
        }
    }
}
