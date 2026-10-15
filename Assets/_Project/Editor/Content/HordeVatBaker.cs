using System.Collections.Generic;
using System.IO;
using System.Linq;
using SoulHunter.Gameplay.AI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Horde drawing groundwork (2026-10-15): bakes an enemy model's clips into vertex animation textures, so the horde
    /// can be drawn with GPU instancing (SoulHunter/HordeVat) instead of one SkinnedMeshRenderer and Animator per body.
    /// For each model it writes, under Assets/Art/Generated/HordeVat/&lt;Model&gt;/:
    /// - a position and a normal texture (RGBAHalf, model root space, one block of rows per frame);
    /// - a static rest mesh whose TEXCOORD3.x is each vertex's index (submeshes kept, one per material);
    /// - one SoulHunter/HordeVat material per submesh (instancing on, colours copied from the source material);
    /// - a HordeVatAsset that lists all of the above with each clip's frames.
    /// The mesh and materials come from the model prefab (Assets/Prefabs/Enemies/&lt;Model&gt;.prefab) when it exists,
    /// because HordeMeshMerger gave it one merged material with colours in the vertices; otherwise from the FBX.
    /// Clips always come from the FBX. Several renderers in one model are combined into one mesh, with a submesh per
    /// distinct material. Blend shapes are not baked.
    /// Menu: Soul Hunter/Content/Bake Horde VAT, or -executeMethod SoulHunter.Editor.Content.HordeVatBaker.BakeAll -quit
    /// </summary>
    public static class HordeVatBaker
    {
        public const string OutputRoot = "Assets/Art/Generated/HordeVat";
        public const string ModelFolder = "Assets/Art/Models";
        public const string ModelPrefabFolder = "Assets/Prefabs/Enemies";
        public const string ShaderName = "SoulHunter/HordeVat";

        /// <summary>Clips in this order come first; any other clip in the FBX follows by name.</summary>
        public static readonly string[] ClipOrder = { "Idle", "Run", "Attack", "Death" };

        /// <summary>Wider textures are split into several rows per frame. 2048 is safe on every target, phones included.</summary>
        public const int MaxTextureWidth = 2048;
        /// <summary>The models' clips are 25 frames; sampling above 30 per second only adds rows the eye cannot see.</summary>
        public const float MaxSampleFps = 30f;
        /// <summary>Phones guarantee 4096; desktop allows more, but a taller bake means a model that needs a rethink.</summary>
        public const int MaxTextureHeight = 4096;

        [MenuItem("Soul Hunter/Content/Bake Horde VAT")]
        public static void BakeAll()
        {
            var models = Directory.GetFiles(ModelFolder, "SH10_L??_Enemy.fbx")
                .Concat(Directory.GetFiles(ModelFolder, "SH10_L??_Elite.fbx"))
                .Where(p => p.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) // .NET wildcards can over-match
                .Select(p => p.Replace('\\', '/'))
                .OrderBy(p => p)
                .ToList();
            int baked = 0;
            try
            {
                for (int i = 0; i < models.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("Bake Horde VAT", models[i], (float)i / Mathf.Max(1, models.Count));
                    if (Bake(models[i]) != null) baked++;
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            // The game finds the baked models through one library in Resources (HordeVatLibrary.Load).
            var library = AssetDatabase.LoadAssetAtPath<HordeVatLibrary>(HordeVatLibrary.AssetPath);
            if (library == null) { library = ScriptableObject.CreateInstance<HordeVatLibrary>(); AssetDatabase.CreateAsset(library, HordeVatLibrary.AssetPath); }
            library.Assets = AssetDatabase.FindAssets("t:HordeVatAsset", new[] { OutputRoot })
                .Select(guid => AssetDatabase.LoadAssetAtPath<HordeVatAsset>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(a => a != null && a.SourceMesh != null).OrderBy(a => a.name).ToArray();
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log($"[HordeVatBaker] Baked {baked} of {models.Count} models into {OutputRoot}.");
        }

        /// <summary>A renderer of the model, with what the bake needs from it.</summary>
        private sealed class Part
        {
            public Renderer Renderer;
            public SkinnedMeshRenderer Skinned;
            public Mesh Mesh;
            public Material[] Materials;
            public int VertexOffset;
            public Vector3[] StaticVertices, StaticNormals;
        }

        /// <summary>Bakes one FBX (clips) with its model prefab (mesh, materials) when there is one. Returns the asset or null.</summary>
        public static HordeVatAsset Bake(string fbxPath)
        {
            string modelName = Path.GetFileNameWithoutExtension(fbxPath);
            string prefabPath = $"{ModelPrefabFolder}/{modelName}.prefab";
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            string sourcePath = prefabPath;
            if (source == null) { source = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath); sourcePath = fbxPath; }
            if (source == null) { Debug.LogWarning("[HordeVatBaker] Missing model " + fbxPath); return null; }

            var clips = OrderedClips(fbxPath);
            if (clips.Count == 0) { Debug.LogWarning("[HordeVatBaker] No animation clips in " + fbxPath); return null; }

            var instance = Object.Instantiate(source);
            instance.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                // Bake in the model root's space: the root sits at the origin, so the gameplay prefab's visual child
                // transform (position, turn, size) applies on top at draw time, as it does for the skinned body.
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                instance.transform.localScale = Vector3.one;
                var animator = instance.GetComponentInChildren<Animator>();
                if (animator != null) animator.enabled = false; // SampleAnimation drives the bones; nothing else should

                var parts = CollectParts(instance, modelName);
                if (parts.Count == 0) { Debug.LogWarning($"[HordeVatBaker] {modelName} has no mesh renderers."); return null; }
                int vertexCount = 0;
                foreach (var part in parts) { part.VertexOffset = vertexCount; vertexCount += part.Mesh.vertexCount; }
                if (vertexCount == 0) { Debug.LogWarning($"[HordeVatBaker] {modelName} has no vertices."); return null; }

                // Layout: one block of rows per frame.
                int width = Mathf.Min(vertexCount, MaxTextureWidth);
                int rowsPerFrame = (vertexCount + width - 1) / width;
                var clipInfos = new HordeVatAsset.Clip[clips.Count];
                int totalFrames = 0;
                for (int c = 0; c < clips.Count; c++)
                {
                    var clip = clips[c];
                    float fps = Mathf.Min(clip.frameRate > 0f ? clip.frameRate : MaxSampleFps, MaxSampleFps);
                    bool loop = clip.isLooping;
                    // A loop's last key repeats its first, so it is left out and playback wraps; a one-shot keeps its end.
                    int frames = Mathf.Max(1, Mathf.RoundToInt(clip.length * fps) + (loop ? 0 : 1));
                    clipInfos[c] = new HordeVatAsset.Clip
                    {
                        Name = clip.name, StartFrame = totalFrames, StartRow = totalFrames * rowsPerFrame,
                        FrameCount = frames, Fps = fps, Loop = loop,
                    };
                    totalFrames += frames;
                }
                int height = totalFrames * rowsPerFrame;
                if (height > MaxTextureHeight)
                {
                    Debug.LogError($"[HordeVatBaker] {modelName}: {height} rows ({vertexCount} vertices, {totalFrames} frames) is past {MaxTextureHeight}; not baked.");
                    return null;
                }

                var positionData = new ushort[width * height * 4];
                var normalData = new ushort[width * height * 4];
                ushort one = Mathf.FloatToHalf(1f), zero = Mathf.FloatToHalf(0f);
                var bounds = new Bounds();
                bool boundsSet = false;
                Vector3[] restPositions = null, restNormals = null;

                // The root stays at the origin; a curve on the root itself moves it, and that motion is kept.
                Matrix4x4 rootFromWorld = Matrix4x4.identity;
                var baked = new Mesh();
                try
                {
                    for (int c = 0; c < clips.Count; c++)
                    {
                        var info = clipInfos[c];
                        for (int f = 0; f < info.FrameCount; f++)
                        {
                            float time = Mathf.Min(f / info.Fps, clips[c].length);
                            clips[c].SampleAnimation(instance, time);
                            int frame = info.StartFrame + f;
                            bool keepRest = restPositions == null; // the first frame of the first clip (Idle) is the rest pose
                            if (keepRest) { restPositions = new Vector3[vertexCount]; restNormals = new Vector3[vertexCount]; }

                            foreach (var part in parts)
                            {
                                Vector3[] vertices, normals;
                                Matrix4x4 toRoot;
                                var t = part.Renderer.transform;
                                if (part.Skinned != null)
                                {
                                    // BakeMesh without useScale gives the pose at its real size in the renderer's turned space
                                    // (measured 2026-10-15: these renderers are scaled 100 with bones to match; with useScale the
                                    // L01 enemy baked 0.01 tall, without it 1.49 x 0.70 x 2.03, its drawn size), so only the
                                    // renderer's position and turn are left to apply.
                                    part.Skinned.BakeMesh(baked, false);
                                    vertices = baked.vertices;
                                    normals = baked.normals;
                                    toRoot = rootFromWorld * Matrix4x4.TRS(t.position, t.rotation, Vector3.one);
                                }
                                else
                                {
                                    vertices = part.StaticVertices;
                                    normals = part.StaticNormals;
                                    toRoot = rootFromWorld * t.localToWorldMatrix;
                                }
                                var normalMatrix = toRoot.inverse.transpose;
                                bool hasNormals = normals != null && normals.Length == vertices.Length;
                                for (int v = 0; v < vertices.Length; v++)
                                {
                                    Vector3 p = toRoot.MultiplyPoint3x4(vertices[v]);
                                    Vector3 n = hasNormals ? normalMatrix.MultiplyVector(normals[v]).normalized : Vector3.up;
                                    int index = part.VertexOffset + v;
                                    int texel = ((frame * rowsPerFrame + index / width) * width + index % width) * 4;
                                    positionData[texel] = Mathf.FloatToHalf(p.x);
                                    positionData[texel + 1] = Mathf.FloatToHalf(p.y);
                                    positionData[texel + 2] = Mathf.FloatToHalf(p.z);
                                    positionData[texel + 3] = one;
                                    normalData[texel] = Mathf.FloatToHalf(n.x);
                                    normalData[texel + 1] = Mathf.FloatToHalf(n.y);
                                    normalData[texel + 2] = Mathf.FloatToHalf(n.z);
                                    normalData[texel + 3] = zero;
                                    if (!boundsSet) { bounds = new Bounds(p, Vector3.zero); boundsSet = true; }
                                    else bounds.Encapsulate(p);
                                    if (keepRest) { restPositions[index] = p; restNormals[index] = n; }
                                }
                            }
                        }
                    }
                }
                finally { Object.DestroyImmediate(baked); }
                bounds.Expand(0.05f); // half floats round a little; keep culling from clipping an arm

                string folder = $"{OutputRoot}/{modelName}";
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();

                var positions = SaveTexture($"{folder}/{modelName}_VatPositions.asset", width, height, positionData);
                var normalsTexture = SaveTexture($"{folder}/{modelName}_VatNormals.asset", width, height, normalData);

                var restMesh = BuildRestMesh(parts, vertexCount, restPositions, restNormals, bounds, out var sourceMaterials);
                restMesh.name = $"{modelName}_VatMesh";
                restMesh = SaveOrReplace(restMesh, $"{folder}/{modelName}_VatMesh.asset");

                var materials = BuildMaterials(folder, modelName, sourceMaterials, positions, normalsTexture, width, rowsPerFrame);

                string assetPath = $"{folder}/{modelName}_Vat.asset";
                var asset = AssetDatabase.LoadAssetAtPath<HordeVatAsset>(assetPath);
                if (asset == null) { asset = ScriptableObject.CreateInstance<HordeVatAsset>(); AssetDatabase.CreateAsset(asset, assetPath); }
                asset.Mesh = restMesh;
                asset.Positions = positions;
                asset.Normals = normalsTexture;
                asset.Materials = materials;
                asset.VertexCount = vertexCount;
                asset.TextureWidth = width;
                asset.RowsPerFrame = rowsPerFrame;
                asset.TotalFrames = totalFrames;
                asset.Clips = clipInfos;
                asset.Bounds = bounds;
                asset.SourceMesh = parts.Count == 1 && parts[0].Skinned != null ? parts[0].Mesh : null;
                asset.SourceModel = source;
                asset.SourceModelPath = sourcePath;
                asset.SourceClipsPath = fbxPath;
                asset.SourceMaterials = sourceMaterials;
                asset.SourceRenderers = parts.Select(p => PathUnder(instance.transform, p.Renderer.transform)).ToArray();
                EditorUtility.SetDirty(asset);
                Debug.Log($"[HordeVatBaker] {modelName}: {vertexCount} vertices, {parts.Count} renderers, {sourceMaterials.Length} submeshes, " +
                          $"{totalFrames} frames ({string.Join(", ", clipInfos.Select(c => $"{c.Name} {c.FrameCount}@{c.Fps:0.#}"))}), texture {width}x{height}.");
                return asset;
            }
            finally { Object.DestroyImmediate(instance); }
        }

        /// <summary>The FBX's clips (preview clips left out): Idle, Run, Attack, Death first, then the rest by name.</summary>
        private static List<AnimationClip> OrderedClips(string fbxPath)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__")).ToList();
            var ordered = new List<AnimationClip>();
            foreach (string name in ClipOrder)
            {
                var clip = all.FirstOrDefault(c => string.Equals(c.name, name, System.StringComparison.OrdinalIgnoreCase));
                if (clip != null) ordered.Add(clip);
                else Debug.LogWarning($"[HordeVatBaker] {fbxPath} has no {name} clip.");
            }
            ordered.AddRange(all.Where(c => !ordered.Contains(c)).OrderBy(c => c.name));
            return ordered;
        }

        /// <summary>Every active mesh renderer of the model (particles left out), skinned or rigid.</summary>
        private static List<Part> CollectParts(GameObject root, string modelName)
        {
            var parts = new List<Part>();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(false))
            {
                if (!renderer.enabled) continue;
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    if (skinned.sharedMesh == null) continue;
                    if (skinned.sharedMesh.blendShapeCount > 0)
                        Debug.LogWarning($"[HordeVatBaker] {modelName}/{renderer.name} has blend shapes; they are baked at their current weights.");
                    parts.Add(new Part { Renderer = renderer, Skinned = skinned, Mesh = skinned.sharedMesh, Materials = skinned.sharedMaterials });
                }
                else if (renderer is MeshRenderer && renderer.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null)
                {
                    // A rigid part (a prop on a bone) follows its transform in every frame.
                    parts.Add(new Part
                    {
                        Renderer = renderer, Mesh = filter.sharedMesh, Materials = renderer.sharedMaterials,
                        StaticVertices = filter.sharedMesh.vertices, StaticNormals = filter.sharedMesh.normals,
                    });
                }
            }
            return parts;
        }

        /// <summary>
        /// One static mesh for every part: rest pose (bounds and a sensible look if drawn without the shader), the model's
        /// colours (vertex colours) and glow (UV1) as HordeMeshMerger left them, and the vertex index in TEXCOORD3.x.
        /// Submeshes of every part that share a material become one submesh.
        /// </summary>
        private static Mesh BuildRestMesh(List<Part> parts, int vertexCount, Vector3[] positions, Vector3[] normals, Bounds bounds,
            out Material[] submeshMaterials)
        {
            var colours = new Color[vertexCount];
            var uv0 = new Vector2[vertexCount];
            var glow = new Vector3[vertexCount];
            var vertexIndex = new Vector2[vertexCount];
            var materials = new List<Material>();
            var submeshIndices = new List<List<int>>();
            var glowList = new List<Vector3>();

            foreach (var part in parts)
            {
                var mesh = part.Mesh;
                int count = mesh.vertexCount;
                var meshColours = mesh.colors;
                var meshUv = mesh.uv;
                glowList.Clear();
                mesh.GetUVs(1, glowList);
                for (int v = 0; v < count; v++)
                {
                    int i = part.VertexOffset + v;
                    colours[i] = meshColours.Length == count ? meshColours[v] : Color.white;
                    uv0[i] = meshUv.Length == count ? meshUv[v] : Vector2.zero;
                    glow[i] = glowList.Count == count ? glowList[v] : Vector3.zero;
                    vertexIndex[i] = new Vector2(i, 0f);
                }
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    if (mesh.GetTopology(s) != MeshTopology.Triangles) continue;
                    var material = s < part.Materials.Length ? part.Materials[s] : part.Materials.LastOrDefault();
                    int slot = materials.IndexOf(material);
                    if (slot < 0) { slot = materials.Count; materials.Add(material); submeshIndices.Add(new List<int>()); }
                    var target = submeshIndices[slot];
                    foreach (int index in mesh.GetIndices(s)) target.Add(index + part.VertexOffset);
                }
            }

            var rest = new Mesh { indexFormat = vertexCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            rest.SetVertices(positions);
            rest.SetNormals(normals);
            rest.SetColors(colours);
            rest.SetUVs(0, uv0);
            rest.SetUVs(1, glow);
            rest.SetUVs(3, vertexIndex);
            rest.subMeshCount = submeshIndices.Count;
            for (int s = 0; s < submeshIndices.Count; s++) rest.SetIndices(submeshIndices[s], MeshTopology.Triangles, s, false);
            rest.bounds = bounds;
            rest.UploadMeshData(false); // keep it readable: the asset is saved from the CPU copy
            submeshMaterials = materials.ToArray();
            return rest;
        }

        /// <summary>One SoulHunter/HordeVat material per submesh, with the source material's colour and glow.</summary>
        private static Material[] BuildMaterials(string folder, string modelName, Material[] sources, Texture2D positions,
            Texture2D normals, int width, int rowsPerFrame)
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[HordeVatBaker] Shader {ShaderName} not found; {modelName} has no VAT materials yet. Bake again once it imports.");
                return new Material[0];
            }
            var result = new Material[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                string path = sources.Length == 1 ? $"{folder}/{modelName}_Vat.mat" : $"{folder}/{modelName}_Vat_{i}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
                material.shader = shader;
                var source = sources[i];
                // A merged horde material keeps white here and its colours in the vertices; a FacetedPreview material
                // has white vertices and its colour here. Either way the shader multiplies the two.
                material.SetColor("_Color", source != null && source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white);
                material.SetColor("_EmissionColor", source != null && source.HasProperty("_EmissionColor") ? source.GetColor("_EmissionColor") : Color.black);
                Texture baseMap = null;
                if (source != null && source.HasProperty("_BaseMap")) baseMap = source.GetTexture("_BaseMap");
                if (baseMap == null && source != null && source.HasProperty("_MainTex")) baseMap = source.GetTexture("_MainTex");
                material.SetTexture("_BaseMap", baseMap);
                material.SetTexture("_VatPositions", positions);
                material.SetTexture("_VatNormals", normals);
                material.SetFloat("_VatWidth", width);
                material.SetFloat("_VatRowsPerFrame", rowsPerFrame);
                material.enableInstancing = true; // RenderMeshInstanced refuses a material without it
                EditorUtility.SetDirty(material);
                result[i] = material;
            }
            return result;
        }

        /// <summary>
        /// Writes the texture, or refills the existing one in place so its GUID (and the materials' references) stays.
        /// Linear, no mips, point sampled: the shader loads exact texels and blends frames itself.
        /// </summary>
        private static Texture2D SaveTexture(string path, int width, int height, ushort[] data)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            bool created = texture == null;
            if (created) texture = new Texture2D(width, height, TextureFormat.RGBAHalf, false, true);
            else texture.Reinitialize(width, height, TextureFormat.RGBAHalf, false);
            texture.name = Path.GetFileNameWithoutExtension(path);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.anisoLevel = 0;
            texture.SetPixelData(data, 0);
            texture.Apply(false, false); // stays readable: the asset is written from the CPU copy
            if (created) AssetDatabase.CreateAsset(texture, path);
            else EditorUtility.SetDirty(texture);
            return texture;
        }

        /// <summary>Writes a new asset, or copies into the existing one so its GUID (and every reference to it) stays.</summary>
        private static T SaveOrReplace<T>(T created, string path) where T : Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null) { AssetDatabase.CreateAsset(created, path); return created; }
            EditorUtility.CopySerialized(created, existing);
            existing.name = Path.GetFileNameWithoutExtension(path);
            Object.DestroyImmediate(created);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static string PathUnder(Transform root, Transform t)
        {
            var names = new List<string>();
            for (var current = t; current != null && current != root; current = current.parent) names.Add(current.name);
            names.Reverse();
            return string.Join("/", names);
        }
    }
}
