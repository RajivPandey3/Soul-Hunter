#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Removes the old fixed showcase ground (owner, 2026-09-25): the SH10_Lxx_Ground instances in the ten
    /// showcase scenes, their models, prefabs and materials, and the unused Ground.prefab. The stage floors
    /// (StageFloorImporter) replace them. CLI:
    /// -executeMethod SoulHunter.Editor.Content.LegacyGroundRemover.RemoveAll -quit
    /// </summary>
    public static class LegacyGroundRemover
    {
        public static string GroundName(int stage) => $"SH10_L{stage:00}_Ground";

        [MenuItem("Soul Hunter/Content/Remove Legacy Showcase Ground")]
        public static void RemoveAll()
        {
            int instances = 0, assets = 0;
            for (int stage = 1; stage <= StageFloorImporter.Stages; stage++)
            {
                string scenePath = $"Assets/Scenes/Levels/SH10_L{stage:00}_Showcase.unity";
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                bool changed = false;
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (!root.name.StartsWith(GroundName(stage))) continue;
                    Object.DestroyImmediate(root);
                    instances++;
                    changed = true;
                }
                if (changed) EditorSceneManager.SaveScene(scene);

                string ground = GroundName(stage);
                if (AssetDatabase.DeleteAsset($"{StageFloorImporter.PrefabFolder}/{ground}.prefab")) assets++;
                if (AssetDatabase.DeleteAsset($"{StageFloorImporter.ModelFolder}/{ground}.fbx")) assets++;
                foreach (string guid in AssetDatabase.FindAssets(ground + "_ t:Material", new[] { StageFloorImporter.MaterialFolder }))
                    if (AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid))) assets++;
            }
            if (AssetDatabase.DeleteAsset($"{StageFloorImporter.PrefabFolder}/Ground.prefab")) assets++;
            AssetDatabase.SaveAssets();
            Debug.Log($"[LegacyGroundRemover] Removed {instances} showcase ground instances and {assets} assets.");
        }
    }
}
#endif
