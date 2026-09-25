#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Wires the Blender stage floors (ArtSource/Generators/build_grounds.py) into the game:
    /// imports each SH10_Lxx_Floor.fbx with faceted materials from the generator's manifest,
    /// builds Assets/Prefabs/Environment/SH10_Lxx_Floor.prefab (tile + one flat BoxCollider) and
    /// fills GameContentCatalog.StageFloors, which InfiniteMap tiles for the current stage.
    /// Menu: Soul Hunter/Content/Import Stage Floors, or CLI:
    /// -executeMethod SoulHunter.Editor.Content.StageFloorImporter.ImportAll -quit
    /// </summary>
    public static class StageFloorImporter
    {
        public const int Stages = 10;
        public const float TileSize = 20f;
        public const string ModelFolder = "Assets/Art/Models";
        public const string MaterialFolder = "Assets/Art/Materials";
        public const string PrefabFolder = "Assets/Prefabs/Environment";
        public const string CatalogPath = "Assets/Resources/GameContent.asset";
        private const string ShaderName = "SoulHunter/Review/FacetedPreview";

        public static string FloorName(int stage) => $"SH10_L{stage:00}_Floor";

        [MenuItem("Soul Hunter/Content/Import Stage Floors")]
        public static void ImportAll()
        {
            var colors = LoadManifest();
            var floors = new GameObject[Stages];
            for (int stage = 1; stage <= Stages; stage++)
            {
                ConfigureModel(FloorName(stage), colors);
                floors[stage - 1] = BuildPrefab(FloorName(stage));
            }
            var catalog = AssetDatabase.LoadAssetAtPath<GameContentCatalog>(CatalogPath);
            catalog.StageFloors = floors;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[StageFloorImporter] Imported {Stages} stage floors into the content catalog.");
        }

        private static Dictionary<string, Color> LoadManifest()
        {
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath), "ArtSource", "SH10_Floors_v01", "manifest.json");
            if (!File.Exists(path)) throw new FileNotFoundException("Run build_grounds.py first.", path);
            var colors = new Dictionary<string, Color>();
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(path),
                         "\"(SH10_L\\d\\d_Floor_[A-Za-z]+)\":\\s*\\[\\s*([0-9.eE+-]+),\\s*([0-9.eE+-]+),\\s*([0-9.eE+-]+)\\s*\\]"))
                colors[m.Groups[1].Value] = new Color(float.Parse(m.Groups[2].Value, inv), float.Parse(m.Groups[3].Value, inv), float.Parse(m.Groups[4].Value, inv), 1f);
            return colors;
        }

        private static void ConfigureModel(string floor, Dictionary<string, Color> colors)
        {
            string path = $"{ModelFolder}/{floor}.fbx";
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new FileNotFoundException("Missing floor model", path);
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            var shader = Shader.Find(ShaderName);
            foreach (var entry in colors.Where(c => c.Key.StartsWith(floor + "_")))
            {
                string matPath = $"{MaterialFolder}/{entry.Key}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null)
                {
                    mat = new Material(shader) { name = entry.Key };
                    AssetDatabase.CreateAsset(mat, matPath);
                }
                mat.shader = shader;
                mat.SetColor("_Color", entry.Value);
                EditorUtility.SetDirty(mat);
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), entry.Key), mat);
            }
            importer.SaveAndReimport();
        }

        private static GameObject BuildPrefab(string floor)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}/{floor}.fbx");
            var root = new GameObject(floor);
            try
            {
                var tile = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                tile.transform.SetParent(root.transform, false);
                // One flat collider for the whole tile: decor is visual only and never blocks movement.
                var collider = root.AddComponent<BoxCollider>();
                collider.center = new Vector3(0f, -0.1f, 0f);
                collider.size = new Vector3(TileSize, 0.2f, TileSize);
                foreach (var renderer in root.GetComponentsInChildren<Renderer>())
                {
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
                return PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/{floor}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
#endif
