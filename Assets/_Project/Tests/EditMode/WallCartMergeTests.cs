using System.Linq;
using NUnit.Framework;
using SoulHunter.Gameplay.Hazards;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Draw calls (owner, 2026-10-15: "castle wall or rail cart bhi jodo", then "sab ko single mesh or single material
    /// mein convert karo"): PropMeshMerger joins the Ruined Castle wall (136 parts in 3 materials, 14 modules on screen on
    /// Level 4) into one mesh, and the Level 3 rail cart (30 primitive parts) into one mesh per group and shadow setting,
    /// all drawn with the one shared SoulHunter/PropMerged material. The wall keeps every triangle of its model and casts
    /// shadows; the cart's rails and its moving cart stay separate groups, so the cart still rides the rails. Paths:
    /// LightSourceImporter.WallPrefabPath / WallModel and StageHazardBuilder.RailCartPath (the editor assembly is not
    /// referenced by the tests). Shared checks: MergedProps (PropMergeTests.cs).
    /// </summary>
    public class WallCartMergeTests
    {
        private const string WallPrefab = "Assets/Prefabs/Props/SH10_CastleWall.prefab";
        private const string WallModel = "Assets/Art/Models/SH10_Prop_CastleWall.fbx";
        private const string CartPrefab = "Assets/Prefabs/Props/SH10_RailCart.prefab";
        private const string HazardMaterials = "Assets/Art/Materials/Hazards";

        [Test]
        public void TheCastleWall_IsOneMergedRenderer_AndKeepsEveryMaterialTriangleAndItsShadows()
        {
            var visual = AssetDatabase.LoadAssetAtPath<GameObject>(WallPrefab).transform.Find("Visual");
            Assert.IsNotNull(visual, "The wall keeps its Visual group.");
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(WallModel);
            var modelMaterials = model.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct().ToList();
            var renderers = MergedProps.AssertMergedCount(visual, 1, "Castle wall Visual (it was 136)");
            Assert.That(visual.GetComponentsInChildren<MeshRenderer>(true).Length, Is.EqualTo(1), "No glow or other part left in the wall.");
            var drawn = MergedProps.Drawn(visual);
            Assert.That(drawn.Select(m => m.name), Is.EquivalentTo(modelMaterials.Select(m => m.name)), "Every material of the model is still baked in.");
            MergedProps.AssertRenderersSound(visual, "Castle wall");
            Assert.That(MergedProps.Triangles(visual), Is.EqualTo(MergedProps.Triangles(model.transform)), "Every triangle of the model is still drawn.");
            Assert.That(renderers.All(r => r.enabled && r.shadowCastingMode == ShadowCastingMode.On), Is.True, "The wall still casts shadows.");
        }

        private static Material Hazard(string name)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>($"{HazardMaterials}/SH10_Hazard_{name}.mat");
            Assert.IsNotNull(m, name);
            return m;
        }

        /// <summary>
        /// Triangles of <paramref name="count"/> copies of the primitive StageHazardBuilder makes with CreatePrimitive. The
        /// "Sphere.fbx" builtin resource is not that mesh (760 triangles against the primitive's 768), so the primitive itself is read.
        /// </summary>
        private static long Builtin(string mesh, int count)
        {
            var type = mesh.StartsWith("Sphere") ? PrimitiveType.Sphere : mesh.StartsWith("Cylinder") ? PrimitiveType.Cylinder : PrimitiveType.Cube;
            var go = GameObject.CreatePrimitive(type);
            try { return MergedProps.Triangles(go.GetComponent<MeshFilter>().sharedMesh) * count; }
            finally { Object.DestroyImmediate(go); }
        }

        /// <summary>StageHazardBuilder.BuildRailCart's sleeper loop.</summary>
        private static int Sleepers()
        {
            int n = 0;
            float length = RailCartTrack.Length;
            for (float x = -length * 0.5f + 0.5f; x <= length * 0.5f - 0.4f; x += 1.5f) n++;
            return n;
        }

        /// <summary>
        /// StageHazardBuilder.BuildRailCart sets the shadows per part, and the merge keeps different shadow settings apart:
        /// Rails = the two iron rails and the wooden sleepers (no shadow) + the two iron end stops (shadow): 2 renderers;
        /// Cart/Visual = the wooden body, two iron bands and four iron wheels (shadow) + three soul crystals (no shadow):
        /// 2 renderers. 4 in all (30 before the merge).
        /// </summary>
        [Test]
        public void TheRailCart_IsOneMergedRendererPerShadowSetting_AndItsCartStaysApartFromItsRails()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CartPrefab);
            var rails = prefab.transform.Find("Rails");
            var cart = prefab.transform.Find("Cart");
            var cartVisual = prefab.transform.Find("Cart/Visual");
            Assert.IsNotNull(rails, "The rails group is kept.");
            Assert.IsNotNull(cartVisual, "The cart's visual is kept under the moving Cart.");

            Material iron = Hazard("Iron"), sleeper = Hazard("SleeperWood"), wood = Hazard("CartWood"), soul = Hazard("SoulCrystal");
            var expected = new[]
            {
                (group: rails, shadows: ShadowCastingMode.Off, sources: new[] { iron, sleeper }, triangles: Builtin("Cube.fbx", 2 + Sleepers())),
                (group: rails, shadows: ShadowCastingMode.On, sources: new[] { iron }, triangles: Builtin("Cube.fbx", 2)),
                (group: cartVisual, shadows: ShadowCastingMode.On, sources: new[] { wood, iron }, triangles: Builtin("Cube.fbx", 3) + Builtin("Cylinder.fbx", 4)),
                (group: cartVisual, shadows: ShadowCastingMode.Off, sources: new[] { soul }, triangles: Builtin("Sphere.fbx", 3)),
            };
            foreach (var group in new[] { rails, cartVisual })
            {
                MergedProps.AssertMergedCount(group, 2, "Rail cart " + group.name);
                MergedProps.AssertRenderersSound(group, "Rail cart " + group.name);
            }
            foreach (var e in expected)
            {
                var r = e.group.GetComponentsInChildren<MeshRenderer>(true).SingleOrDefault(x => x.shadowCastingMode == e.shadows);
                Assert.IsNotNull(r, $"{e.group.name} has one merged renderer with shadows {e.shadows}.");
                Assert.That(r.enabled, Is.True);
                Assert.That(MergedProps.SourcesOf(r), Is.EquivalentTo(e.sources), $"{e.group.name} ({e.shadows}) bakes {string.Join(", ", e.sources.Select(m => m.name))}.");
                Assert.That(MergedProps.Triangles(r.GetComponent<MeshFilter>().sharedMesh), Is.EqualTo(e.triangles),
                    $"{e.group.name} ({e.shadows}): every triangle of its primitives is still drawn.");
            }

            Assert.That(rails.GetComponentsInChildren<MeshRenderer>(true).Any(r => r.transform.IsChildOf(cart)), Is.False, "No rail part rides with the cart.");
            Assert.That(cartVisual.GetComponentsInChildren<MeshRenderer>(true).All(r => r.transform.IsChildOf(cart)), Is.True, "Every cart part rides with the cart.");
            Assert.That(prefab.GetComponentsInChildren<MeshRenderer>(true).Length, Is.EqualTo(4), "The cart drew 30 renderers before the merge.");
        }
    }
}
