using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.VFX;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SoulHunter.Editor.Content
{
    /// <summary>
    /// Draw calls (owner, 2026-10-15: "SRP batcher or call drop kam se kam karna hai"): the render census of Level 5
    /// (run-20261015-drawnow) found the props and pickups built from many small meshes, one renderer each: a Soul Lantern
    /// 29 renderers, a Soul Gem 5, a chest about 30. Every renderer is a draw call (and a shadow-caster draw when it casts).
    /// This joins, inside one group of a prefab, every mesh that uses the same material with the same shadow settings
    /// into one mesh, so a lantern draws once per material. The look is unchanged: same meshes, same materials, same
    /// places. Names a script looks up (the chest's "Lid" shell and "LockPlate", any "GroundGlow") are left alone, and a
    /// caller can split a group further (the chest keeps its lid parts apart from its body parts, so the lid still opens).
    /// Running it again changes nothing. Menu: Soul Hunter/Content/Merge Prop Meshes, or
    /// -executeMethod SoulHunter.Editor.Content.PropMeshMerger.MergeAll -quit
    /// </summary>
    public static class PropMeshMerger
    {
        public const string MeshFolder = "Assets/Art/Models/PropMerged";
        public const string MergedPrefix = "Merged_";

        /// <summary>One prefab to merge: which child objects are groups, what to keep, and an optional extra split.</summary>
        public sealed class Target
        {
            public string PrefabPath;
            /// <summary>The group roots inside the opened prefab; each is merged on its own.</summary>
            public Func<GameObject, IEnumerable<Transform>> Groups;
            /// <summary>True for a renderer that must stay as it is (a script finds it by name or moves it).</summary>
            public Func<Renderer, bool> Keep = _ => false;
            /// <summary>An extra key: renderers with different keys are never joined (for example lid against body).</summary>
            public Func<Renderer, Transform, string> Split = (_, __) => "";
            /// <summary>True (default): every bakeable part (URP Lit, opaque, no textures) is drawn with the one shared
            /// SoulHunter/PropMerged material, its colour, metallic, smoothness and glow baked into the mesh.</summary>
            public bool OneMaterial = true;
        }

        public const string SharedMaterialPath = "Assets/Art/Materials/PropMerged/SH10_PropMerged.mat";
        private const string LitShader = "Universal Render Pipeline/Lit";
        private const string BakedKey = "baked";

        /// <summary>Parts a script recolours by their material's name: 1 = mask A, 2 = mask B (see PropMergedLook).</summary>
        public static int MaskFor(Material m)
        {
            if (m == null) return 0;
            if (m.name.Contains("Chest_Iron") || m.name.Contains("Rune")) return 1;
            if (m.name.Contains("Chest_Soul")) return 2;
            return 0;
        }

        /// <summary>A URP Lit material whose look is only numbers: opaque, metallic workflow, no textures, default
        /// highlights, reflections and culling. Anything else keeps its own material.</summary>
        public static bool Bakeable(Material m)
        {
            if (m == null || m.shader == null || m.shader.name != LitShader) return false;
            float F(string p, float fallback) => m.HasProperty(p) ? m.GetFloat(p) : fallback;
            if (F("_Surface", 0f) != 0f || F("_AlphaClip", 0f) != 0f || F("_WorkflowMode", 1f) != 1f) return false;
            if (F("_SpecularHighlights", 1f) != 1f || F("_EnvironmentReflections", 1f) != 1f || F("_Cull", 2f) != 2f) return false;
            foreach (string tex in new[] { "_BaseMap", "_MainTex", "_EmissionMap", "_MetallicGlossMap", "_BumpMap", "_OcclusionMap", "_ParallaxMap", "_DetailAlbedoMap", "_SpecGlossMap" })
                if (m.HasProperty(tex) && m.GetTexture(tex) != null) return false;
            return true;
        }

        public static Material SharedMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(SharedMaterialPath);
            if (material != null) return material;
            var shader = Shader.Find(PropMergedLook.ShaderName);
            if (shader == null) throw new FileNotFoundException("Missing shader " + PropMergedLook.ShaderName);
            Directory.CreateDirectory(Path.GetDirectoryName(SharedMaterialPath));
            AssetDatabase.Refresh();
            material = new Material(shader) { name = "SH10_PropMerged" };
            AssetDatabase.CreateAsset(material, SharedMaterialPath);
            return material;
        }

        public static bool KeepByName(Renderer r) => r.name.StartsWith("GroundGlow");

        /// <summary>The prefabs this tool merges. Other files (chest, plates) add theirs to this list.</summary>
        public static readonly List<Target> Targets = new List<Target>
        {
            new Target
            {
                PrefabPath = LightSourceImporter.PrefabPath,
                Groups = root => One(root.transform.Find("Visual")),
                Keep = KeepByName,
            },
            new Target { PrefabPath = PickupV2Importer.GemPrefab, Groups = TierRoots, Keep = KeepByName },
            new Target { PrefabPath = PickupV2Importer.CoinPrefab, Groups = TierRoots, Keep = KeepByName },
            ChestMergeTarget.Target,  // ChestMergeTarget.cs: the lid and lock stay apart so the chest still opens
            PlateMergeTarget.Target,  // PlateMergeTarget.cs: the Rune Plate
            // The Ruined Castle corridor wall: 136 parts in 3 materials, 14 modules on screen at once (Level 4).
            new Target { PrefabPath = LightSourceImporter.WallPrefabPath, Groups = root => One(root.transform.Find("Visual")), Keep = KeepByName },
            // The Level 3 rail cart: the rails stay put and the cart rides along them, so each is its own group.
            new Target
            {
                PrefabPath = StageHazardBuilder.RailCartPath,
                Groups = root => new[] { root.transform.Find("Rails"), root.transform.Find("Cart/Visual") }.Where(t => t != null),
                Keep = KeepByName,
            },
            // Owner, 2026-10-15: "sabhi ko single mesh or single material banao". The Wandering Merchant: one mesh with 14
            // URP Lit colours (Level 3). The four light-source pickups: 3-6 primitives each.
            new Target
            {
                PrefabPath = MerchantModelImporter.PrefabPath,
                Groups = root => One(root.transform.Find("SH10_NPC_WanderingMerchant")), // the model keeps its own object
                Keep = KeepByName,
            },
            new Target { PrefabPath = ExtraPickupBuilder.GildedCharmPath, Groups = VisualOrRoot, Keep = KeepByName },
            new Target { PrefabPath = ExtraPickupBuilder.LuckCharmPath, Groups = VisualOrRoot, Keep = KeepByName },
            new Target { PrefabPath = ExtraPickupBuilder.FireBreathPath, Groups = VisualOrRoot, Keep = KeepByName },
            new Target { PrefabPath = ExtraPickupBuilder.ScreenWipePath, Groups = VisualOrRoot, Keep = KeepByName },
        };

        private static IEnumerable<Transform> VisualOrRoot(GameObject root) => One(root.transform.Find("Visual") ?? root.transform);

        private static IEnumerable<Transform> One(Transform t) { if (t != null) yield return t; }

        /// <summary>Each tier of a TieredVisual (gems, coins) is its own group, so switching tiers still works.</summary>
        public static IEnumerable<Transform> TierRoots(GameObject root)
        {
            foreach (var tiered in root.GetComponentsInChildren<TieredVisual>(true))
                foreach (var tier in tiered.Tiers)
                    if (tier != null) yield return tier.transform;
        }

        [MenuItem("Soul Hunter/Content/Merge Prop Meshes")]
        public static void MergeAll()
        {
            Directory.CreateDirectory(MeshFolder);
            AssetDatabase.Refresh();
            int before = 0, after = 0;
            foreach (var target in Targets)
            {
                var (b, a) = Merge(target);
                before += b; after += a;
                Debug.Log($"[PropMeshMerger] {target.PrefabPath}: {b} renderers -> {a}");
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[PropMeshMerger] Total {before} renderers -> {after}.");
        }

        /// <summary>Merges one prefab; returns its mesh renderers before and after.</summary>
        public static (int before, int after) Merge(Target target)
        {
            if (!AssetDatabase.IsValidFolder(MeshFolder)) { Directory.CreateDirectory(MeshFolder); AssetDatabase.Refresh(); }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(target.PrefabPath) == null)
            {
                Debug.LogWarning("[PropMeshMerger] Missing " + target.PrefabPath);
                return (0, 0);
            }
            var root = PrefabUtility.LoadPrefabContents(target.PrefabPath);
            try
            {
                int before = root.GetComponentsInChildren<MeshRenderer>(true).Length;
                // Nested model prefabs (the FBX under "Visual") must be unpacked before their parts can be removed.
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t != null && PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject))
                        PrefabUtility.UnpackPrefabInstance(t.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

                bool changed = false;
                string owner = Path.GetFileNameWithoutExtension(target.PrefabPath);
                foreach (var group in new List<Transform>(target.Groups(root)))
                    changed |= MergeGroup(group, target, owner);
                int after = root.GetComponentsInChildren<MeshRenderer>(true).Length;
                if (changed) PrefabUtility.SaveAsPrefabAsset(root, target.PrefabPath);
                return (before, after);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private sealed class Part
        {
            public Material Material;
            public ShadowCastingMode Shadows;
            public bool Receive;
            public int Layer;
            public MeshRenderer Template; // the first source: probes, rendering layers, motion vectors... are copied from it
            public string Name;
            public readonly List<CombineInstance> Pieces = new List<CombineInstance>();
            public bool Baked;
            public readonly List<Material> PieceMaterials = new List<Material>(); // per piece, baked parts only
        }

        private static string KeyOf(Material m, MeshRenderer r, string split, bool oneMaterial) =>
            $"{(oneMaterial && Bakeable(m) ? BakedKey : m.GetInstanceID().ToString())}|{r.shadowCastingMode}|{r.receiveShadows}|{r.gameObject.layer}|{r.lightProbeUsage}|{r.reflectionProbeUsage}|{r.renderingLayerMask}|{r.motionVectorGenerationMode}|{r.staticShadowCaster}|{r.gameObject.tag}|{split}";

        /// <summary>False for a part hidden in the prefab (a disabled renderer or an inactive object under the group),
        /// which must not become visible, and for a mirrored part (CombineMeshes would keep its flipped winding).</summary>
        private static bool Mergeable(MeshRenderer renderer, Transform group)
        {
            if (!renderer.enabled) return false;
            for (var t = renderer.transform; t != null && t != group; t = t.parent)
                if (!t.gameObject.activeSelf) return false;
            return (group.worldToLocalMatrix * renderer.transform.localToWorldMatrix).determinant > 0f;
        }

        private static bool MergeGroup(Transform group, Target target, string owner)
        {
            // Every candidate renderer with the part key of each of its submeshes.
            var candidates = new Dictionary<MeshRenderer, List<(int sub, string key)>>();
            foreach (var renderer in group.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (target.Keep(renderer) || !Mergeable(renderer, group)) continue;
                if (!renderer.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null) continue;
                var materials = renderer.sharedMaterials;
                if (materials.Length != filter.sharedMesh.subMeshCount) continue; // extra or missing materials: leave it
                string split = target.Split(renderer, group) ?? "";
                var keys = new List<(int, string)>();
                bool ok = true;
                for (int sub = 0; sub < materials.Length; sub++)
                {
                    if (materials[sub] == null) { ok = false; break; }
                    keys.Add((sub, KeyOf(materials[sub], renderer, split, target.OneMaterial)));
                }
                if (ok) candidates.Add(renderer, keys);
            }
            // A part is built only when at least two pieces go into it, and a renderer goes only when every one of its
            // submeshes goes into a built part (else some of it would draw twice). Drop renderers until both hold.
            while (true)
            {
                var count = new Dictionary<string, int>();
                foreach (var keys in candidates.Values)
                    foreach (var (_, key) in keys) count[key] = count.TryGetValue(key, out int c) ? c + 1 : 1;
                var drop = new List<MeshRenderer>();
                foreach (var pair in candidates)
                    if (pair.Value.Exists(k => count[k.key] < 2)) drop.Add(pair.Key);
                if (drop.Count == 0) break;
                foreach (var r in drop) candidates.Remove(r);
            }
            if (candidates.Count == 0) return false;

            var parts = new Dictionary<string, Part>();
            var order = new List<string>();
            foreach (var pair in candidates)
            {
                var renderer = pair.Key;
                var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                foreach (var (sub, key) in pair.Value)
                {
                    if (!parts.TryGetValue(key, out var part))
                    {
                        var mat = renderer.sharedMaterials[sub];
                        string split = target.Split(renderer, group) ?? "";
                        bool baked = key.StartsWith(BakedKey + "|");
                        part = new Part
                        {
                            Baked = baked,
                            Material = baked ? SharedMaterial() : mat, Shadows = renderer.shadowCastingMode, Receive = renderer.receiveShadows,
                            Layer = renderer.gameObject.layer, Template = renderer,
                            // A stable name (no running index), so a later run overwrites the same asset.
                            Name = $"{owner}_{group.name}_{(baked ? "Baked" : mat.name)}_{renderer.shadowCastingMode}{(split.Length > 0 ? "_" + split : "")}",
                        };
                        parts.Add(key, part);
                        order.Add(key);
                    }
                    part.Pieces.Add(new CombineInstance
                    {
                        mesh = mesh,
                        subMeshIndex = sub,
                        transform = group.worldToLocalMatrix * renderer.transform.localToWorldMatrix,
                    });
                    if (part.Baked) part.PieceMaterials.Add(renderer.sharedMaterials[sub]);
                }
            }

            var used = new HashSet<string>();
            foreach (string key in order)
            {
                var part = parts[key];
                string name = part.Name;
                for (int n = 2; !used.Add(name); n++) name = part.Name + "_" + n;
                Mesh mesh;
                if (part.Baked) mesh = BuildBaked(part, name);
                else
                {
                    mesh = new Mesh { name = name };
                    int vertices = 0;
                    foreach (var p in part.Pieces) vertices += p.mesh.vertexCount;
                    mesh.indexFormat = vertices > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
                    mesh.CombineMeshes(part.Pieces.ToArray(), true, true, false);
                    mesh.RecalculateBounds();
                }
                string assetPath = $"{MeshFolder}/{Safe(mesh.name)}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
                if (existing != null) { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing; }
                else AssetDatabase.CreateAsset(mesh, assetPath);

                var go = new GameObject(MergedPrefix + (part.Baked ? "Prop" : part.Material.name)) { layer = part.Layer, tag = part.Template.gameObject.tag };
                GameObjectUtility.SetStaticEditorFlags(go, GameObjectUtility.GetStaticEditorFlags(part.Template.gameObject));
                go.transform.SetParent(group, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var merged = go.AddComponent<MeshRenderer>();
                EditorUtility.CopySerialized(part.Template, merged); // probes, rendering layers, motion vectors, sorting
                merged.sharedMaterials = new[] { part.Material };
                merged.shadowCastingMode = part.Shadows;
                merged.receiveShadows = part.Receive;
                if (part.Baked)
                {
                    var look = go.AddComponent<PropMergedLook>();
                    look.Sources = part.PieceMaterials.Distinct().ToArray();
                    look.MaskA = look.Sources.FirstOrDefault(m => MaskFor(m) == 1);
                    look.MaskB = look.Sources.FirstOrDefault(m => MaskFor(m) == 2);
                    foreach (var m in look.Sources)
                        if (MaskFor(m) != 0 && m != look.MaskA && m != look.MaskB)
                            Debug.LogWarning($"[PropMeshMerger] {name}: a second mask material {m.name}; it is recoloured with the first.");
                }
            }

            foreach (var source in candidates.Keys)
            {
                var go = source.gameObject;
                UnityEngine.Object.DestroyImmediate(source.GetComponent<MeshFilter>());
                UnityEngine.Object.DestroyImmediate(source);
                RemoveIfEmpty(go, group);
            }
            return true;
        }

        /// <summary>
        /// One mesh for a baked part: every piece's triangles in the group's space, with its material's look in the
        /// vertices: colour = linear base colour, UV1 = linear emission (when the material's emission is on), UV2 =
        /// (metallic, smoothness, mask, 0). Only the vertices a piece's submesh uses are copied.
        /// </summary>
        private static Mesh BuildBaked(Part part, string name)
        {
            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var colours = new List<Color>();
            var emission = new List<Vector3>();
            var surface = new List<Vector4>();
            var triangles = new List<int>();
            var srcPos = new List<Vector3>();
            var srcNrm = new List<Vector3>();
            for (int i = 0; i < part.Pieces.Count; i++)
            {
                var piece = part.Pieces[i];
                var material = part.PieceMaterials[i];
                Color baseColour = material.GetColor("_BaseColor").linear;
                // An [HDR] colour (URP Lit _EmissionColor) is kept linear by Unity and reaches the shader as it is; only
                // _BaseColor is converted from gamma (checked on spheres, run-20261015-onemat/diag3).
                Color glow = material.IsKeywordEnabled("_EMISSION") ? material.GetColor("_EmissionColor") : Color.black;
                var look = new Vector4(material.GetFloat("_Metallic"), material.GetFloat("_Smoothness"), MaskFor(material), 0f);
                piece.mesh.GetVertices(srcPos);
                piece.mesh.GetNormals(srcNrm);
                if (srcNrm.Count != srcPos.Count) throw new InvalidDataException($"{piece.mesh.name} has no normals.");
                var normalMatrix = piece.transform.inverse.transpose;
                var remap = new Dictionary<int, int>();
                foreach (int index in piece.mesh.GetTriangles(piece.subMeshIndex))
                {
                    if (!remap.TryGetValue(index, out int to))
                    {
                        to = positions.Count;
                        remap.Add(index, to);
                        positions.Add(piece.transform.MultiplyPoint3x4(srcPos[index]));
                        normals.Add(normalMatrix.MultiplyVector(srcNrm[index]).normalized);
                        colours.Add(baseColour);
                        emission.Add(new Vector3(glow.r, glow.g, glow.b));
                        surface.Add(look);
                    }
                    triangles.Add(to);
                }
            }
            var mesh = new Mesh { name = name, indexFormat = positions.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetColors(colours);
            mesh.SetUVs(1, emission);
            mesh.SetUVs(2, surface);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Removes an object left with only its Transform and no children, then its parents in turn (not the group).</summary>
        private static void RemoveIfEmpty(GameObject go, Transform group)
        {
            while (go != null && go.transform != group && go.transform.childCount == 0 && go.GetComponents<Component>().Length == 1)
            {
                var parent = go.transform.parent;
                UnityEngine.Object.DestroyImmediate(go);
                go = parent != null ? parent.gameObject : null;
            }
        }

        private static string Safe(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name.Replace(' ', '_');
        }
    }
}
