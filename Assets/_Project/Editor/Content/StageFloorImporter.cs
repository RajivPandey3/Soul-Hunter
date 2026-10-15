#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Wires the Blender stage floors (ArtSource/Generators/build_grounds.py) into the game:
    /// imports each SH10_Lxx_Floor.fbx with textured URP Lit materials from the generator's manifest
    /// (palette colour x stylized detail texture from build_ground_textures.py),
    /// builds Assets/Prefabs/Environment/SH10_Lxx_Floor.prefab (tile + one flat BoxCollider; the
    /// hidden _Blockers mesh becomes the obstacle MeshCollider on the Obstacle layer) and
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
        public const string ObstacleLayerName = "Obstacle";
        private const string SourceFolder = "SH10_Floors_v02";

        public static string FloorName(int stage) => $"SH10_L{stage:00}_Floor";

        [MenuItem("Soul Hunter/Content/Import Stage Floors")]
        public static void ImportAll()
        {
            var colors = LoadManifest();
            if (LayerMask.NameToLayer(ObstacleLayerName) < 0) throw new System.InvalidOperationException("Add the Obstacle layer first.");
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
            FloorMeshMerger.MergeAll(); // one mesh and one material per tile renderer (draw calls, 2026-10-15)
            Debug.Log($"[StageFloorImporter] Imported {Stages} stage floors into the content catalog.");
        }

        /// <summary>One floor material from the generator manifest: palette colour, detail texture pattern, glow.</summary>
        public sealed class FloorMaterial
        {
            public Color Color;
            public string Texture;   // build_ground_textures.py pattern, or null for a plain colour
            public bool Emissive;
        }

        public const string TextureFolder = "Assets/Art/Textures/Ground";
        /// <summary>Mean brightness of the generated detail textures (build_ground_textures.py MEAN).</summary>
        public const float TextureMean = 0.8f;
        private const string LitShaderName = "Universal Render Pipeline/Lit";

        public static Dictionary<string, FloorMaterial> LoadManifest()
        {
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath), "ArtSource", SourceFolder, "manifest.json");
            if (!File.Exists(path)) throw new FileNotFoundException("Run build_grounds.py first.", path);
            string json = File.ReadAllText(path);
            var result = new Dictionary<string, FloorMaterial>();
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            const string name = @"""(SH10_L\d\d_Floor_\w+)""";
            foreach (Match m in Regex.Matches(json, name + @":\s*\[\s*([0-9.eE+-]+),\s*([0-9.eE+-]+),\s*([0-9.eE+-]+)\s*\]"))
                result[m.Groups[1].Value] = new FloorMaterial
                {
                    Color = new Color(float.Parse(m.Groups[2].Value, inv), float.Parse(m.Groups[3].Value, inv), float.Parse(m.Groups[4].Value, inv), 1f)
                };
            foreach (Match m in Regex.Matches(json, name + @":\s*""([a-z]+)"""))
                if (result.TryGetValue(m.Groups[1].Value, out var material)) material.Texture = m.Groups[2].Value;
            foreach (Match block in Regex.Matches(json, @"""emissive"":\s*\[([^\]]*)\]"))
                foreach (Match m in Regex.Matches(block.Groups[1].Value, @"""([^""]+)"""))
                    if (result.TryGetValue(m.Groups[1].Value, out var material)) material.Emissive = true;
            return result;
        }

        private static Texture2D DetailTexture(string pattern)
        {
            string path = $"{TextureFolder}/SH10_Tex_{pattern}.png";
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                bool changed = importer.wrapMode != TextureWrapMode.Repeat || !importer.mipmapEnabled ||
                               importer.maxTextureSize != 512 || importer.anisoLevel != 4 || !importer.sRGBTexture;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.mipmapEnabled = true;
                importer.sRGBTexture = true;
                importer.maxTextureSize = 512;
                importer.anisoLevel = 4;
                if (changed) importer.SaveAndReimport();
            }
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) throw new FileNotFoundException("Run build_ground_textures.py first.", path);
            return texture;
        }

        /// <summary>
        /// URP Lit: the detail texture tinted by the palette colour (divided by the texture's mean so the average colour
        /// stays the palette colour), matte, lit by the stage light and receiving/casting its shadows.
        /// </summary>
        /// <summary>
        /// Eye comfort (owner, 2026-10-02: "ground ka colour ekdam se dark comfortable nahi lagta ... aankho ko chubhe
        /// nahi", a stage lasts 30 minutes). The palettes ran from near-black (Levels 9-10 at 0.09 brightness) to blinding
        /// white snow (Level 6 at 1.0), with strong glows. Every floor colour's brightness is pressed into a calm mid band
        /// (ComfortMin..ComfortMax), keeping each level's order of light and dark, and strong colours are softened.
        /// </summary>
        public const float ComfortMin = 0.30f, ComfortMax = 0.68f, ComfortMaxSaturation = 0.55f, GlowStrength = 1.0f;

        public static Color ComfortGrade(Color c)
        {
            Color.RGBToHSV(c, out float h, out float sat, out float v);
            v = Mathf.Lerp(ComfortMin, ComfortMax, Mathf.Clamp01(v));
            sat = Mathf.Min(sat, ComfortMaxSaturation) * 0.9f;
            var graded = Color.HSVToRGB(h, sat, v);
            graded.a = 1f;
            return graded;
        }

        public static void ApplyLook(Material mat, FloorMaterial look)
        {
            var texture = look.Texture != null ? DetailTexture(look.Texture) : null;
            var color = ComfortGrade(look.Color);
            var tint = texture != null ? color / TextureMean : color;
            tint = new Color(Mathf.Min(1f, tint.r), Mathf.Min(1f, tint.g), Mathf.Min(1f, tint.b), 1f);
            mat.SetTexture("_BaseMap", texture);
            mat.SetTexture("_MainTex", texture);
            mat.SetColor("_BaseColor", tint);
            mat.SetColor("_Color", tint);
            mat.SetFloat("_Smoothness", 0.08f);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_EnvironmentReflections", 0f);
            mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            if (look.Emissive)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color.linear * GlowStrength); // was 2.5x the raw colour: harsh over a long stage
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
            }
        }

        private static void ConfigureModel(string floor, Dictionary<string, FloorMaterial> colors)
        {
            string path = $"{ModelFolder}/{floor}.fbx";
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new FileNotFoundException("Missing floor model", path);
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            var shader = Shader.Find(LitShaderName);
            if (shader == null) throw new System.InvalidOperationException("URP Lit shader not found.");
            // Materials of an earlier floor version that the current manifest no longer uses.
            foreach (string guid in AssetDatabase.FindAssets(floor + "_ t:Material", new[] { MaterialFolder }))
            {
                string stale = AssetDatabase.GUIDToAssetPath(guid);
                if (!colors.ContainsKey(Path.GetFileNameWithoutExtension(stale))) AssetDatabase.DeleteAsset(stale);
            }
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
                ApplyLook(mat, entry.Value);
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
                // One flat collider for the whole walkable tile.
                var collider = root.AddComponent<BoxCollider>();
                collider.center = new Vector3(0f, -0.1f, 0f);
                collider.size = new Vector3(TileSize, 0.2f, TileSize);
                // Obstacles block the player through the simple, hidden blocker boxes only. Enemies are
                // triggers and projectiles ignore the Obstacle layer, so only the player is blocked.
                int obstacleLayer = LayerMask.NameToLayer(ObstacleLayerName);
                foreach (var child in tile.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == floor + "_Obstacles") child.gameObject.layer = obstacleLayer;
                    if (child.name != floor + "_Blockers") continue;
                    child.gameObject.layer = obstacleLayer;
                    child.GetComponent<MeshRenderer>().enabled = false;
                    child.gameObject.AddComponent<MeshCollider>().sharedMesh = child.GetComponent<MeshFilter>().sharedMesh;
                }
                // The tilted camera shows depth, so the stage light's shadows ground the scene: obstacles cast
                // them, the floor only receives them (its flat detail would add cost for no visible shadow).
                foreach (var renderer in root.GetComponentsInChildren<Renderer>())
                {
                    bool obstacle = renderer.name == floor + "_Obstacles";
                    renderer.shadowCastingMode = obstacle ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = true;
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
