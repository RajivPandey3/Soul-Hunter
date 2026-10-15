using System.Collections.Generic;
using System.IO;
using SoulHunter.Gameplay.VFX;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// CPU audit (2026-10-14, owner: "reduce draw calls to one"): every enemy, elite and boss model was one mesh with 4-5
    /// flat-colour FacetedPreview materials, so each body on screen cost 4-5 draw calls (1,000-2,500 a frame late in a
    /// level). This bakes each material's colour into the vertex colours (linear, float) and its glow into UV1, keeps the
    /// skin (bone weights, bind poses) as it was, and gives the model one submesh drawn with the shared
    /// SoulHunter/HordeMerged material. A vertex shared by two materials is split, so no colour bleeds. The look is the
    /// same: HordeMerged lights exactly as FacetedPreview did.
    /// Menu: Soul Hunter/Content/Merge Horde Materials, or -executeMethod SoulHunter.Editor.Content.HordeMeshMerger.MergeAll -quit
    /// </summary>
    public static class HordeMeshMerger
    {
        public const string MaterialPath = "Assets/Art/Materials/Horde/SH10_HordeMerged.mat";
        public const string MeshFolder = "Assets/Art/Models/HordeMerged";
        public const string FacetedShader = "SoulHunter/Review/FacetedPreview";

        /// <summary>Every enemy, elite and boss model prefab (the gameplay prefabs and the 235 types nest these).</summary>
        public static IEnumerable<string> ModelPrefabs()
        {
            for (int level = 1; level <= 10; level++)
            {
                yield return $"Assets/Prefabs/Enemies/SH10_L{level:00}_Enemy.prefab";
                yield return $"Assets/Prefabs/Enemies/SH10_L{level:00}_Elite.prefab";
                yield return $"Assets/Prefabs/Bosses/SH10_L{level:00}_Boss.prefab";
            }
        }

        [MenuItem("Soul Hunter/Content/Merge Horde Materials")]
        public static void MergeAll()
        {
            Directory.CreateDirectory(MeshFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
            AssetDatabase.Refresh();
            var material = SharedMaterial();
            int renderers = 0, prefabs = 0;
            foreach (string path in ModelPrefabs())
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) { Debug.LogWarning("[HordeMeshMerger] Missing " + path); continue; }
                var root = PrefabUtility.LoadPrefabContents(path);
                int merged = 0;
                try
                {
                    foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                    {
                        if (renderer is ParticleSystemRenderer) continue;
                        if (MergeRenderer(renderer, Path.GetFileNameWithoutExtension(path), material)) merged++;
                    }
                    if (merged > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
                renderers += merged;
                if (merged > 0) prefabs++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[HordeMeshMerger] Merged {renderers} renderers in {prefabs} model prefabs into one material each.");
        }

        public static Material SharedMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null) return material;
            var shader = Shader.Find(HordeMaterials.ShaderName);
            if (shader == null) throw new FileNotFoundException("Missing shader " + HordeMaterials.ShaderName);
            material = new Material(shader) { name = "SH10_HordeMerged" };
            material.SetColor("_Color", Color.white);
            material.SetFloat("_TintAmount", 0f);
            material.SetColor("_EmissionColor", Color.black);
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static bool MergeRenderer(Renderer renderer, string owner, Material shared)
        {
            var materials = renderer.sharedMaterials;
            if (materials.Length == 0) return false;
            foreach (var m in materials)
                if (m == null || m.shader == null || m.shader.name != FacetedShader) return false; // already merged, or another look

            Mesh source = renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh
                : renderer.TryGetComponent(out MeshFilter filter) ? filter.sharedMesh : null;
            if (source == null) return false;
            if (source.blendShapeCount > 0) { Debug.LogWarning($"[HordeMeshMerger] {owner}/{renderer.name} has blend shapes; left as it was."); return false; }

            var merged = BuildMerged(source, materials);
            merged.name = $"{owner}_{renderer.name}_Merged";
            string assetPath = $"{MeshFolder}/{merged.name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (existing != null) { EditorUtility.CopySerialized(merged, existing); Object.DestroyImmediate(merged); merged = existing; }
            else AssetDatabase.CreateAsset(merged, assetPath);

            if (renderer is SkinnedMeshRenderer smr) smr.sharedMesh = merged;
            else renderer.GetComponent<MeshFilter>().sharedMesh = merged;
            renderer.sharedMaterials = new[] { shared };
            return true;
        }

        /// <summary>One submesh; each old submesh's vertices carry its material's colour (linear) and glow (UV1).</summary>
        public static Mesh BuildMerged(Mesh source, Material[] materials)
        {
            bool linear = PlayerSettings.colorSpace == ColorSpace.Linear;
            Vector3[] positions = source.vertices, normals = source.normals;
            Vector4[] tangents = source.tangents;
            Vector2[] uv0 = source.uv;
            var bonesPerVertex = source.GetBonesPerVertex();
            var weights = source.GetAllBoneWeights();
            bool skinned = bonesPerVertex.Length == positions.Length && weights.Length > 0;
            var weightStart = new int[positions.Length];
            if (skinned) for (int v = 1; v < positions.Length; v++) weightStart[v] = weightStart[v - 1] + bonesPerVertex[v - 1];

            var outPos = new List<Vector3>(); var outNrm = new List<Vector3>(); var outTan = new List<Vector4>(); var outUv = new List<Vector2>();
            var outCol = new List<Color>(); var outGlow = new List<Vector3>();
            var outBones = new List<byte>(); var outWeights = new List<BoneWeight1>();
            var indices = new List<int>();
            var map = new Dictionary<(int, int), int>();

            int submeshes = Mathf.Min(source.subMeshCount, materials.Length);
            for (int s = 0; s < submeshes; s++)
            {
                Color colour = materials[s].HasProperty("_Color") ? materials[s].GetColor("_Color") : Color.white;
                Color glow = materials[s].HasProperty("_EmissionColor") ? materials[s].GetColor("_EmissionColor") : Color.black;
                if (linear) { colour = colour.linear; glow = glow.linear; }
                foreach (int v in source.GetIndices(s))
                {
                    if (!map.TryGetValue((v, s), out int n))
                    {
                        n = outPos.Count;
                        map[(v, s)] = n;
                        outPos.Add(positions[v]);
                        if (normals.Length == positions.Length) outNrm.Add(normals[v]);
                        if (tangents.Length == positions.Length) outTan.Add(tangents[v]);
                        if (uv0.Length == positions.Length) outUv.Add(uv0[v]);
                        outCol.Add(new Color(colour.r, colour.g, colour.b, 1f));
                        outGlow.Add(new Vector3(glow.r, glow.g, glow.b));
                        if (skinned)
                        {
                            outBones.Add(bonesPerVertex[v]);
                            for (int w = 0; w < bonesPerVertex[v]; w++) outWeights.Add(weights[weightStart[v] + w]);
                        }
                    }
                    indices.Add(n);
                }
            }

            var mesh = new Mesh { indexFormat = outPos.Count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            mesh.SetVertices(outPos);
            if (outNrm.Count == outPos.Count) mesh.SetNormals(outNrm);
            if (outTan.Count == outPos.Count) mesh.SetTangents(outTan);
            if (outUv.Count == outPos.Count) mesh.SetUVs(0, outUv);
            mesh.SetColors(outCol);
            mesh.SetUVs(1, outGlow);
            mesh.SetTriangles(indices, 0);
            if (skinned)
            {
                using (var b = new NativeArray<byte>(outBones.ToArray(), Allocator.Temp))
                using (var w = new NativeArray<BoneWeight1>(outWeights.ToArray(), Allocator.Temp))
                    mesh.SetBoneWeights(b, w);
                mesh.bindposes = source.bindposes;
            }
            mesh.bounds = source.bounds;
            return mesh;
        }
    }
}
