using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Environment;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Draw calls (owner, 2026-10-15: "stage floor ko bhi single mesh or material karo"): every stage floor tile draws as
    /// at most two renderers (ground, obstacles) with one mesh and the one shared FloorMerged material each, and every part
    /// keeps what URP Lit drew: its tint, glow and ground texture, its Inverse grade and, for a Blood Arenas stain, the plain
    /// floor that "Disable blood" shows.
    /// </summary>
    public class FloorMergeTests
    {
        // The editor code (FloorMeshMerger, StageFloorImporter) is not referenced by this assembly; its constants are
        // repeated here and its bake is called by reflection, as PropMergeTests does.
        private const string ArrayPath = "Assets/Art/Textures/Ground/SH10_GroundArray.png";
        private const string MaterialPath = "Assets/Art/Materials/FloorMerged/SH10_FloorMerged.mat";
        private const string PrefabFolder = "Assets/Prefabs/Environment", ModelFolder = "Assets/Art/Models", MaterialFolder = "Assets/Art/Materials";
        private static string FloorName(int stage) => $"SH10_L{stage:00}_Floor";
        private static IEnumerable<int> Stages() => Enumerable.Range(1, 10);

        private static System.Type Merger => System.AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("SoulHunter.Editor.Content.FloorMeshMerger", false)).First(t => t != null);
        private static string[] SliceNames() => (string[])Merger.GetMethod("SliceNames").Invoke(null, null);
        private static Mesh Bake(string floor, Mesh source, Material[] materials, Dictionary<string, int> slices) =>
            (Mesh)Merger.GetMethod("Bake").Invoke(null, new object[] { floor, source, materials, slices });
        private struct PartLook { public Color Tint, Inverse; public int Slice; }
        private static PartLook Look(Material material, Dictionary<string, int> slices)
        {
            object look = Merger.GetMethod("Look").Invoke(null, new object[] { material, slices });
            var type = look.GetType();
            return new PartLook
            {
                Tint = (Color)type.GetField("Tint").GetValue(look),
                Inverse = (Color)type.GetField("Inverse").GetValue(look),
                Slice = (int)type.GetField("Slice").GetValue(look),
            };
        }

        [Test]
        public void TheGroundArray_HoldsWhiteAndEveryGroundTexture()
        {
            var array = AssetDatabase.LoadAssetAtPath<Texture2DArray>(ArrayPath);
            Assert.IsNotNull(array, "SH10_GroundArray imports as a texture array.");
            var names = SliceNames();
            Assert.That(names.Length, Is.EqualTo(17), "white + the 16 ground textures");
            Assert.That(array.depth, Is.GreaterThanOrEqualTo(names.Length));
            Assert.That(array.width, Is.EqualTo(512));
            Assert.That(array.wrapMode, Is.EqualTo(TextureWrapMode.Repeat), "The floor's world UVs repeat.");
            Assert.That(array.mipmapCount, Is.GreaterThan(1));
            // No alpha: like the ground PNGs it compresses to DXT1, not DXT5 (twice the reads; -11% FPS on Level 4).
            var importer = (TextureImporter)AssetImporter.GetAtPath(ArrayPath);
            Assert.That(importer.alphaSource, Is.EqualTo(TextureImporterAlphaSource.None));
            Assert.That(importer.DoesSourceTextureHaveAlpha(), Is.False);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Assert.That(material.shader.name, Is.EqualTo(FloorMergedLook.ShaderName));
            Assert.That(material.GetTexture("_GroundArray"), Is.EqualTo(array));
        }

        [Test]
        public void EveryFloorTile_IsAtMostTwoRenderers_OfOneMeshAndTheOneMaterial([ValueSource(nameof(Stages))] int stage)
        {
            var shared = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{FloorName(stage)}.prefab");
            var drawn = prefab.GetComponentsInChildren<MeshRenderer>().Where(r => r.enabled).ToList();
            Assert.That(drawn.Count, Is.InRange(1, 2), "ground + obstacles");
            foreach (var renderer in drawn)
            {
                Assert.That(renderer.sharedMaterials, Is.EqualTo(new[] { shared }), renderer.name);
                var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(mesh.subMeshCount, Is.EqualTo(1), renderer.name);
                Assert.That(mesh.isReadable, Is.True, "The tests and the Disable Blood check read the baked looks.");
            }
            var obstacles = drawn.FirstOrDefault(r => r.name.EndsWith("_Obstacles"));
            if (obstacles != null) Assert.That(obstacles.shadowCastingMode, Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.On));
            var ground = drawn.First(r => !r.name.EndsWith("_Obstacles"));
            Assert.That(ground.shadowCastingMode, Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.Off), "The ground only receives shadows.");
        }

        /// <summary>Baking the old FBX materials again gives exactly what the prefab's merged mesh holds, part by part.</summary>
        [Test]
        public void EveryPart_KeepsItsTintGlowTextureAndInverseGrade([ValueSource(nameof(Stages))] int stage)
        {
            string floor = FloorName(stage);
            var slices = SliceNames().Select((n, i) => (n, i)).ToDictionary(x => x.n, x => x.i);
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}/{floor}.fbx");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{floor}.prefab");
            foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>().Where(r => r.enabled))
            {
                var source = fbx.GetComponentsInChildren<MeshRenderer>(true).First(r => r.name == renderer.name);
                var expected = Bake(floor, source.GetComponent<MeshFilter>().sharedMesh, source.sharedMaterials, slices);
                var actual = renderer.GetComponent<MeshFilter>().sharedMesh;
                try
                {
                    Assert.That(actual.vertexCount, Is.EqualTo(expected.vertexCount), renderer.name);
                    Assert.That(actual.colors, Is.EqualTo(expected.colors), "tint and stain flag");
                    for (int channel = 1; channel <= 4; channel++)
                    {
                        var a = new List<Vector4>(); actual.GetUVs(channel, a);
                        var e = new List<Vector4>(); expected.GetUVs(channel, e);
                        Assert.That(a, Is.EqualTo(e), $"{renderer.name} UV{channel}");
                    }
                    Assert.That(actual.vertices, Is.EqualTo(expected.vertices), "positions unchanged");
                }
                finally { Object.DestroyImmediate(expected); }

                // Every textured part points at its own texture's slice.
                foreach (var material in source.sharedMaterials)
                {
                    var look = Look(material, slices);
                    var texture = material.GetTexture("_BaseMap");
                    Assert.That(look.Slice, Is.EqualTo(texture == null ? 0 : slices[texture.name]), material.name);
                    Assert.That(look.Inverse, Is.EqualTo(InverseMap.Grade(material.GetColor("_BaseColor")).linear), material.name);
                }
            }
        }

        [Test]
        public void TheBloodArenaStains_CarryThePlainFloorThatDisableBloodShows()
        {
            string floor = FloorName(8);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{floor}.prefab");
            var plain = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{floor}_Base.mat");
            var slices = SliceNames().Select((n, i) => (n, i)).ToDictionary(x => x.n, x => x.i);
            var plainLook = Look(plain, slices);
            int stains = 0;
            foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>().Where(r => r.enabled))
            {
                var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                var colors = mesh.colors;
                var plainUv = new List<Vector4>(); mesh.GetUVs(3, plainUv);
                for (int i = 0; i < colors.Length; i++)
                {
                    if (colors[i].a < 0.5f) continue;
                    stains++;
                    Assert.That(plainUv[i], Is.EqualTo(new Vector4(plainLook.Tint.r, plainLook.Tint.g, plainLook.Tint.b, plainLook.Slice)));
                }
            }
            Assert.That(stains, Is.GreaterThan(0), "Level 8 has blood stains.");
            foreach (int stage in Stages().Where(s => s != 8))
            {
                var other = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{FloorName(stage)}.prefab");
                Assert.That(other.GetComponentsInChildren<MeshFilter>().Where(f => f.sharedMesh != null && f.GetComponent<MeshRenderer>().enabled)
                    .SelectMany(f => f.sharedMesh.colors).Any(c => c.a > 0.5f), Is.False, $"Level {stage} has no stains.");
            }
        }
    }
}
