using UnityEngine;
using UnityEditor;

namespace SoulHunter.EditorScripts.Validator
{
    /// <summary>
    /// LIVE BACKGROUND WATCHDOG
    /// Ye script automatically Unity ke background mein chalti rehti hai.
    /// Jab bhi aap Hierarchy mein koi change karte hain ya Play button dabate hain, ye check karti hai.
    /// </summary>
    [InitializeOnLoad]
    public class LiveBackgroundValidator
    {
        static LiveBackgroundValidator()
        {
            // Jab bhi Hierarchy mein kuch change ho, ye function chale
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            
            // Jab Play button dabaya jaye, tab bhi check kare
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnHierarchyChanged()
        {
            // Agar game chal rahi hai tou bar bar check kar ke slow na kare
            if (Application.isPlaying) return;

            CheckCriticalDuplicatesSilently();
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            // Full validation uses reflection and scans the whole project. Running it
            // automatically while entering Play Mode can make the Game View appear
            // frozen on slower machines. Keep validation available through the
            // explicit validator command, but never block gameplay startup with it.
        }

        private static void CheckCriticalDuplicatesSilently()
        {
            CheckAndWarn<SoulHunter.Gameplay.Player.PlayerController>();
            CheckAndWarn<SoulHunter.Gameplay.AI.EnemySpawner>();
            CheckAndWarn<SoulHunter.Gameplay.Combat.WeaponManager>();
            CheckInvalidHierarchySilently();
        }

        private static void CheckInvalidHierarchySilently()
        {
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj.transform.parent != null)
                {
                    Component[] myComps = obj.GetComponents<Component>();
                    foreach (var myComp in myComps)
                    {
                        if (myComp == null || myComp.GetType().Namespace == null || !myComp.GetType().Namespace.StartsWith("SoulHunter")) continue;
                        
                        if (obj.transform.parent.GetComponent(myComp.GetType()) != null)
                        {
                            Debug.LogError($"[LIVE WATCHDOG] 🔴 HIERARCHY ERROR: Nested Entity! '{obj.name}' is a child of '{obj.transform.parent.name}' and BOTH share '{myComp.GetType().Name}'. Ek baap bete ki tarah same component nahi rakh sakte. Isey foran theek karein!");
                        }
                    }
                }
            }
        }

        private static void CheckAndWarn<T>() where T : Component
        {
            T[] instances = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (instances.Length > 1)
            {
                Debug.LogError($"[LIVE WATCHDOG] 🔴 RUK JAYEN! Scene mein {instances.Length} '{typeof(T).Name}' paida ho gaye hain. Asoolan sirf 1 hona chahiye. Extra ko foran delete karein!");
            }
        }
    }
}
