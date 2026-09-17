using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;

namespace SoulHunter.EditorScripts.Setup
{
    public class HierarchyOrganizer : EditorWindow
    {
        public static void OrganizeHierarchy()
        {
            // 1. Create Separators (Folders) if they don't exist
            GameObject managersFolder = GetOrCreateSeparator("--- MANAGERS ---");
            GameObject environmentFolder = GetOrCreateSeparator("--- ENVIRONMENT ---");
            GameObject uiFolder = GetOrCreateSeparator("--- UI ---");
            GameObject camerasFolder = GetOrCreateSeparator("--- CAMERAS ---");
            GameObject entitiesFolder = GetOrCreateSeparator("--- ENTITIES ---");

            // 2. Find all root objects
            UnityEngine.SceneManagement.Scene activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            GameObject[] rootObjects = activeScene.GetRootGameObjects();

            // 3. Move objects to their respective folders WITHOUT DELETING anything
            foreach (GameObject obj in rootObjects)
            {
                // Skip the folders themselves
                if (obj.name.StartsWith("--- ")) continue;

                // --- MANAGERS ---
                if (obj.name.Contains("Manager") || obj.name.Contains("Spawner") || obj.name.Contains("System"))
                {
                    // Exceptions: EventSystem goes to UI, ParticleSystem goes to Environment (if any)
                    if (obj.GetComponent<EventSystem>() != null)
                    {
                        Undo.SetTransformParent(obj.transform, uiFolder.transform, "Organize hierarchy");
                    }
                    else
                    {
                        Undo.SetTransformParent(obj.transform, managersFolder.transform, "Organize hierarchy");
                    }
                    continue;
                }

                // --- ENVIRONMENT ---
                if (obj.name.Contains("Map") || obj.name.Contains("Ground") || obj.name.Contains("Environment") || obj.name.Contains("Light"))
                {
                    Undo.SetTransformParent(obj.transform, environmentFolder.transform, "Organize hierarchy");
                    continue;
                }

                // --- UI ---
                if (obj.name.Contains("Canvas") || obj.name.Contains("HUD") || obj.name.Contains("UI") || obj.GetComponent<EventSystem>() != null)
                {
                    Undo.SetTransformParent(obj.transform, uiFolder.transform, "Organize hierarchy");
                    continue;
                }

                // --- CAMERAS ---
                if (obj.name.Contains("Camera") || obj.name.Contains("Cinemachine") || obj.GetComponent<Camera>() != null)
                {
                    Undo.SetTransformParent(obj.transform, camerasFolder.transform, "Organize hierarchy");
                    continue;
                }

                // --- ENTITIES ---
                if (obj.name.Contains("Player") || obj.name.Contains("Enemy") || obj.name.Contains("Boss"))
                {
                    // Player Entity is usually kept at root, but we can put it in Entities for neatness
                    Undo.SetTransformParent(obj.transform, entitiesFolder.transform, "Organize hierarchy");
                    continue;
                }
            }

            // Move folders to top of hierarchy for a clean look
            managersFolder.transform.SetAsFirstSibling();
            environmentFolder.transform.SetSiblingIndex(1);
            uiFolder.transform.SetSiblingIndex(2);
            camerasFolder.transform.SetSiblingIndex(3);
            entitiesFolder.transform.SetSiblingIndex(4);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("<color=green>Hierarchy Organized Successfully! (Safe Mode: Kuch bhi delete nahi kiya gaya)</color>");
        }

        private static GameObject GetOrCreateSeparator(string name)
        {
            GameObject separator = GameObject.Find(name);
            if (separator == null)
            {
                separator = new GameObject(name);
                // Learning Comment: Organizational parents must survive builds with their children.
                Undo.RegisterCreatedObjectUndo(separator, "Create hierarchy group");
            }
            if (separator.CompareTag("EditorOnly"))
            {
                Undo.RecordObject(separator, "Keep runtime hierarchy in builds");
                separator.tag = "Untagged";
            }
            return separator;
        }
    }
}
