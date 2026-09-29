#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.VFX;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Puts the owner-approved (2026-09-25) Soul Hunter pickups v2 into the game: Soul Gems in three tiers (cyan shard,
    /// violet cluster, crimson crystal with a gold band), Soul Coins (bronze, silver, gold) and the Soul Reliquary chest,
    /// all built in Blender by ArtSource/Generators/build_pickups_v2.py (--export). Materials are URP Lit made from the
    /// generator's material list (metals, glowing soul parts), and each pickup gets a soft ground glow.
    /// Menu: Soul Hunter/Content/Import Pickups v2, or CLI:
    /// -executeMethod SoulHunter.Editor.Content.PickupV2Importer.ImportAll -quit
    /// </summary>
    public static class PickupV2Importer
    {
        public const string ModelFolder = "Assets/Art/Models";
        public const string MaterialFolder = "Assets/Art/Materials/Pickups";
        public const string GemPrefab = "Assets/Prefabs/Items/XP_Gem.prefab";
        public const string CoinPrefab = "Assets/Prefabs/Items/SH10_Gold_Coin.prefab";
        public const string ChestPrefab = "Assets/Prefabs/Items/Chest.prefab";
        public static readonly int[] GemThresholds = { 21, 91 };    // XP: VS blue "up to 2", green "up to 9", red beyond (Experience_Gem), x10 for SH's 10-XP gems (owner, 2026-09-30)
        public static readonly int[] CoinThresholds = { 5, 25 };    // gold: under 5 bronze, under 25 silver, else gold

        private sealed class Look { public Color Color; public float Metallic, Roughness, Strength; public Color Emission; }

        [MenuItem("Soul Hunter/Content/Import Pickups v2")]
        public static void ImportAll()
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/Art/Materials", "Pickups");
            var looks = LoadLooks();
            string[] models =
            {
                "SH10_Pickup_SoulGem_T1", "SH10_Pickup_SoulGem_T2", "SH10_Pickup_SoulGem_T3",
                "SH10_Pickup_Coin_Bronze", "SH10_Pickup_Coin_Silver", "SH10_Pickup_Coin_Gold", "SH10_Pickup_Chest",
            };
            foreach (string model in models) ConfigureModel(model, looks);

            BuildGem();
            BuildCoin();
            BuildChest();
            AssetDatabase.SaveAssets();
            Debug.Log("[PickupV2Importer] Soul Gems, Soul Coins and the Soul Reliquary chest are in the game.");
        }

        private static Dictionary<string, Look> LoadLooks()
        {
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath), "ArtSource", "SH10_Pickups_v2", "materials.json");
            if (!File.Exists(path)) throw new FileNotFoundException("Run build_pickups_v2.py --export first.", path);
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
            string path = $"{MaterialFolder}/SH10_Pickup_{name}.mat";
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
                // No HDR bloom in the game: scale the glow into 0..1 so soul parts read bright but keep their hue.
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

        private static void ConfigureModel(string model, Dictionary<string, Look> looks)
        {
            string path = $"{ModelFolder}/{model}.fbx";
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new FileNotFoundException("Missing model (run build_pickups_v2.py --export)", path);
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            foreach (var entry in looks)
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), entry.Key), LitMaterial(entry.Key, entry.Value));
            importer.SaveAndReimport();
        }

        private static Material Glow(string name, Color tint)
        {
            string path = $"{MaterialFolder}/SH10_Pickup_Glow_{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("SoulHunter/VFX/Particle");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{WeaponVfxBuilder.TextureFolder}/SH10_Vfx_Soft.png"));
            mat.SetColor("_Color", tint);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            mat.renderQueue = 3000;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>A model instance scaled so its largest side is <paramref name="worldSize"/> (after the parents' scale).</summary>
        private static GameObject Model(Transform parent, string model, float worldSize, Vector3 euler = default)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}/{model}.fbx");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = model;
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var renderers = go.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            float parentScale = parent.lossyScale.x;
            float longest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) / Mathf.Max(parentScale, 0.0001f);
            go.transform.localScale = Vector3.one * (worldSize / parentScale / Mathf.Max(longest, 0.0001f));
            go.transform.localRotation = Quaternion.Euler(euler);
            // Centre it on the pivot.
            renderers = go.GetComponentsInChildren<Renderer>();
            bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            go.transform.position += parent.position - bounds.center;
            foreach (var r in renderers) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return go;
        }

        private static void GroundGlow(Transform parent, Material mat, float worldSize, float worldHeightBelow)
        {
            var glow = new GameObject("GroundGlow");
            glow.transform.SetParent(parent, false);
            float s = parent.lossyScale.x;
            glow.transform.localPosition = new Vector3(0f, -worldHeightBelow / s, 0f);
            glow.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            glow.transform.localScale = Vector3.one * (worldSize / s);
            glow.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var r = glow.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        /// <summary>Removes the pickup's old visual children (keeps the beacon), then makes a fresh "Visual" pivot.</summary>
        private static Transform FreshPivot(GameObject root, float height, Vector3 euler)
        {
            foreach (Transform child in root.transform.Cast<Transform>().ToList())
                if (child.name != "Beacon") Object.DestroyImmediate(child.gameObject);
            var pivot = new GameObject("Visual").transform;
            pivot.SetParent(root.transform, false);
            pivot.localPosition = new Vector3(0f, height / root.transform.lossyScale.y, 0f);
            pivot.localRotation = Quaternion.Euler(euler);
            return pivot;
        }

        private static void Finish(GameObject root, Transform pivot, float bob, float spin)
        {
            var rootRenderer = root.GetComponent<MeshRenderer>();
            if (rootRenderer != null) rootRenderer.enabled = false;
            var motion = root.GetComponent<PickupIdleMotion>() ?? root.AddComponent<PickupIdleMotion>();
            motion.Visual = pivot;
            motion.BobHeight = bob;
            motion.SpinDegreesPerSecond = spin;
        }

        private static void BuildGem()
        {
            var root = PrefabUtility.LoadPrefabContents(GemPrefab);
            try
            {
                var pivot = FreshPivot(root, 0.4f, Vector3.zero);
                var tiers = new[]
                {
                    ("SH10_Pickup_SoulGem_T1", 0.55f, "Cyan", new Color(0.2f, 0.8f, 1f, 0.55f)),
                    ("SH10_Pickup_SoulGem_T2", 0.65f, "Violet", new Color(0.6f, 0.35f, 1f, 0.6f)),
                    ("SH10_Pickup_SoulGem_T3", 0.8f, "Crimson", new Color(1f, 0.2f, 0.3f, 0.65f)),
                };
                var tierObjects = new List<GameObject>();
                foreach (var (model, size, glow, tint) in tiers)
                {
                    var tier = new GameObject(model.Replace("SH10_Pickup_SoulGem_", "Tier_")).transform;
                    tier.SetParent(pivot, false);
                    Model(tier, model, size);
                    GroundGlow(tier, Glow(glow, tint), size * 1.8f, 0.38f);
                    tier.gameObject.SetActive(tierObjects.Count == 0);
                    tierObjects.Add(tier.gameObject);
                }
                var tiered = root.GetComponent<TieredVisual>() ?? root.AddComponent<TieredVisual>();
                tiered.Thresholds = GemThresholds;
                tiered.Tiers = tierObjects.ToArray();
                Finish(root, pivot, 0.12f, 120f);
                PrefabUtility.SaveAsPrefabAsset(root, GemPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void BuildCoin()
        {
            var root = PrefabUtility.LoadPrefabContents(CoinPrefab);
            try
            {
                // The coin model lies flat (face up); stand it up to face the camera, and spin it.
                var pivot = FreshPivot(root, 0.5f, new Vector3(90f, 0f, 0f));
                var tierObjects = new List<GameObject>();
                foreach (string model in new[] { "SH10_Pickup_Coin_Bronze", "SH10_Pickup_Coin_Silver", "SH10_Pickup_Coin_Gold" })
                {
                    var tier = new GameObject(model.Replace("SH10_Pickup_Coin_", "Tier_")).transform;
                    tier.SetParent(pivot, false);
                    Model(tier, model, 0.75f);
                    tier.gameObject.SetActive(tierObjects.Count == 0);
                    tierObjects.Add(tier.gameObject);
                }
                GroundGlow(root.transform, Glow("Gold", new Color(1f, 0.75f, 0.3f, 0.45f)), 1.1f, 0f);
                var tiered = root.GetComponent<TieredVisual>() ?? root.AddComponent<TieredVisual>();
                tiered.Thresholds = CoinThresholds;
                tiered.Tiers = tierObjects.ToArray();
                Finish(root, pivot, 0.1f, 180f);
                PrefabUtility.SaveAsPrefabAsset(root, CoinPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void BuildChest()
        {
            var root = PrefabUtility.LoadPrefabContents(ChestPrefab);
            try
            {
                var pivot = FreshPivot(root, 0.55f, Vector3.zero);
                Model(pivot, "SH10_Pickup_Chest", 1.6f, new Vector3(0f, 180f, 0f)); // lock and skull face the camera
                GroundGlow(root.transform, Glow("Soul", new Color(0.3f, 0.9f, 1f, 0.5f)), 2.6f, 0f);
                Finish(root, pivot, 0.05f, 0f);
                PrefabUtility.SaveAsPrefabAsset(root, ChestPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
#endif
