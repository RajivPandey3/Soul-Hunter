#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Environment;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Draw calls (owner, 2026-10-15: "stage floor ko bhi single mesh or material karo"). Every stage floor tile was two
    /// renderers (the ground and its obstacles) with 5-13 textured URP Lit materials, one draw each, and InfiniteMap lays 25
    /// tiles. This joins each renderer's parts into one mesh drawn with one shared material (shader SoulHunter/FloorMerged):
    /// the 16 ground detail textures go into one texture array (SH10_GroundArray, slice 0 white for untextured parts), and
    /// each part's tint, glow and texture slice are baked into its vertices, with the Inverse-graded tint and, for the Blood
    /// Arenas stains, the plain floor that "Disable blood" shows (FloorMergedLook switches both). The ground keeps receiving
    /// shadows only and the obstacles keep casting them, so a tile stays two renderers: 25 tiles draw in 50 calls.
    /// StageFloorImporter runs this after every import. Menu Soul Hunter/Content/Merge Stage Floors, or
    /// -executeMethod SoulHunter.Editor.Content.FloorMeshMerger.MergeAll -quit
    /// </summary>
    public static class FloorMeshMerger
    {
        public const string ArrayPath = StageFloorImporter.TextureFolder + "/SH10_GroundArray.png";
        public const string MaterialPath = "Assets/Art/Materials/FloorMerged/SH10_FloorMerged.mat";
        public const string MeshFolder = "Assets/Art/Models/FloorMerged";
        public const int Slice = 512, Columns = 5, Rows = 4;
        private const string TexturePrefix = "SH10_Tex_";

        /// <summary>The array's slices in order: white first, then every ground texture by name.</summary>
        public static string[] SliceNames()
        {
            var names = Directory.GetFiles(StageFloorImporter.TextureFolder, TexturePrefix + "*.png")
                .Select(Path.GetFileNameWithoutExtension).OrderBy(n => n, System.StringComparer.Ordinal).ToList();
            names.Insert(0, "white");
            return names.ToArray();
        }

        [MenuItem("Soul Hunter/Content/Merge Stage Floors")]
        public static void MergeAll()
        {
            var slices = BuildArray();
            var material = SharedMaterial();
            for (int stage = 1; stage <= StageFloorImporter.Stages; stage++) MergeFloor(StageFloorImporter.FloorName(stage), slices, material);
            AssetDatabase.SaveAssets();
            Debug.Log($"[FloorMeshMerger] Merged {StageFloorImporter.Stages} stage floors into {MaterialPath}.");
        }

        /// <summary>Lays the ground textures out as a 5 x 4 flipbook and imports it as a texture array.</summary>
        private static Dictionary<string, int> BuildArray()
        {
            var names = SliceNames();
            if (names.Length > Columns * Rows) throw new System.InvalidOperationException($"{names.Length} slices do not fit {Columns}x{Rows}.");
            var sheet = new Texture2D(Slice * Columns, Slice * Rows, TextureFormat.RGBA32, false);
            var white = Enumerable.Repeat(new Color32(255, 255, 255, 255), Slice * Slice).ToArray();
            var map = new Dictionary<string, int>();
            for (int i = 0; i < Columns * Rows; i++)
            {
                Color32[] pixels = white;
                if (i > 0 && i < names.Length)
                {
                    var source = new Texture2D(2, 2);
                    source.LoadImage(File.ReadAllBytes($"{StageFloorImporter.TextureFolder}/{names[i]}.png"));
                    if (source.width != Slice || source.height != Slice)
                        throw new System.InvalidOperationException($"{names[i]} is {source.width}x{source.height}, not {Slice}.");
                    pixels = source.GetPixels32();
                    Object.DestroyImmediate(source);
                }
                if (i < names.Length) map[names[i]] = i;
                // Slice 0 is the flipbook's top-left cell; pixel rows count from the bottom.
                int x = i % Columns, y = Rows - 1 - i / Columns;
                sheet.SetPixels32(x * Slice, y * Slice, Slice, Slice, pixels);
            }
            File.WriteAllBytes(ArrayPath, sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
            AssetDatabase.ImportAsset(ArrayPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(ArrayPath);
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2DArray;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.flipbookColumns = Columns;
            settings.flipbookRows = Rows;
            importer.SetTextureSettings(settings);
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.anisoLevel = 4;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
            return map;
        }

        private static Material SharedMaterial()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
            var shader = Shader.Find(FloorMergedLook.ShaderName) ?? throw new System.InvalidOperationException("SoulHunter/FloorMerged not found.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.shader = shader;
            material.SetTexture("_GroundArray", AssetDatabase.LoadAssetAtPath<Texture2DArray>(ArrayPath));
            material.SetFloat("_Smoothness", 0.08f); // StageFloorImporter.ApplyLook
            material.enableInstancing = false;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void MergeFloor(string floor, Dictionary<string, int> slices, Material shared)
        {
            string prefabPath = $"{StageFloorImporter.PrefabFolder}/{floor}.prefab";
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!renderer.enabled) continue; // the hidden _Blockers mesh is only a collider
                    var filter = renderer.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null) continue;
                    if (renderer.sharedMaterials.All(m => m == shared)) continue; // already merged
                    var mesh = Bake(floor, filter.sharedMesh, renderer.sharedMaterials, slices);
                    Directory.CreateDirectory(MeshFolder);
                    string meshPath = $"{MeshFolder}/{floor}_{renderer.name}.asset";
                    var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if (existing != null) { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); mesh = existing; }
                    else AssetDatabase.CreateAsset(mesh, meshPath);
                    filter.sharedMesh = mesh;
                    renderer.sharedMaterials = new[] { shared };
                }
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>One submesh per old material becomes one part; every part gets its own copy of its vertices, so a vertex
        /// two materials shared keeps each one's values.</summary>
        public static Mesh Bake(string floor, Mesh source, Material[] materials, Dictionary<string, int> slices)
        {
            var positions = source.vertices;
            var normals = source.normals;
            var uv0 = new List<Vector2>();
            source.GetUVs(0, uv0);
            var outPos = new List<Vector3>();
            var outNormal = new List<Vector3>();
            var outUv = new List<Vector2>();
            var outColor = new List<Color>();
            var outGlow = new List<Vector4>();
            var outInverse = new List<Vector3>();
            var outPlain = new List<Vector4>();
            var outPlainInverse = new List<Vector3>();
            var triangles = new List<int>();
            var plainMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{StageFloorImporter.MaterialFolder}/{floor}_Base.mat");

            for (int sub = 0; sub < source.subMeshCount; sub++)
            {
                var material = materials[Mathf.Min(sub, materials.Length - 1)];
                bool blood = material != null && material.name.EndsWith("_Blood");
                var look = Look(material, slices);
                var plain = blood && plainMaterial != null ? Look(plainMaterial, slices) : look;
                var remap = new Dictionary<int, int>();
                foreach (int index in source.GetTriangles(sub))
                {
                    if (!remap.TryGetValue(index, out int at))
                    {
                        at = outPos.Count;
                        remap[index] = at;
                        outPos.Add(positions[index]);
                        outNormal.Add(normals.Length > index ? normals[index] : Vector3.up);
                        outUv.Add(uv0.Count > index ? uv0[index] : Vector2.zero);
                        var tint = look.Tint;
                        tint.a = blood ? 1f : 0f;
                        outColor.Add(tint);
                        outGlow.Add(new Vector4(look.Glow.r, look.Glow.g, look.Glow.b, look.Slice));
                        outInverse.Add(V(look.Inverse));
                        outPlain.Add(new Vector4(plain.Tint.r, plain.Tint.g, plain.Tint.b, plain.Slice));
                        outPlainInverse.Add(V(plain.Inverse));
                    }
                    triangles.Add(at);
                }
            }

            var mesh = new Mesh { name = $"{floor}_{source.name}_Merged" };
            if (outPos.Count > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(outPos);
            mesh.SetNormals(outNormal);
            mesh.SetUVs(0, outUv);
            mesh.SetColors(outColor);
            mesh.SetUVs(1, outGlow);
            mesh.SetUVs(2, outInverse);
            mesh.SetUVs(3, outPlain);
            mesh.SetUVs(4, outPlainInverse);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(false);
            return mesh;
        }

        private static Vector3 V(Color c) => new Vector3(c.r, c.g, c.b);

        /// <summary>What URP Lit drew for one floor material: linear tint (x the texture), linear glow, texture slice.</summary>
        public struct PartLook
        {
            public Color Tint, Inverse, Glow;
            public int Slice;
        }

        public static PartLook Look(Material material, Dictionary<string, int> slices)
        {
            if (material == null) return new PartLook { Tint = Color.white, Inverse = Color.white, Glow = Color.black };
            Color baseColor = material.GetColor("_BaseColor");
            var texture = material.GetTexture("_BaseMap");
            int slice = 0;
            if (texture != null && !slices.TryGetValue(texture.name, out slice))
                throw new System.InvalidOperationException($"{material.name}: texture {texture.name} is not in the ground array.");
            Color glow = material.IsKeywordEnabled("_EMISSION") ? material.GetColor("_EmissionColor") : Color.black;
            return new PartLook
            {
                Tint = baseColor.linear,
                Inverse = InverseMap.Grade(baseColor).linear,
                Glow = glow, // HDR colours are stored linear already
                Slice = slice,
            };
        }
    }
}
#endif
