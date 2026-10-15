using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.VFX;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Checks shared by the prop merge tests (PropMergeTests, WallCartMergeTests, PickupV2Tests). PropMeshMerger joins
    /// each group into ONE mesh drawn with ONE shared SoulHunter/PropMerged material: each original URP Lit part's
    /// linear base colour is baked into the vertex colour, its linear glow (only when the material's _EMISSION keyword
    /// is on) into UV1 and (metallic, smoothness, mask) into UV2. A PropMergedLook on the merged renderer lists the
    /// original materials (Sources). Textured or transparent materials (the GroundGlow quads) keep their own material.
    /// </summary>
    public static class MergedProps
    {
        public const string MergedShader = PropMergedLook.ShaderName;
        public const string LitShader = "Universal Render Pipeline/Lit";
        private const float Tolerance = 1e-4f;

        public static bool IsGlow(Renderer r) => r.name.StartsWith("GroundGlow");

        public static bool IsMerged(Renderer r) => r != null && r.GetComponent<PropMergedLook>() != null;

        /// <summary>The materials a renderer draws: its own, or (merged) the originals baked into its mesh.</summary>
        public static IEnumerable<Material> SourcesOf(Renderer r)
        {
            var look = r.GetComponent<PropMergedLook>();
            return (look != null ? look.Sources : r.sharedMaterials).Where(m => m != null);
        }

        /// <summary>Every material drawn under <paramref name="root"/> (merged sources included), ground glows left out.</summary>
        public static HashSet<Material> Drawn(Transform root) =>
            new HashSet<Material>(root.GetComponentsInChildren<MeshRenderer>(true).Where(r => !IsGlow(r)).SelectMany(SourcesOf));

        public static long Triangles(Mesh mesh)
        {
            long count = 0;
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
                if (mesh.GetTopology(sub) == MeshTopology.Triangles) count += mesh.GetIndexCount(sub) / 3;
            return count;
        }

        /// <summary>Triangles drawn under <paramref name="root"/>, ground glows left out.</summary>
        public static long Triangles(Transform root) =>
            root.GetComponentsInChildren<MeshRenderer>(true).Where(r => !IsGlow(r))
                .Select(r => r.GetComponent<MeshFilter>()).Where(f => f != null && f.sharedMesh != null)
                .Sum(f => Triangles(f.sharedMesh));

        /// <summary>The look PropMeshMerger bakes for one source material: colour, glow, (metallic, smoothness, mask).</summary>
        public static (Color colour, Vector3 glow, Vector4 surface) BakedLook(Material m, PropMergedLook look)
        {
            Color colour = m.GetColor("_BaseColor").linear;
            Color glow = m.IsKeywordEnabled("_EMISSION") ? m.GetColor("_EmissionColor") : Color.black;
            float mask = look != null && m == look.MaskA ? 1f : look != null && m == look.MaskB ? 2f : 0f;
            return (colour, new Vector3(glow.r, glow.g, glow.b), new Vector4(m.GetFloat("_Metallic"), m.GetFloat("_Smoothness"), mask, 0f));
        }

        private static bool Close(float a, float b) => Mathf.Abs(a - b) <= Tolerance;
        private static bool Close(Color a, Color b) => Close(a.r, b.r) && Close(a.g, b.g) && Close(a.b, b.b) && Close(a.a, b.a);
        private static bool Close(Vector3 a, Vector3 b) => Close(a.x, b.x) && Close(a.y, b.y) && Close(a.z, b.z);
        private static bool Close(Vector4 a, Vector4 b) => Close(a.x, b.x) && Close(a.y, b.y) && Close(a.z, b.z) && Close(a.w, b.w);

        /// <summary>
        /// A merged renderer draws one SoulHunter/PropMerged material, lists its URP Lit sources, and its saved mesh
        /// carries exactly their looks: every vertex has the colour, glow, metallic and smoothness of one source, and every
        /// source's look appears on some vertex.
        /// </summary>
        public static void AssertMergedSound(MeshRenderer r, string what)
        {
            var look = r.GetComponent<PropMergedLook>();
            Assert.IsNotNull(look, $"{what}/{r.name} has a PropMergedLook.");
            Assert.That(r.sharedMaterials.Length, Is.EqualTo(1), $"{what}/{r.name} draws one material.");
            Assert.IsNotNull(r.sharedMaterial, $"{what}/{r.name}: no missing material.");
            Assert.That(r.sharedMaterial.shader.name, Is.EqualTo(MergedShader), $"{what}/{r.name} uses the shared merged material.");
            Assert.That(look.Sources, Is.Not.Empty, $"{what}/{r.name} lists the materials baked into it.");
            foreach (var m in look.Sources)
            {
                Assert.IsNotNull(m, $"{what}/{r.name}: no missing source material.");
                Assert.That(m.shader.name, Is.EqualTo(LitShader), $"{what}/{r.name}: source {m.name} is URP Lit.");
            }

            var filter = r.GetComponent<MeshFilter>();
            Assert.That(filter != null && filter.sharedMesh != null, Is.True, $"{what}/{r.name} has its mesh.");
            var mesh = filter.sharedMesh;
            Assert.That(AssetDatabase.Contains(mesh), Is.True, $"{what}/{r.name}: mesh {mesh.name} is a saved asset.");
            Assert.That(mesh.isReadable, Is.True, $"{what}/{r.name}: {mesh.name} is readable.");
            Assert.That(mesh.subMeshCount, Is.EqualTo(1), $"{what}/{r.name}: one submesh for the one material.");

            int n = mesh.vertexCount;
            var colours = new List<Color>();
            var glows = new List<Vector3>();
            var surfaces = new List<Vector4>();
            mesh.GetColors(colours);
            mesh.GetUVs(1, glows);
            mesh.GetUVs(2, surfaces);
            Assert.That(colours.Count, Is.EqualTo(n), $"{mesh.name}: a colour per vertex.");
            Assert.That(glows.Count, Is.EqualTo(n), $"{mesh.name}: a glow (UV1) per vertex.");
            Assert.That(surfaces.Count, Is.EqualTo(n), $"{mesh.name}: metallic and smoothness (UV2) per vertex.");

            var looks = look.Sources.Select(m => BakedLook(m, look)).ToArray();
            var seen = new bool[looks.Length];
            for (int v = 0; v < n; v++)
            {
                bool any = false;
                for (int s = 0; s < looks.Length; s++)
                    if (Close(colours[v], looks[s].colour) && Close(glows[v], looks[s].glow) && Close(surfaces[v], looks[s].surface)) { seen[s] = true; any = true; }
                if (!any)
                    Assert.Fail($"{mesh.name} vertex {v}: colour {colours[v]}, glow {glows[v]}, surface {surfaces[v]} matches none of its sources "
                        + string.Join(", ", look.Sources.Select(m => m.name)) + ".");
            }
            for (int s = 0; s < looks.Length; s++)
                Assert.That(seen[s], Is.True, $"{mesh.name}: source {look.Sources[s].name} is baked into some vertex.");
        }

        /// <summary>Every renderer still draws: merged ones as above; unmerged ones one material per submesh, its mesh saved.</summary>
        public static void AssertRenderersSound(Transform group, string what)
        {
            foreach (var r in group.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (IsMerged(r)) { AssertMergedSound(r, what); continue; }
                var filter = r.GetComponent<MeshFilter>();
                Assert.That(filter != null && filter.sharedMesh != null, Is.True, $"{what}/{r.name} has its mesh.");
                Assert.That(r.sharedMaterials.Length, Is.EqualTo(filter.sharedMesh.subMeshCount), $"{what}/{r.name}: one material per submesh.");
                Assert.That(r.sharedMaterials.All(m => m != null), Is.True, $"{what}/{r.name}: no missing material.");
                Assert.That(AssetDatabase.Contains(filter.sharedMesh), Is.True,
                    $"{what}/{r.name}: mesh {filter.sharedMesh.name} is a saved asset (a scene-only mesh is lost from the prefab).");
            }
        }

        /// <summary>The non-glow renderers of a group: exactly <paramref name="expected"/>, every one merged.</summary>
        public static MeshRenderer[] AssertMergedCount(Transform group, int expected, string what)
        {
            var renderers = group.GetComponentsInChildren<MeshRenderer>(true).Where(r => !IsGlow(r)).ToArray();
            Assert.That(renderers.Length, Is.EqualTo(expected),
                $"{what}: {renderers.Length} renderers ({string.Join(", ", renderers.Select(r => r.name))}), expected {expected}: run Soul Hunter/Content/Merge Prop Meshes.");
            foreach (var r in renderers) Assert.That(IsMerged(r), Is.True, $"{what}/{r.name} is the merged renderer.");
            return renderers;
        }
    }

    /// <summary>
    /// Draw calls (owner, 2026-10-15: "SRP batcher or call drop kam se kam karna hai", then "sab ko single mesh or single
    /// material mein convert karo"): PropMeshMerger joins each group of a prefab into one mesh with one shared
    /// SoulHunter/PropMerged material. These tests read the prefabs as saved after the merge: the Soul Lantern (group
    /// "Visual"), the Soul Gem and the Soul Coin (each TieredVisual tier). The look must be unchanged (every model
    /// material still baked in, its values in the mesh, every triangle kept, shadows kept), the names other code looks up
    /// must stay (the lantern's GroundGlow on the root, each gem tier's GroundGlow, the tiers themselves), and running the
    /// merge again must change nothing.
    /// The paths are LightSourceImporter.PrefabPath / .Model and PickupV2Importer.GemPrefab / .CoinPrefab / ModelFolder
    /// (the editor assembly is not referenced by the tests; PropMeshMerger is reached by reflection for the re-run test).
    /// </summary>
    public class PropMergeTests
    {
        private const string LanternPrefab = "Assets/Prefabs/Props/SH10_SoulLantern.prefab";
        private const string LanternModel = "Assets/Art/Models/SH10_Prop_SoulLantern.fbx";
        private const string GemPrefab = "Assets/Prefabs/Items/XP_Gem.prefab";
        private const string CoinPrefab = "Assets/Prefabs/Items/SH10_Gold_Coin.prefab";
        private static readonly string[] GemModels =
        {
            "Assets/Art/Models/SH10_Pickup_SoulGem_T1.fbx", "Assets/Art/Models/SH10_Pickup_SoulGem_T2.fbx", "Assets/Art/Models/SH10_Pickup_SoulGem_T3.fbx",
        };
        // Owner (2026-10-13): every coin tier is the gold coin; its worth shows by its size (PickupV2Importer.BuildCoin).
        private static readonly string[] CoinModels =
        {
            "Assets/Art/Models/SH10_Pickup_Coin_Gold.fbx", "Assets/Art/Models/SH10_Pickup_Coin_Gold.fbx", "Assets/Art/Models/SH10_Pickup_Coin_Gold.fbx",
        };

        private static bool IsGlow(Renderer r) => MergedProps.IsGlow(r);

        private static GameObject Load(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, path);
            return prefab;
        }

        private static HashSet<Material> ModelMaterials(Transform root) =>
            new HashSet<Material>(root.GetComponentsInChildren<MeshRenderer>(true).Where(r => !IsGlow(r))
                .SelectMany(r => r.sharedMaterials).Where(m => m != null));

        // ---------------------------------------------------------------- Soul Lantern

        [Test]
        public void TheLantern_IsOneMergedRenderer_AndStillDrawsEveryModelMaterial()
        {
            var visual = Load(LanternPrefab).transform.Find("Visual");
            Assert.IsNotNull(visual, "The lantern keeps its Visual group.");
            var modelMaterials = ModelMaterials(Load(LanternModel).transform);
            Assert.That(modelMaterials.Count, Is.GreaterThan(0));

            Assert.That(visual.GetComponentsInChildren<MeshRenderer>(true).Count(IsGlow), Is.EqualTo(0), "The lantern's glow is on the root, not in Visual.");
            MergedProps.AssertMergedCount(visual, 1, "Lantern Visual");
            var drawn = MergedProps.Drawn(visual);
            foreach (var m in modelMaterials) Assert.That(drawn.Contains(m), Is.True, $"The lantern still draws {m.name}.");
            Assert.That(drawn.Except(modelMaterials).Select(m => m.name), Is.Empty, "No material the model does not use.");
            MergedProps.AssertRenderersSound(visual, "Lantern");
        }

        [Test]
        public void TheLantern_KeepsEveryTriangle_ItsShadows_AndItsGroundGlow()
        {
            var prefab = Load(LanternPrefab);
            var visual = prefab.transform.Find("Visual");
            Assert.IsNotNull(visual);
            long expected = MergedProps.Triangles(Load(LanternModel).transform);
            Assert.That(expected, Is.GreaterThan(0));
            Assert.That(MergedProps.Triangles(visual), Is.EqualTo(expected), "Merging joins meshes; it never adds or drops a triangle.");

            foreach (var r in visual.GetComponentsInChildren<MeshRenderer>(true))
            {
                Assert.That(r.shadowCastingMode, Is.EqualTo(ShadowCastingMode.On), $"{r.name} still casts its shadow (LightSourceImporter).");
                Assert.That(r.enabled && r.gameObject.activeSelf, Is.True, $"{r.name} is drawn.");
            }

            // RelicPickup, SoulEggPickup, StageItems and WeaponCachePickup copy SoulLantern.transform.Find("GroundGlow").
            var glow = prefab.transform.Find("GroundGlow");
            Assert.IsNotNull(glow, "GroundGlow stays a direct child of the lantern root.");
            var glowRenderer = glow.GetComponent<MeshRenderer>();
            Assert.IsNotNull(glowRenderer);
            Assert.That(glowRenderer.sharedMaterial, Is.Not.Null);
            Assert.That(glowRenderer.sharedMaterial.shader.name, Is.Not.EqualTo(MergedProps.MergedShader), "The textured glow keeps its own material.");
            Assert.That(MergedProps.IsMerged(glowRenderer), Is.False);
            Assert.That(glowRenderer.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off), "The glow casts no shadow.");
        }

        // ---------------------------------------------------------------- Soul Gem and Soul Coin

        private static IEnumerable<TestCaseData> Tiered()
        {
            yield return new TestCaseData(GemPrefab, GemModels, true).SetName("Gem");
            yield return new TestCaseData(CoinPrefab, CoinModels, false).SetName("Coin");
        }

        [TestCaseSource(nameof(Tiered))]
        public void EachTier_StaysReferenced_AndIsOneMergedRenderer(string path, string[] models, bool hasGlow)
        {
            var prefab = Load(path);
            var tiered = prefab.GetComponent<TieredVisual>();
            Assert.IsNotNull(tiered, path);
            Assert.That(tiered.Tiers.Length, Is.EqualTo(models.Length), "Every tier is still listed.");
            var all = prefab.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < tiered.Tiers.Length; i++)
            {
                var tier = tiered.Tiers[i];
                Assert.IsNotNull(tier, $"{path} tier {i} is still referenced.");
                Assert.That(all.Contains(tier.transform), Is.True, $"{path} tier {i} is part of the prefab.");
                Assert.That(tier.activeSelf, Is.EqualTo(i == 0), "Only the lowest tier shows until a value is set.");

                var modelMaterials = ModelMaterials(Load(models[i]).transform);
                MergedProps.AssertMergedCount(tier.transform, 1, tier.name);
                var drawn = MergedProps.Drawn(tier.transform);
                foreach (var m in modelMaterials) Assert.That(drawn.Contains(m), Is.True, $"{tier.name} still draws {m.name}.");
                Assert.That(drawn.Except(modelMaterials).Select(m => m.name), Is.Empty, $"{tier.name}: no material its model does not use.");
                MergedProps.AssertRenderersSound(tier.transform, tier.name);

                var glow = tier.GetComponentsInChildren<MeshRenderer>(true).Where(IsGlow).ToArray();
                if (hasGlow)
                {
                    Assert.That(glow.Length, Is.EqualTo(1), $"{tier.name} keeps its own GroundGlow.");
                    Assert.That(glow[0].shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
                    Assert.That(MergedProps.IsMerged(glow[0]), Is.False, $"{tier.name}: the textured glow keeps its own material.");
                    Assert.That(glow[0].sharedMaterial.shader.name, Is.Not.EqualTo(MergedProps.MergedShader));
                }
                else Assert.That(glow, Is.Empty, $"{tier.name}: no flat disc under the coin (owner, 2026-10-13).");
                Assert.That(tier.GetComponentsInChildren<MeshRenderer>(true).Length, Is.EqualTo(1 + (hasGlow ? 1 : 0)),
                    $"{tier.name}: one baked renderer{(hasGlow ? " + its GroundGlow" : "")}.");
            }
        }

        [TestCaseSource(nameof(Tiered))]
        public void EachTier_KeepsEveryTriangle_AndItsShadows(string path, string[] models, bool hasGlow)
        {
            var tiered = Load(path).GetComponent<TieredVisual>();
            Assert.IsNotNull(tiered, path);
            for (int i = 0; i < tiered.Tiers.Length; i++)
            {
                var tier = tiered.Tiers[i];
                long expected = MergedProps.Triangles(Load(models[i]).transform);
                Assert.That(expected, Is.GreaterThan(0), models[i]);
                Assert.That(MergedProps.Triangles(tier.transform), Is.EqualTo(expected), $"{tier.name}: merging never adds or drops a triangle.");
                foreach (var r in tier.GetComponentsInChildren<MeshRenderer>(true).Where(r => !IsGlow(r)))
                {
                    Assert.That(r.shadowCastingMode, Is.EqualTo(ShadowCastingMode.On), $"{tier.name}/{r.name} still casts its shadow (PickupV2Importer.Model).");
                    Assert.That(r.enabled && r.gameObject.activeSelf, Is.True, $"{tier.name}/{r.name} is drawn when its tier shows.");
                }
            }
        }

        // ---------------------------------------------------------------- Running it again

        /// <summary>
        /// PropMeshMerger.Merge saves the prefab only when a group changed, so on a merged prefab it writes nothing (the one
        /// merged renderer per group has a PropMerged material, which is not bakeable, and is alone, so nothing joins). The
        /// test runs it only once the prefab already looks merged (the checks above), so it never merges an unmerged
        /// prefab by itself; the prefab file must be byte for byte the same afterwards.
        /// </summary>
        [TestCase(LanternPrefab)]
        [TestCase(GemPrefab)]
        [TestCase(CoinPrefab)]
        public void MergingAgain_ChangesNothing(string path)
        {
            if (!LooksMerged(path)) Assert.Ignore($"{path} is not merged yet (run Soul Hunter/Content/Merge Prop Meshes); not merging it from a test.");

            var mergerType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("SoulHunter.Editor.Content.PropMeshMerger", false)).FirstOrDefault(t => t != null);
            Assert.IsNotNull(mergerType, "SoulHunter.Editor.Content.PropMeshMerger is compiled in the editor assembly.");
            var targets = (IEnumerable)mergerType.GetField("Targets", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            object target = null;
            foreach (var t in targets)
                if ((string)t.GetType().GetField("PrefabPath").GetValue(t) == path) { target = t; break; }
            Assert.IsNotNull(target, $"PropMeshMerger.Targets lists {path}.");

            string file = Path.Combine(Path.GetDirectoryName(Application.dataPath), path);
            byte[] before = File.ReadAllBytes(file);
            int renderersBefore = Load(path).GetComponentsInChildren<MeshRenderer>(true).Length;

            var merge = mergerType.GetMethod("Merge", BindingFlags.Public | BindingFlags.Static);
            var result = merge.Invoke(null, new[] { target });
            var fields = result.GetType();
            int b = (int)fields.GetField("Item1").GetValue(result), a = (int)fields.GetField("Item2").GetValue(result);

            Assert.That(b, Is.EqualTo(renderersBefore), "Merge counts the prefab's renderers as saved.");
            Assert.That(a, Is.EqualTo(b), "Nothing left to merge: the renderer count stays the same.");
            Assert.That(File.ReadAllBytes(file), Is.EqualTo(before), $"{path} is not rewritten when nothing merges.");
            Assert.That(Load(path).GetComponentsInChildren<MeshRenderer>(true).Length, Is.EqualTo(renderersBefore));
        }

        /// <summary>The one-merged-renderer-per-group condition of the tests above, without asserting.</summary>
        private static bool LooksMerged(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return false;
            bool OneMerged(Transform group)
            {
                var drawn = group.GetComponentsInChildren<MeshRenderer>(true).Where(r => !IsGlow(r)).ToArray();
                return drawn.Length == 1 && MergedProps.IsMerged(drawn[0]);
            }
            if (path == LanternPrefab)
            {
                var visual = prefab.transform.Find("Visual");
                return visual != null && OneMerged(visual);
            }
            var tiered = prefab.GetComponent<TieredVisual>();
            var models = path == GemPrefab ? GemModels : CoinModels;
            if (tiered == null || tiered.Tiers.Length != models.Length) return false;
            return tiered.Tiers.All(tier => tier != null && OneMerged(tier.transform));
        }
    }
}
