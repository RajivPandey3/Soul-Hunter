#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Bug MIS v2 C18 (2026-10-02): the Cross card showed a blue comet, the Bible a brass horn, and Duplicator and
    /// Spellbinder opaque glow squares. This builds Soul Hunter's own icons:
    /// - Cross and Bible: their SH10 weapon models rendered on a transparent background (like the Gold Pouch icon);
    /// - Duplicator and Spellbinder: ArtSource/Generators/build_item_icons.py composes them from the approved VFX v2 sprites.
    /// Each is imported as a sprite and set on every UpgradeData and WeaponDefinition of that item.
    /// Steps: menu Soul Hunter/Content/Item Icons/1 Render Models (RenderFromCommandLine), then build_item_icons.py, then
    /// 3 Assign Icons (AssignFromCommandLine).
    /// </summary>
    public static class ItemIconBuilder
    {
        public const string Folder = "Assets/Art/UI";

        private static readonly (UpgradeData.UpgradeType Type, string Model, Vector3 Euler)[] Rendered =
        {
            (UpgradeData.UpgradeType.Cross, "Assets/Prefabs/Weapons/SH10_Cross.prefab", new Vector3(0f, 0f, 20f)),
            (UpgradeData.UpgradeType.Bible, "Assets/Prefabs/Weapons/SH10_Bible.prefab", new Vector3(-25f, 30f, 0f)),
        };

        private static readonly UpgradeData.UpgradeType[] Composed =
        {
            UpgradeData.UpgradeType.Duplicator, UpgradeData.UpgradeType.Spellbinder,
        };

        public static string IconPath(UpgradeData.UpgradeType type) => $"{Folder}/SH10_Icon_{type}.png";

        /// <summary>Raw model renders, outside Assets; build_item_icons.py puts them on their badges.</summary>
        public static string RawPath(UpgradeData.UpgradeType type) =>
            Path.Combine(Path.GetDirectoryName(Application.dataPath), "ArtSource", "SH10_ItemIcons", $"raw_{type}.png");

        /// <summary>Step 1: renders the Cross and Bible models to ArtSource/SH10_ItemIcons/raw_*.png.</summary>
        [MenuItem("Soul Hunter/Content/Item Icons/1 Render Models")]
        public static void RenderModels()
        {
            foreach (var item in Rendered)
            {
                string raw = RawPath(item.Type);
                Directory.CreateDirectory(Path.GetDirectoryName(raw));
                RenderModel(item.Model, raw, item.Euler);
            }
        }

        /// <summary>Step 3 (after build_item_icons.py): imports the icons as sprites and sets them on the items.</summary>
        [MenuItem("Soul Hunter/Content/Item Icons/3 Assign Icons")]
        public static void AssignAll()
        {
            AssetDatabase.Refresh();
            foreach (var type in Rendered.Select(r => r.Type).Concat(Composed))
                if (File.Exists(IconPath(type))) Assign(type);
                else Debug.LogWarning($"[ItemIconBuilder] {IconPath(type)} missing: run ArtSource/Generators/build_item_icons.py.");
            AssetDatabase.SaveAssets();
        }

        public static void RenderFromCommandLine() => Run(RenderModels);
        public static void AssignFromCommandLine() => Run(AssignAll);

        private static void Run(System.Action action)
        {
            int code = 0;
            try { action(); }
            catch (System.Exception e) { Debug.LogException(e); code = 1; }
            EditorApplication.Exit(code);
        }

        private static void Assign(UpgradeData.UpgradeType type)
        {
            string path = IconPath(type);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            if (sprite == null) { Debug.LogWarning($"[ItemIconBuilder] No sprite at {path}."); return; }
            foreach (string guid in AssetDatabase.FindAssets("t:UpgradeData"))
            {
                var upgrade = AssetDatabase.LoadAssetAtPath<UpgradeData>(AssetDatabase.GUIDToAssetPath(guid));
                if (upgrade == null || upgrade.Type != type) continue;
                upgrade.Icon = sprite;
                EditorUtility.SetDirty(upgrade);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:WeaponDefinition"))
            {
                var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (definition == null || definition.Type != type) continue;
                definition.Icon = sprite;
                EditorUtility.SetDirty(definition);
            }
            Debug.Log($"[ItemIconBuilder] {type} icon: {path}");
        }

        /// <summary>Renders a model prefab, lit and centred, on a transparent 256x256 icon.</summary>
        private static void RenderModel(string prefabPath, string outPath, Vector3 euler)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) throw new FileNotFoundException("Icon model missing", prefabPath);
            var root = new GameObject("IconStage") { hideFlags = HideFlags.HideAndDontSave };
            root.transform.position = new Vector3(0f, -2000f, 0f);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            model.hideFlags = HideFlags.HideAndDontSave;
            model.transform.SetParent(root.transform, false);
            model.transform.localRotation = Quaternion.Euler(euler) * model.transform.localRotation;
            foreach (var c in model.GetComponentsInChildren<Collider>(true)) c.enabled = false;

            var renderers = model.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer)).ToArray();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);

            var key = new GameObject("Key") { hideFlags = HideFlags.HideAndDontSave }.AddComponent<Light>();
            key.type = LightType.Directional; key.intensity = 1.25f; key.color = new Color(1f, 0.96f, 0.9f);
            key.transform.rotation = Quaternion.Euler(35f, -40f, 0f);
            var fill = new GameObject("Fill") { hideFlags = HideFlags.HideAndDontSave }.AddComponent<Light>();
            fill.type = LightType.Directional; fill.intensity = 0.55f; fill.color = new Color(0.6f, 0.75f, 1f);
            fill.transform.rotation = Quaternion.Euler(-20f, 140f, 0f);

            var cam = new GameObject("IconCamera") { hideFlags = HideFlags.HideAndDontSave }.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y) * 1.12f;
            cam.transform.position = bounds.center + Vector3.back * (bounds.extents.magnitude * 4f + 1f);
            cam.transform.rotation = Quaternion.identity;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = bounds.extents.magnitude * 10f + 10f;
            cam.allowHDR = false;

            var rt = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(outPath, tex.EncodeToPNG());

            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(cam.gameObject);
            Object.DestroyImmediate(key.gameObject);
            Object.DestroyImmediate(fill.gameObject);
            Object.DestroyImmediate(root);
        }
    }
}
#endif
