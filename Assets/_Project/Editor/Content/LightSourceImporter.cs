using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Pickups;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Imports the Soul Lantern (ArtSource/Generators/build_light_source.py --export) as the VS light source:
    /// URP Lit materials from the generator's values, and Assets/Prefabs/Props/SH10_SoulLantern.prefab with an
    /// enemy-tagged trigger so every weapon can break it, 10 HP, LightSource and a soft soul glow on the ground.
    /// Fills GameContentCatalog.SoulLantern (spawned by LightSourceSpawner).
    /// Menu: Soul Hunter/Content/Import Soul Lantern, or
    /// -executeMethod SoulHunter.Editor.Content.LightSourceImporter.ImportAll -quit
    /// </summary>
    public static class LightSourceImporter
    {
        public const string Model = "Assets/Art/Models/SH10_Prop_SoulLantern.fbx";
        public const string MaterialFolder = "Assets/Art/Materials/Props";
        public const string PrefabFolder = "Assets/Prefabs/Props";
        public const string PrefabPath = PrefabFolder + "/SH10_SoulLantern.prefab";
        /// <summary>Owner (2026-09-26): the lantern read too small from the game camera; drawn 1.4x the Blender size.</summary>
        public const float Scale = 1.4f;

        private sealed class Look { public Color Color; public float Metallic, Roughness, Strength; public Color Emission; }

        [MenuItem("Soul Hunter/Content/Import Soul Lantern")]
        public static void ImportAll()
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/Art/Materials", "Props");
            if (!AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.CreateFolder("Assets/Prefabs", "Props");

            var importer = AssetImporter.GetAtPath(Model) as ModelImporter;
            if (importer == null) throw new FileNotFoundException("Missing model (run build_light_source.py --export)", Model);
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            foreach (var entry in LoadLooks())
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), entry.Key), LitMaterial(entry.Key, entry.Value));
            importer.SaveAndReimport();

            var prefab = BuildPrefab();
            var catalog = GameContentCatalog.Load();
            if (catalog == null) throw new System.InvalidOperationException("GameContentCatalog (Resources/GameContent) is missing.");
            catalog.SoulLantern = prefab;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("[LightSourceImporter] The Soul Lantern is in the game.");
        }

        private static GameObject BuildPrefab()
        {
            var root = new GameObject("SH10_SoulLantern");
            try
            {
                root.tag = "Enemy";                            // Whip, Bible, Santa Water... hit the "Enemy" tag
                root.layer = LayerMask.NameToLayer("Enemy");   // Garlic, Cross, Soul Burst query the Enemy layer
                var body = root.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
                var trigger = root.AddComponent<CapsuleCollider>();
                trigger.isTrigger = true;
                trigger.radius = 0.45f * Scale;
                trigger.height = 1.4f * Scale;
                trigger.center = new Vector3(0f, 0.7f * Scale, 0f);
                var health = root.AddComponent<HealthController>();
                var so = new SerializedObject(health);
                so.FindProperty("_maxHealth").intValue = LightSource.MaxHealth;
                so.ApplyModifiedPropertiesWithoutUndo();
                root.AddComponent<LightSource>();

                var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model));
                visual.name = "Visual";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localScale = Vector3.one * Scale;
                foreach (var r in visual.GetComponentsInChildren<Renderer>())
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                // Stand on the pivot, like the heroes and the horde.
                var renderers = visual.GetComponentsInChildren<Renderer>();
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                visual.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);

                var glow = new GameObject("GroundGlow");
                glow.transform.SetParent(root.transform, false);
                glow.transform.localPosition = new Vector3(0f, 0.03f, 0f);
                glow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                glow.transform.localScale = Vector3.one * 1.6f * Scale;
                glow.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
                var glowRenderer = glow.AddComponent<MeshRenderer>();
                glowRenderer.sharedMaterial = GlowMaterial();
                glowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                glowRenderer.receiveShadows = false;

                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Dictionary<string, Look> LoadLooks()
        {
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath), "ArtSource", "SH10_LightSource", "materials.json");
            if (!File.Exists(path)) throw new FileNotFoundException("Run build_light_source.py --export first.", path);
            var inv = CultureInfo.InvariantCulture;
            const string num = @"([0-9.eE+-]+)";
            string vec = $@"\[\s*{num},\s*{num},\s*{num}\s*\]";
            var looks = new Dictionary<string, Look>();
            foreach (Match m in Regex.Matches(File.ReadAllText(path),
                         $@"""([^""]+)"":\s*\{{\s*""color"":\s*{vec},\s*""metallic"":\s*{num},\s*""roughness"":\s*{num},\s*""emission"":\s*{vec},\s*""strength"":\s*{num}"))
            {
                float F(int g) => float.Parse(m.Groups[g].Value, inv);
                looks[m.Groups[1].Value] = new Look
                {
                    Color = new Color(F(2), F(3), F(4)), Metallic = F(5), Roughness = F(6),
                    Emission = new Color(F(7), F(8), F(9)), Strength = F(10),
                };
            }
            if (looks.Count == 0) throw new System.InvalidOperationException("No materials in " + path);
            return looks;
        }

        private static Material LitMaterial(string name, Look look)
        {
            string path = $"{MaterialFolder}/SH10_Prop_{name}.mat";
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetColor("_BaseColor", look.Color);
            mat.SetFloat("_Metallic", look.Metallic);
            mat.SetFloat("_Smoothness", Mathf.Clamp01(1f - look.Roughness));
            if (look.Strength > 0.5f)
            {
                // No HDR bloom in the game: scale the glow into 0..1 so the soul flame reads bright but keeps its hue.
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", look.Emission * Mathf.Clamp(look.Strength / 3f, 0.35f, 1f));
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material GlowMaterial()
        {
            string path = $"{MaterialFolder}/SH10_Prop_Lantern_GroundGlow.mat";
            var shader = Shader.Find("SoulHunter/VFX/Particle");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{WeaponVfxBuilder.TextureFolder}/SH10_Vfx_Soft.png"));
            mat.SetColor("_Color", new Color(0.35f, 0.95f, 1f, 0.55f));
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            mat.renderQueue = 3000;
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
