using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;

namespace SoulHunter.EditorScripts.Setup
{
    public class ManagerSetupLinker : EditorWindow
    {
        public static void ExecutePhase3()
        {
            // 1. Create Parent Container (For organization only)
            GameObject managersContainer = GameObject.Find("--- MANAGERS ---");
            if (managersContainer == null)
            {
                managersContainer = new GameObject("--- MANAGERS ---");
            }
            
            // RULE ENFORCED: No deletion allowed! 
            // Agar "GameManagers" naam ka purana object mojood hai, tou usay wese hi chhor do.
            // Validator khud developer ko inform karega ke duplicate hai.
            GameObject oldManagers = GameObject.Find("GameManagers");
            if (oldManagers != null)
            {
                Debug.LogWarning("[Rule Enforced] Purana 'GameManagers' object detect hua hai. Isey delete nahi kiya gaya. Kripya check karein.");
            }

            // 2. Find existing managers or create them if they don't exist anywhere
            var spawner = GetOrCreateManager<SoulHunter.Gameplay.AI.EnemySpawner>(managersContainer, "EnemySpawner_Manager");
            var weaponMgr = GetOrCreateManager<SoulHunter.Gameplay.Combat.WeaponManager>(managersContainer, "Weapon_Manager");
            GetOrCreateManager<SoulHunter.Gameplay.Core.GameSystemManager>(managersContainer, "GameSystem_Manager");
            GetOrCreateManager<SoulHunter.Gameplay.Core.LevelProgressionManager>(managersContainer, "LevelProgression_Manager");
            GetOrCreateManager<SoulHunter.Gameplay.Core.LevelUpManager>(managersContainer, "LevelUp_Manager");
            GetOrCreateManager<SoulHunter.Gameplay.Core.ChestLogicController>(managersContainer, "ChestLogic_Manager");

            // 3. Link Enemy Spawner Waves
            if (spawner != null)
            {
                string[] waveGuids = AssetDatabase.FindAssets("t:WaveData");
                List<SoulHunter.Gameplay.Data.WaveData> waves = new List<SoulHunter.Gameplay.Data.WaveData>();
                foreach (var guid in waveGuids)
                {
                    waves.Add(AssetDatabase.LoadAssetAtPath<SoulHunter.Gameplay.Data.WaveData>(AssetDatabase.GUIDToAssetPath(guid)));
                }

                FieldInfo wavesField = typeof(SoulHunter.Gameplay.AI.EnemySpawner).GetField("_waves", BindingFlags.NonPublic | BindingFlags.Instance);
                if (wavesField != null) wavesField.SetValue(spawner, waves);
            }

            // 4. Link Weapon Manager
            if (weaponMgr != null)
            {
                SetWeapon(weaponMgr, "_magicWandObject", "Wand_Weapon");
                SetWeapon(weaponMgr, "_garlicWeaponObject", "GarlicWeapon");
                SetWeapon(weaponMgr, "_whipWeaponObject", "Whip_Weapon");
                SetWeapon(weaponMgr, "_axeWeaponObject", "Axe_Weapon");
                SetWeapon(weaponMgr, "_bibleWeaponObject", "Bible_Weapon");
                SetWeapon(weaponMgr, "_crossWeaponObject", "Cross_Weapon");
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("<color=green>Phase 3 Complete: Modular Managers safely wired using Read First, Match Later rule!</color>");
        }

        private static T GetOrCreateManager<T>(GameObject defaultParent, string defaultName) where T : Component
        {
            // Read First: Check if the component already exists ANYWHERE in the scene
            T existingComp = Object.FindFirstObjectByType<T>();
            if (existingComp != null)
            {
                return existingComp;
            }

            // If it doesn't exist, create it under the default parent
            GameObject obj = new GameObject(defaultName);
            obj.transform.SetParent(defaultParent.transform);
            return obj.AddComponent<T>();
        }

        private static void SetWeapon(object target, string fieldName, string prefabName)
        {
            FieldInfo field = typeof(SoulHunter.Gameplay.Combat.WeaponManager).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                string[] guids = AssetDatabase.FindAssets(prefabName + " t:Prefab");
                if (guids.Length > 0)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
                    field.SetValue(target, prefab);
                }
            }
        }
    }
}
