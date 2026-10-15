using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SoulHunter.Gameplay.VFX;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Draw calls (owner, 2026-10-15): PropMeshMerger joins the chest's 43 parts into one mesh per side of the lid seam
    /// (ChestMergeTarget), drawn with the one shared SoulHunter/PropMerged material: each part's look is baked into the
    /// mesh, and PropMergedLook lists the original materials it holds (Sources) and its recolourable parts (mask A iron,
    /// mask B soul). These check the chest prefab as saved: the parts scripts look up by name are still there, it draws at
    /// most 7 times, nothing of the approved model is lost (every material, every triangle), the recoloured parts are
    /// marked, and the opening screen's hinge rule (ChestPreviewStage.BuildHinge: parts whose bounds centre is above the
    /// "Lid" shell's lowest point) takes exactly as many triangles as it took from the unmerged model.
    /// </summary>
    public class ChestMergeTests
    {
        private const string ChestPrefab = "Assets/Prefabs/Items/Chest.prefab";      // PickupV2Importer.ChestPrefab
        private const string ChestModel = "Assets/Art/Models/SH10_Pickup_Chest.fbx"; // PickupV2Importer.BuildChest's model
        private const int MaxRenderers = 8; // 6 kept (Lid, 2 LidBands, LockPlate, Cranium, and SoulSeam, which sits on the lid seam) + the merged lid + the merged body
        private const float SeamMargin = 0.01f; // ChestMergeTarget.SeamMargin

        private static Transform Visual()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChestPrefab);
            Assert.That(prefab, Is.Not.Null, ChestPrefab);
            var visual = prefab.transform.Find("Visual");
            Assert.That(visual, Is.Not.Null, "The chest keeps its Visual pivot (ChestPickup, SoulCasket, ChestPreviewStage).");
            return visual;
        }

        private static GameObject Model()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ChestModel);
            Assert.That(model, Is.Not.Null, ChestModel);
            return model;
        }

        /// <summary>A merged mesh drawn with the shared PropMerged material (it carries a PropMergedLook).</summary>
        private static bool Baked(MeshRenderer r) => r.TryGetComponent(out PropMergedLook _);

        /// <summary>The original materials a renderer stands for: its Sources when merged, else its own materials.</summary>
        private static IEnumerable<Material> Represented(MeshRenderer r) =>
            r.TryGetComponent(out PropMergedLook look) ? look.Sources : r.sharedMaterials;

        private static List<Material> BakedSources(IEnumerable<MeshRenderer> renderers) =>
            renderers.Where(Baked).SelectMany(r => r.GetComponent<PropMergedLook>().Sources).Where(m => m != null).Distinct().ToList();

        /// <summary>PropMeshMerger.MaskFor (editor code, not reachable from this assembly).</summary>
        private static int MaskFor(Material m) =>
            m == null ? 0 : m.name.Contains("Chest_Iron") || m.name.Contains("Rune") ? 1 : m.name.Contains("Chest_Soul") ? 2 : 0;

        [Test]
        public void TheChest_KeepsThePartsScriptsLookUpByName()
        {
            var names = Visual().GetComponentsInChildren<MeshRenderer>(true).Select(r => r.name).ToList();
            string all = string.Join(", ", names);
            Assert.That(names.Contains("Lid"), "The lid shell (the preview's seam and up axis). Parts: " + all);
            Assert.That(names.Count(n => n.StartsWith("LidBand")), Is.EqualTo(2), "Both lid rings (the preview's up axis). Parts: " + all);
            Assert.That(names.Any(n => n.StartsWith("LockPlate")), "The lock plate (the preview's front). Parts: " + all);
            Assert.That(names.Any(n => n.StartsWith("Cranium")), "The skull (ChestPreviewPlayModeTests). Parts: " + all);
            Assert.That(Visual().GetComponentsInChildren<Transform>(true).Any(t => t.name == "SH10_Pickup_Chest"), "PickupV2Tests finds the model.");
        }

        [Test]
        public void TheChest_DrawsAtMost8Times()
        {
            var renderers = Visual().GetComponentsInChildren<MeshRenderer>(true);
            Assert.That(renderers.Length, Is.LessThanOrEqualTo(MaxRenderers),
                "Run Soul Hunter/Content/Merge Prop Meshes. Parts: " + string.Join(", ", renderers.Select(r => r.name)));
        }

        [Test]
        public void TheMergedMeshes_UseTheOneSharedPropMergedMaterial()
        {
            var baked = Visual().GetComponentsInChildren<MeshRenderer>(true).Where(Baked).ToList();
            Assert.That(baked.Count, Is.GreaterThan(0), "Run Soul Hunter/Content/Merge Prop Meshes.");
            foreach (var r in baked)
            {
                Assert.That(r.sharedMaterials.Length, Is.EqualTo(1), $"{r.name} draws once.");
                var material = r.sharedMaterial;
                Assert.That(material != null && material.shader != null ? material.shader.name : "(none)",
                    Is.EqualTo(PropMergedLook.ShaderName), $"{r.name}'s material.");
                Assert.That(r.GetComponent<MeshFilter>().sharedMesh.subMeshCount, Is.EqualTo(1), $"{r.name} is one mesh.");
                Assert.That(r.GetComponent<PropMergedLook>().Sources.Length, Is.GreaterThan(0), $"{r.name} lists its source materials.");
            }
            Assert.That(baked.Select(r => r.sharedMaterial).Distinct().Count(), Is.EqualTo(1), "The merged meshes share one material.");
        }

        [Test]
        public void TheChest_KeepsEveryMaterialAndEveryTriangleOfTheModel()
        {
            var model = Model().GetComponentsInChildren<MeshRenderer>(true);
            var chest = Visual().GetComponentsInChildren<MeshRenderer>(true);
            var modelMaterials = model.SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct().ToList();
            var represented = chest.SelectMany(Represented).Where(m => m != null).Distinct().ToList();
            var ordinary = chest.Where(r => !Baked(r)).ToList();
            var bakedSources = BakedSources(chest);
            foreach (var m in modelMaterials)
            {
                Assert.That(represented.Contains(m), $"{m.name} is still on the chest (as a material or a merged mesh's source).");
                long modelTris = Triangles(model, m), kept = Triangles(ordinary, m);
                // A material no merged mesh holds is drawn by ordinary renderers only, triangle for triangle.
                if (!bakedSources.Contains(m)) Assert.That(kept, Is.EqualTo(modelTris), $"Triangles drawn with {m.name}.");
                else Assert.That(kept, Is.LessThan(modelTris), $"{m.name}: some of its triangles are in a merged mesh.");
            }
            long bakedExpected = modelMaterials.Sum(m => Triangles(model, m) - Triangles(ordinary, m));
            Assert.That(chest.Where(Baked).Sum(r => Triangles(r)), Is.EqualTo(bakedExpected), "The merged meshes hold every other triangle.");
            Assert.That(chest.Sum(r => Triangles(r)), Is.EqualTo(model.Sum(r => Triangles(r))), "Every triangle of the model is drawn.");
        }

        [Test]
        public void TheRecolouredParts_AreMarkedInTheMergedMeshes()
        {
            var model = Model().GetComponentsInChildren<MeshRenderer>(true);
            var chest = Visual().GetComponentsInChildren<MeshRenderer>(true);
            var ordinary = chest.Where(r => !Baked(r)).ToList();
            var bakedSources = BakedSources(chest);
            foreach (int mask in new[] { 1, 2 })
            {
                var materials = bakedSources.Where(m => MaskFor(m) == mask).ToList();
                long expected = materials.Sum(m => Triangles(model, m) - Triangles(ordinary, m));
                long marked = chest.Where(Baked).Sum(r => MaskedTriangles(r, mask));
                Assert.That(marked, Is.EqualTo(expected), $"Triangles marked mask {mask} ({string.Join(", ", materials.Select(m => m.name))}).");
                foreach (var r in chest.Where(Baked))
                {
                    var look = r.GetComponent<PropMergedLook>();
                    var source = mask == 1 ? look.MaskA : look.MaskB;
                    Assert.That(source != null, Is.EqualTo(MaskedTriangles(r, mask) > 0),
                        $"{r.name}: mask {mask} names its material exactly when it holds such parts.");
                }
            }
            Assert.That(chest.Any(r => PropMergedLook.TryFind(r, "Chest_Iron", out _)), "ChestLook finds the iron bands.");
            Assert.That(chest.Any(r => PropMergedLook.TryFind(r, "Chest_Soul", out _)), "ChestLook and SoulCasket find the soul seam.");
        }

        [Test]
        public void TheHinge_TakesTheSameGeometry_AsFromTheUnmergedModel()
        {
            var visual = Visual();
            var model = Model();
            var after = Classify(visual);
            var before = Classify(model.transform);
            foreach (var side in new[] { Side.Lid, Side.Body, Side.OnSeam })
                Assert.That(after.Where(p => p.Value == side).Sum(p => Triangles(p.Key)),
                    Is.EqualTo(before.Where(p => p.Value == side).Sum(p => Triangles(p.Key))), $"Triangles on the {side} side.");
            // Materials no merged mesh holds, side by side and material by material.
            var bakedSources = BakedSources(after.Keys);
            var materials = model.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r => r.sharedMaterials)
                .Where(m => m != null && !bakedSources.Contains(m)).Distinct();
            foreach (var m in materials)
                foreach (var side in new[] { Side.Lid, Side.Body, Side.OnSeam })
                    Assert.That(after.Where(p => p.Value == side).Sum(p => Triangles(p.Key, m)),
                        Is.EqualTo(before.Where(p => p.Value == side).Sum(p => Triangles(p.Key, m))),
                        $"{m.name}: triangles on the {side} side.");
            // Each merged mesh lands on the side it was built for (its mesh name ends with ChestMergeTarget's split).
            var merged = after.Where(p => Baked(p.Key)).ToList();
            foreach (var pair in merged)
            {
                string mesh = pair.Key.GetComponent<MeshFilter>().sharedMesh.name;
                Side expected = mesh.Contains("_lid") ? Side.Lid : Side.Body;
                Assert.That(pair.Value, Is.EqualTo(expected), $"{mesh} is on the {expected} side of the seam.");
            }
            Assert.That(merged.Any(p => p.Value == Side.Lid), "A merged mesh hangs on the hinge (the lid side).");
            Assert.That(merged.Any(p => p.Value == Side.Body), "A merged mesh stays in the body.");
            Assert.That(after.Any(p => p.Value == Side.Body && PropMergedLook.Draws(p.Key, "Chest_Soul")),
                "A soul seam stays in the body (the preview dims it when the lid opens).");
        }

        private enum Side { Lid, Body, OnSeam }

        /// <summary>ChestPreviewStage.Layout/BuildHinge's rule, in <paramref name="space"/>'s own axes (the preview's turn only swaps axes).</summary>
        private static Dictionary<MeshRenderer, Side> Classify(Transform space)
        {
            var renderers = space.GetComponentsInChildren<MeshRenderer>(true);
            var box = renderers.ToDictionary(r => r, r => BoxIn(space, r));
            var whole = box[renderers[0]];
            foreach (var b in box.Values) whole.Encapsulate(b);
            var lids = renderers.Where(r => r.name.StartsWith("Lid")).ToList();
            Assert.That(lids.Count, Is.GreaterThan(0), "The model has a lid.");
            Vector3 up = Snap(lids.Aggregate(Vector3.zero, (s, r) => s + box[r].center) / lids.Count - whole.center);
            float seam = renderers.Where(r => r.name.StartsWith("Lid") && !r.name.StartsWith("LidBand")).Min(r => Low(box[r], up));
            float margin = SeamMargin * (High(whole, up) - Low(whole, up));
            return renderers.ToDictionary(r => r, r =>
            {
                float centre = Vector3.Dot(up, box[r].center);
                // A merged mesh's box is its real vertices' box, so its centre is on its side by more than the margin;
                // the unmerged LidBand rings sit on the seam and are left alone by the merge.
                return Mathf.Abs(centre - seam) < margin ? Side.OnSeam : centre > seam ? Side.Lid : Side.Body;
            });
        }

        private static Bounds BoxIn(Transform space, MeshRenderer r)
        {
            var mesh = r.GetComponent<MeshFilter>().sharedMesh;
            Matrix4x4 m = space.worldToLocalMatrix * r.transform.localToWorldMatrix;
            Vector3 c = m.MultiplyPoint3x4(mesh.bounds.center), e = mesh.bounds.extents;
            var ext = new Vector3(
                Mathf.Abs(m.m00) * e.x + Mathf.Abs(m.m01) * e.y + Mathf.Abs(m.m02) * e.z,
                Mathf.Abs(m.m10) * e.x + Mathf.Abs(m.m11) * e.y + Mathf.Abs(m.m12) * e.z,
                Mathf.Abs(m.m20) * e.x + Mathf.Abs(m.m21) * e.y + Mathf.Abs(m.m22) * e.z);
            return new Bounds(c, ext * 2f);
        }

        private static Vector3 Snap(Vector3 v)
        {
            Vector3 a = new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
            if (a.x >= a.y && a.x >= a.z) return new Vector3(Mathf.Sign(v.x), 0f, 0f);
            if (a.y >= a.z) return new Vector3(0f, Mathf.Sign(v.y), 0f);
            return new Vector3(0f, 0f, Mathf.Sign(v.z));
        }

        private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        private static float Low(Bounds b, Vector3 up) => Vector3.Dot(up, b.center) - Vector3.Dot(Abs(up), b.extents);
        private static float High(Bounds b, Vector3 up) => Vector3.Dot(up, b.center) + Vector3.Dot(Abs(up), b.extents);

        private static long Triangles(IEnumerable<MeshRenderer> renderers, Material material) => renderers.Sum(r => Triangles(r, material));

        /// <summary>Triangles a renderer draws, with <paramref name="material"/> only when it is given (by submesh material).</summary>
        private static long Triangles(MeshRenderer r, Material material = null)
        {
            var mesh = r.TryGetComponent(out MeshFilter f) ? f.sharedMesh : null;
            if (mesh == null) return 0;
            var materials = r.sharedMaterials;
            long total = 0;
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
                if (material == null || (sub < materials.Length && materials[sub] == material))
                    total += mesh.GetIndexCount(sub) / 3;
            return total;
        }

        /// <summary>Triangles of a merged mesh whose vertices carry mask <paramref name="mask"/> (UV2.z, PropMeshMerger.BuildBaked).</summary>
        private static long MaskedTriangles(MeshRenderer r, int mask)
        {
            var mesh = r.GetComponent<MeshFilter>().sharedMesh;
            var surface = new List<Vector4>();
            mesh.GetUVs(2, surface);
            Assert.That(surface.Count, Is.EqualTo(mesh.vertexCount), $"{mesh.name} has its baked surface (UV2).");
            var tris = mesh.GetTriangles(0);
            long count = 0;
            for (int i = 0; i < tris.Length; i += 3)
                if (Mathf.RoundToInt(surface[tris[i]].z) == mask) count++;
            return count;
        }
    }
}
