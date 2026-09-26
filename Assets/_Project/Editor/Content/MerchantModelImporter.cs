#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Puts the owner-approved (2026-09-26) Wandering Merchant model into the game. The model is built in Blender by
    /// ArtSource/Generators/build_merchant.py (--export). Materials are URP Lit from the generator's list, and the
    /// soul and ember lanterns glow. The prefab stands 1.11x the player's hero height with a trigger collider and the
    /// WanderingMerchant shop, and GameContentCatalog.WanderingMerchant points at it (CampaignSignatureSystem spawns it).
    /// Menu: Soul Hunter/Content/Import Wandering Merchant, or CLI:
    /// -executeMethod SoulHunter.Editor.Content.MerchantModelImporter.ImportAll -quit
    /// </summary>
    public static class MerchantModelImporter
    {
        public const string ModelPath = "Assets/Art/Models/SH10_NPC_WanderingMerchant.fbx";
        public const string MaterialFolder = "Assets/Art/Materials/NPC";
        public const string PrefabFolder = "Assets/Prefabs/NPC";
        public const string PrefabPath = PrefabFolder + "/SH10_NPC_WanderingMerchant.prefab";
        public const string PlayerPrefab = "Assets/Prefabs/Player/Player.prefab";
        /// <summary>Approved concept: 2.25 tall next to Kael's 2.03.</summary>
        public const float HeightToHero = 1.11f;

        private sealed class Look { public Color Color; public float Metallic, Roughness, Strength; public Color Emission; }

        [MenuItem("Soul Hunter/Content/Import Wandering Merchant")]
        public static void ImportAll()
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/Art/Materials", "NPC");
            if (!AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.CreateFolder("Assets/Prefabs", "NPC");
            ConfigureModel(LoadLooks());
            var prefab = BuildPrefab(HeroHeight() * HeightToHero);

            var catalog = GameContentCatalog.Load();
            if (catalog == null) throw new FileNotFoundException("Resources/GameContent catalog is missing.");
            catalog.WanderingMerchant = prefab;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("[MerchantModelImporter] The Wandering Merchant model is in the game.");
        }

        private static Dictionary<string, Look> LoadLooks()
        {
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath), "ArtSource", "SH10_Merchant_v01", "materials.json");
            if (!File.Exists(path)) throw new FileNotFoundException("Run build_merchant.py --export first.", path);
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
            string path = $"{MaterialFolder}/{name}.mat";
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
                // Same rule as the pickups: no HDR bloom, so the glow is scaled into 0..1 and keeps its hue.
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

        private static void ConfigureModel(Dictionary<string, Look> looks)
        {
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null) throw new FileNotFoundException("Missing model (run build_merchant.py --export)", ModelPath);
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            foreach (var entry in looks)
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), entry.Key), LitMaterial(entry.Key, entry.Value));
            importer.SaveAndReimport();
        }

        /// <summary>Height of the player's hero as it stands in play (the Player prefab's SH10_Visual).</summary>
        private static float HeroHeight()
        {
            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab));
            try
            {
                var visual = player.transform.Find("SH10_Visual");
                if (visual == null) throw new FileNotFoundException("Player prefab has no SH10_Visual child.");
                return Mathf.Max(0.5f, RendererBounds(visual.gameObject).size.y);
            }
            finally { Object.DestroyImmediate(player); }
        }

        public static Bounds RendererBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            return bounds;
        }

        private static GameObject BuildPrefab(float height)
        {
            var root = new GameObject("WanderingMerchant");
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
                model.name = "SH10_NPC_WanderingMerchant";
                model.transform.SetParent(root.transform, false);
                var bounds = RendererBounds(model);
                model.transform.localScale = Vector3.one * (height / Mathf.Max(bounds.size.y, 0.001f));
                bounds = RendererBounds(model);
                // Feet on the ground at the root, centred on it.
                model.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                foreach (var r in model.GetComponentsInChildren<Renderer>())
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

                var trigger = root.AddComponent<CapsuleCollider>();
                trigger.isTrigger = true;
                trigger.radius = 0.9f;
                trigger.height = height;
                trigger.center = new Vector3(0f, height * 0.5f, 0f);
                root.AddComponent<WanderingMerchant>();
                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
#endif
