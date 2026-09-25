#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SoulHunter.Gameplay.Animation;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Wires the Blender hero models (ArtSource/Generators/build_heroes.py) into the game:
    /// 1. imports each SH10_Hero_*.fbx like SH10_Kael (Generic rig, Kael's clip settings) with
    ///    faceted materials from the generator's manifest;
    /// 2. builds a prefab per hero (model + Animator on SH10_Kael.controller + EntityAnimator);
    /// 3. points each CharacterData at its hero prefab and renders its menu portrait from it.
    /// Menu: Soul Hunter/Content/Import Hero Models, or CLI:
    /// -executeMethod SoulHunter.Editor.Content.HeroModelImporter.ImportAll -quit
    /// </summary>
    public static class HeroModelImporter
    {
        public const string ModelFolder = "Assets/Art/Models";
        public const string MaterialFolder = "Assets/Art/Materials";
        public const string PrefabFolder = "Assets/Prefabs/Player";
        public const string PortraitFolder = "Assets/_Project/Art/Portraits";
        public const string KaelModel = "Assets/Art/Models/SH10_Kael.fbx";
        public const string KaelController = "Assets/_Project/Animation/Controllers/SH10_Kael.controller";
        private const string ShaderName = "SoulHunter/Review/FacetedPreview";
        private const int PortraitSize = 256;
        private static readonly Color KeyColor = new Color(1f, 0f, 1f, 1f);

        /// <summary>Character asset name -> hero model name (SH10_Hero_*).</summary>
        public static readonly Dictionary<string, string> CharacterModels = new Dictionary<string, string>
        {
            { "Char_Kael", "SH10_Hero_Kael" },
            { "Char_Corvan", "SH10_Hero_Corvan" },
            { "Char_Aria", "SH10_Hero_Aria" },
            { "Char_Draven", "SH10_Hero_Draven" },
            { "Char_Gideon", "SH10_Hero_Gideon" },
            { "Char_Isolde", "SH10_Hero_Isolde" },
            { "Char_Kaelen", "SH10_Hero_Kaelen" },
            { "Char_Lumina", "SH10_Hero_Lumina" },
            { "Char_Mage", "SH10_Hero_Elaria" },
            { "Char_Rurik", "SH10_Hero_Rurik" },
            { "Char_Seraphina", "SH10_Hero_Seraphina" },
            { "Char_Sylas", "SH10_Hero_Sylas" },
            { "Char_Valerius", "SH10_Hero_Valerius" },
        };

        [MenuItem("Soul Hunter/Content/Import Hero Models")]
        public static void ImportAll()
        {
            var palettes = LoadManifest();
            foreach (string model in CharacterModels.Values.Distinct())
            {
                ConfigureModel(model, palettes);
                BuildPrefab(model);
            }
            AssetDatabase.SaveAssets();
            AssignCharactersAndPortraits();
            AssetDatabase.SaveAssets();
            Debug.Log($"[HeroModelImporter] Imported {CharacterModels.Count} hero models, prefabs and portraits.");
        }

        public static string PrefabPath(string model) => $"{PrefabFolder}/SH10_Hero_{model.Replace("SH10_Hero_", "").Replace("SH10_", "")}.prefab";

        private static Dictionary<string, Color> LoadManifest()
        {
            var colors = new Dictionary<string, Color>();
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath), "ArtSource", "SH10_Heroes_v01", "manifest.json");
            if (!File.Exists(path)) throw new FileNotFoundException("Run build_heroes.py first.", path);
            // Minimal parse of {"materials": {"Name": [r, g, b]}} entries.
            string json = File.ReadAllText(path);
            var matches = System.Text.RegularExpressions.Regex.Matches(json,
                "\"(SH10_Hero_[A-Za-z]+_[A-Za-z]+)\":\\s*\\[\\s*([0-9.eE+-]+),\\s*([0-9.eE+-]+),\\s*([0-9.eE+-]+)\\s*\\]");
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            foreach (System.Text.RegularExpressions.Match m in matches)
                colors[m.Groups[1].Value] = new Color(float.Parse(m.Groups[2].Value, inv), float.Parse(m.Groups[3].Value, inv), float.Parse(m.Groups[4].Value, inv), 1f);
            return colors;
        }

        private static void ConfigureModel(string model, Dictionary<string, Color> palettes)
        {
            string path = $"{ModelFolder}/{model}.fbx";
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new FileNotFoundException("Missing hero model", path);
            var kael = (ModelImporter)AssetImporter.GetAtPath(KaelModel);

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = kael.avatarSetup;
            importer.importAnimation = true;
            importer.clipAnimations = kael.clipAnimations; // same take names (Rig|Idle, ...) and loop settings
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;

            var shader = Shader.Find(ShaderName);
            foreach (var entry in palettes.Where(p => p.Key.StartsWith(model + "_")))
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

        private static void BuildPrefab(string model)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}/{model}.fbx");
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(KaelController);
            var root = new GameObject(Path.GetFileNameWithoutExtension(PrefabPath(model)));
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                instance.transform.SetParent(root.transform, false);
                var animator = instance.GetComponent<Animator>();
                if (animator == null) animator = instance.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                if (instance.GetComponent<EntityAnimator>() == null) instance.AddComponent<EntityAnimator>();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath(model));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void AssignCharactersAndPortraits()
        {
            Directory.CreateDirectory(PortraitFolder);
            var previousSetup = EditorSceneManager.GetSceneManagerSetup();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var portraits = new List<(string character, string portrait)>();
            try
            {
                foreach (var pair in CharacterModels)
                {
                    string portrait = $"{PortraitFolder}/Portrait_{pair.Key.Replace("Char_", "")}.png";
                    File.WriteAllBytes(portrait, RenderPortrait(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(pair.Value))));
                    portraits.Add((pair.Key, portrait));
                }
            }
            finally
            {
                if (previousSetup != null && previousSetup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }

            foreach (var (_, portrait) in portraits)
            {
                AssetDatabase.ImportAsset(portrait, ImportAssetOptions.ForceUpdate);
                var importer = (TextureImporter)AssetImporter.GetAtPath(portrait);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            // Reload after the imports: references held across them go stale.
            foreach (var (character, portrait) in portraits)
            {
                var data = AssetDatabase.LoadAssetAtPath<CharacterData>($"Assets/_Project/Data/Characters/{character}.asset");
                if (data == null) { Debug.LogError($"[HeroModelImporter] Missing {character}.asset"); continue; }
                data.CharacterModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(CharacterModels[character]));
                data.CharacterIcon = AssetDatabase.LoadAssetAtPath<Sprite>(portrait);
                EditorUtility.SetDirty(data);
            }
        }

        /// <summary>Full-body front view of a hero prefab on a transparent background.</summary>
        private static byte[] RenderPortrait(GameObject prefab)
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var cameraGo = new GameObject("PortraitCamera");
            var target = new RenderTexture(PortraitSize, PortraitSize, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
            try
            {
                var renderers = model.GetComponentsInChildren<Renderer>();
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);

                var camera = cameraGo.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = bounds.extents.y * 1.06f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = KeyColor;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                // Hero models face +Z in Unity; view them from the front, slightly from the side.
                Vector3 view = Quaternion.Euler(0f, -18f, 0f) * Vector3.forward;
                cameraGo.transform.position = bounds.center + view * 10f;
                cameraGo.transform.LookAt(bounds.center);
                camera.targetTexture = target;
                camera.Render();

                var previous = RenderTexture.active;
                RenderTexture.active = target;
                var texture = new Texture2D(PortraitSize, PortraitSize, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, PortraitSize, PortraitSize), 0, 0);
                RenderTexture.active = previous;
                camera.targetTexture = null;

                var pixels = texture.GetPixels();
                for (int i = 0; i < pixels.Length; i++)
                {
                    var p = pixels[i];
                    if (p.r > 0.8f && p.b > 0.8f && p.g < 0.25f) pixels[i] = new Color(0f, 0f, 0f, 0f);
                }
                texture.SetPixels(pixels);
                texture.Apply();
                byte[] png = texture.EncodeToPNG();
                Object.DestroyImmediate(texture);
                return png;
            }
            finally
            {
                Object.DestroyImmediate(model);
                Object.DestroyImmediate(cameraGo);
                target.Release();
                Object.DestroyImmediate(target);
            }
        }
    }
}
#endif
