using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using SoulHunter.Gameplay.VFX;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// CPU audit (2026-10-14, owner: "reduce draw calls to one"): every enemy, elite and boss model draws with one
    /// HordeMerged material and one submesh (HordeMeshMerger), and still looks and moves exactly as its FBX did: each old
    /// submesh's triangles carry that material's colour and glow, and the skin is unchanged.
    /// </summary>
    public class HordeMergeTests
    {
        private static IEnumerable<string> ModelPrefabs()
        {
            for (int level = 1; level <= 10; level++)
            {
                yield return $"Assets/Prefabs/Enemies/SH10_L{level:00}_Enemy.prefab";
                yield return $"Assets/Prefabs/Enemies/SH10_L{level:00}_Elite.prefab";
                yield return $"Assets/Prefabs/Bosses/SH10_L{level:00}_Boss.prefab";
            }
        }

        [Test]
        public void EveryHordeModel_DrawsWithOneHordeMaterial_AndOneSubmesh()
        {
            foreach (string path in ModelPrefabs())
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.IsNotNull(prefab, path);
                int drawn = 0;
                foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer is ParticleSystemRenderer) continue;
                    var mesh = MeshOf(renderer);
                    if (mesh == null) continue;
                    drawn++;
                    Assert.That(renderer.sharedMaterials.Length, Is.EqualTo(1), $"{path}/{renderer.name}: one material, one draw call.");
                    Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo(HordeMaterials.ShaderName), $"{path}/{renderer.name}");
                    Assert.That(HordeMaterials.IsHorde(renderer.sharedMaterial), Is.True);
                    Assert.That(mesh.subMeshCount, Is.EqualTo(1), $"{path}/{renderer.name}: one submesh.");
                    Assert.That(renderer.sharedMaterial.GetFloat("_TintAmount"), Is.EqualTo(0f), "The shared material shows the model's own colours.");
                }
                Assert.That(drawn, Is.GreaterThan(0), path + " draws something.");
            }
        }

        [Test]
        public void EveryMergedModel_KeepsItsColoursGlowAndSkin_FromTheFbx()
        {
            // The shader gets material colours in linear space in a Linear project (ProjectSettings m_ActiveColorSpace 1),
            // so the baked vertex colours must be linear too.
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear || PlayerSettings.colorSpace == ColorSpace.Linear;
            Assert.That(linear, Is.True, "Soul Hunter renders in Linear colour space.");
            foreach (string path in ModelPrefabs())
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                {
                    var merged = MeshOf(renderer);
                    if (merged == null || renderer is ParticleSystemRenderer) continue;
                    var original = PrefabUtility.GetCorrespondingObjectFromOriginalSource(renderer);
                    Assert.IsNotNull(original, $"{path}/{renderer.name} comes from its FBX.");
                    var source = MeshOf(original);
                    var materials = original.sharedMaterials;
                    string where = $"{path}/{renderer.name}";
                    Assert.That(source.subMeshCount, Is.GreaterThan(1), where + ": the FBX had several materials.");

                    Vector3[] mergedPos = merged.vertices, sourcePos = source.vertices;
                    Color[] colours = merged.colors;
                    var glow = new List<Vector3>();
                    merged.GetUVs(1, glow);
                    int[] mergedIndices = merged.GetIndices(0);
                    var mergedBones = merged.GetBonesPerVertex();
                    var sourceBones = source.GetBonesPerVertex();
                    var mergedWeights = merged.GetAllBoneWeights();
                    var sourceWeights = source.GetAllBoneWeights();
                    int[] mergedStart = Starts(mergedBones), sourceStart = Starts(sourceBones);
                    int k = 0;
                    for (int s = 0; s < Mathf.Min(source.subMeshCount, materials.Length); s++)
                    {
                        // The model prefabs drew with the project's own material of the same name (Assets/Art/Materials),
                        // not the FBX's embedded one, so that is the colour the merge must keep.
                        var drawnBefore = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Art/Materials/{materials[s].name}.mat");
                        Assert.IsNotNull(drawnBefore, $"{where}: the project material {materials[s].name}.mat");
                        Color want = drawnBefore.GetColor("_Color");
                        Color wantGlow = drawnBefore.GetColor("_EmissionColor");
                        if (linear) { want = want.linear; wantGlow = wantGlow.linear; }
                        foreach (int v in source.GetIndices(s))
                        {
                            int n = mergedIndices[k++];
                            Assert.That(Vector3.Distance(mergedPos[n], sourcePos[v]), Is.LessThan(1e-5f), where + ": same shape.");
                            Assert.That(Near(colours[n], want), Is.True, $"{where}: submesh {s} ({drawnBefore.name}) keeps its colour {want}, got {colours[n]}.");
                            Assert.That(Vector3.Distance(glow[n], new Vector3(wantGlow.r, wantGlow.g, wantGlow.b)), Is.LessThan(1e-3f), $"{where}: submesh {s} keeps its glow.");
                            Assert.That(mergedBones[n], Is.EqualTo(sourceBones[v]), where + ": same bone count per vertex.");
                            for (int w = 0; w < sourceBones[v]; w++)
                            {
                                var a = mergedWeights[mergedStart[n] + w];
                                var b = sourceWeights[sourceStart[v] + w];
                                Assert.That(a.boneIndex == b.boneIndex && Mathf.Abs(a.weight - b.weight) < 1e-5f, Is.True, where + ": same skin weights.");
                            }
                        }
                    }
                    Assert.That(k, Is.EqualTo(mergedIndices.Length), where + ": every triangle is kept, none added.");
                    Assert.That(merged.bindposes, Is.EqualTo(source.bindposes), where + ": same bind poses.");
                    Assert.That(merged.GetVertexAttributeFormat(UnityEngine.Rendering.VertexAttribute.Color),
                        Is.EqualTo(UnityEngine.Rendering.VertexAttributeFormat.Float32), where + ": colours stored exactly (no 8-bit banding).");
                }
            }
        }

        private static Mesh MeshOf(Renderer r) => r is SkinnedMeshRenderer s ? s.sharedMesh
            : r.TryGetComponent(out MeshFilter f) ? f.sharedMesh : null;

        private static int[] Starts(Unity.Collections.NativeArray<byte> perVertex)
        {
            var starts = new int[perVertex.Length];
            for (int v = 1; v < perVertex.Length; v++) starts[v] = starts[v - 1] + perVertex[v - 1];
            return starts;
        }

        private static bool Near(Color a, Color b) => Mathf.Abs(a.r - b.r) < 1e-3f && Mathf.Abs(a.g - b.g) < 1e-3f && Mathf.Abs(a.b - b.b) < 1e-3f;
    }
}
