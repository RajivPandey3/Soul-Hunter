using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;

namespace SoulHunter.EditorScripts.Validator
{
    /// <summary>
    /// SOUL-HUNTER PROJECT VALIDATOR (Layer 1: Editor Validation)
    /// Enforces CTO-level architectural rules for Scene and Prefabs.
    /// </summary>
    public class ProjectValidatorLayer1 : EditorWindow
    {
        public static void RunEditorValidation()
        {
            Debug.Log("<color=cyan><b>--- SOUL-HUNTER VALIDATOR: LAYER 1 STARTED ---</b></color>");
            int errorCount = 0;
            int warningCount = 0;

            GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            // 1. Missing Scripts Check (The Silent Killer)
            foreach (var obj in allObjects)
            {
                Component[] components = obj.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        LogError($"Missing MonoBehaviour detected on GameObject '{obj.name}'! Fix or remove it.");
                        errorCount++;
                    }
                }
            }

            // 2. Duplicate Singleton/Service/Entity Check
            errorCount += CheckDuplicate<SoulHunter.Gameplay.Player.PlayerController>();
            errorCount += CheckDuplicate<SoulHunter.Gameplay.AI.EnemySpawner>();
            errorCount += CheckDuplicate<SoulHunter.Gameplay.Combat.WeaponManager>();
            errorCount += CheckDuplicate<SoulHunter.Gameplay.Core.GameSystemManager>();
            errorCount += CheckDuplicate<SoulHunter.Gameplay.Core.LevelProgressionManager>();
            errorCount += CheckDuplicate<SoulHunter.Gameplay.Environment.InfiniteMap>();

            // 2.5 INVALID HIERARCHY CHECK (Nested Entities)
            errorCount += CheckInvalidHierarchy(allObjects);

            // 3. Data Integrity & Broken References Watchdog
            foreach (var obj in allObjects)
            {
                Component[] components = obj.GetComponents<Component>();
                foreach (var comp in components)
                {
                    if (comp == null) continue;
                    
                    // Only check fields inside custom SoulHunter scripts to avoid Unity/TMPro false-positives
                    if (comp.GetType().Namespace == null || !comp.GetType().Namespace.StartsWith("SoulHunter")) continue;
                    
                    FieldInfo[] fields = comp.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    foreach (var field in fields)
                    {
                        bool isSerialized = field.GetCustomAttribute<SerializeField>() != null || (field.IsPublic && !field.IsInitOnly);
                        if (!isSerialized) continue;

                        // Check Unity Object References
                        if (typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                        {
                            var val = field.GetValue(comp);
                            if (val == null || val.Equals(null))
                            {
                                // We make it a warning because some references might be optional. 
                                LogWarning($"[Null Reference] '{comp.GetType().Name}.{field.Name}' is NULL on '{obj.name}'.");
                                warningCount++;
                            }
                        }
                        // Check Lists and Arrays
                        else if (typeof(System.Collections.IList).IsAssignableFrom(field.FieldType))
                        {
                            var list = field.GetValue(comp) as System.Collections.IList;
                            if (list == null || list.Count == 0)
                            {
                                LogWarning($"[Empty List/Array] '{comp.GetType().Name}.{field.Name}' is empty on '{obj.name}'.");
                                warningCount++;
                            }
                        }
                    }
                }
            }

            // 4. UI & Front-End Constraints (Canvas Optimization)
            var canvases = GameObject.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                var raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                if (raycaster != null)
                {
                    LogInfo($"Canvas '{canvas.name}' has a GraphicRaycaster. Ensure it actually needs UI input. If it's just for display, disable it for performance.");
                }
            }

            Debug.Log($"<color=cyan><b>--- VALIDATION COMPLETE ---</b></color>\n<color=red>Errors: {errorCount}</color> | <color=yellow>Warnings: {warningCount}</color>");
        }

        private static int CheckInvalidHierarchy(GameObject[] allObjects)
        {
            int errCount = 0;
            foreach (var obj in allObjects)
            {
                if (obj.transform.parent != null)
                {
                    // Check if object and its parent share the same critical component (Nested Entity Bug)
                    Component[] myComps = obj.GetComponents<Component>();
                    foreach (var myComp in myComps)
                    {
                        if (myComp == null) continue;
                        
                        // Only check custom scripts
                        if (myComp.GetType().Namespace == null || !myComp.GetType().Namespace.StartsWith("SoulHunter")) continue;

                        // Does parent have the exact same component type?
                        if (obj.transform.parent.GetComponent(myComp.GetType()) != null)
                        {
                            Debug.LogError($"🔴 ERROR [Invalid Hierarchy]: Nested Entity Detected! GameObject '{obj.name}' is a child of '{obj.transform.parent.name}' and BOTH have '{myComp.GetType().Name}' attached. This creates double behavior.");
                            errCount++;
                        }
                    }
                }
            }
            return errCount;
        }

        private static int CheckDuplicate<T>() where T : Component
        {
            T[] instances = GameObject.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (instances.Length > 1)
            {
                LogError($"Duplicate Manager/Service detected! Found {instances.Length} instances of {typeof(T).Name}. There should only be one.");
                return 1;
            }
            return 0;
        }

        private static void LogError(string msg) => Debug.LogError($"🔴 ERROR: {msg}");
        private static void LogWarning(string msg) => Debug.LogWarning($"🟡 WARNING: {msg}");
        private static void LogInfo(string msg) => Debug.Log($"🔵 INFO: {msg}");
    }
}
