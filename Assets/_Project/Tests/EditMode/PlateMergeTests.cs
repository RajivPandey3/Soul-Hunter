using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SoulHunter.Gameplay.Pickups;
using SoulHunter.Gameplay.VFX;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Draw calls (owner, 2026-10-15: "sab ko single mesh or single material mein convert karo"): PropMeshMerger joins the
    /// Rune Plate's parts (Slab, Inset and 17 rune parts) into one mesh drawn with the one shared SoulHunter/PropMerged
    /// material, the rune parts marked mask A. The look must be unchanged, and PressurePlate must still find its runes
    /// (PropMergedLook.TryFind "Rune") and dim only them while the plate recharges; the stone in the same mesh stays.
    /// The paths are LightSourceImporter.PlatePrefabPath and LightSourceImporter.PlateModel (the editor assembly is not
    /// referenced by the tests).
    /// </summary>
    public class PlateMergeTests
    {
        private const string PlatePrefab = "Assets/Prefabs/Props/SH10_RunePlate.prefab";
        private const string PlateModel = "Assets/Art/Models/SH10_Prop_RunePlate.fbx";
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private static GameObject Prefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlatePrefab);
            Assert.IsNotNull(prefab, PlatePrefab);
            return prefab;
        }

        private static Transform Visual(GameObject root)
        {
            var visual = root.transform.Find("Visual");
            Assert.IsNotNull(visual, "The plate keeps its Visual group.");
            return visual;
        }

        private static long Triangles(Mesh mesh)
        {
            long count = 0;
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
                if (mesh.GetTopology(sub) == MeshTopology.Triangles) count += mesh.GetIndexCount(sub) / 3;
            return count;
        }

        private static long Triangles(Transform root) =>
            root.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).Sum(f => Triangles(f.sharedMesh));

        [Test]
        public void ThePlate_IsOneRenderer_WithTheSharedMergedMaterial()
        {
            var renderers = Visual(Prefab()).GetComponentsInChildren<MeshRenderer>(true);
            Assert.That(renderers.Length, Is.EqualTo(1), $"{renderers.Length} renderers under Visual: run Soul Hunter/Content/Merge Prop Meshes.");
            var r = renderers[0];
            Assert.That(r.sharedMaterials.Length, Is.EqualTo(1), "One material.");
            Assert.That(r.sharedMaterial != null && r.sharedMaterial.shader != null && r.sharedMaterial.shader.name == PropMergedLook.ShaderName, Is.True,
                $"The merged plate uses {PropMergedLook.ShaderName}.");
            Assert.That(r.GetComponent<PropMergedLook>(), Is.Not.Null, "The merged renderer remembers its source materials.");
        }

        [Test]
        public void ThePlate_StillHasItsRuneGlow_AsTheMaskAPart()
        {
            var r = Visual(Prefab()).GetComponentsInChildren<MeshRenderer>(true).Single();
            Assert.That(PropMergedLook.Draws(r, "Rune"), Is.True, "The rune material is baked into the plate.");
            var look = r.GetComponent<PropMergedLook>();
            var rune = look.Sources.FirstOrDefault(m => m != null && m.name.Contains("Rune"));
            Assert.IsNotNull(rune, "A Rune source material.");
            Assert.That(rune.HasProperty(EmissionId) && rune.GetColor(EmissionId).maxColorComponent > 0.05f, Is.True, $"{rune.name} glows.");
            Assert.That(PropMergedLook.TryFind(r, "Rune", out var part), Is.True, "PressurePlate finds the runes by their material.");
            Assert.That(part.Mask, Is.EqualTo(1), "The runes are mask A, so dimming them dims no stone.");
            Assert.That(part.Source, Is.SameAs(rune));
            Assert.That(look.Sources.Where(m => m != null && !m.name.Contains("Rune")).Any(), Is.True, "The stone is baked in too.");
            Assert.That(look.MaskB == null || !look.MaskB.name.Contains("Rune"), Is.True, "Only one mask carries the runes.");
        }

        [Test]
        public void Recharging_DimsOnlyTheRuneMask_AndReadyRestoresIt()
        {
            var instance = Object.Instantiate(Prefab());
            try
            {
                var plate = instance.GetComponent<PressurePlate>();
                Assert.IsNotNull(plate);
                // Edit Mode does not run Awake on Instantiate: call it as Play Mode would.
                typeof(PressurePlate).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(plate, null);
                var setDimmed = typeof(PressurePlate).GetMethod("SetDimmed", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(plate.RunePartCount, Is.EqualTo(1), "One rune part: the merged mask A.");

                var r = Visual(instance).GetComponentsInChildren<MeshRenderer>(true).Single();
                Assert.That(PropMergedLook.TryFind(r, "Rune", out var part), Is.True);
                var srcBase = part.Source.GetColor(BaseColorId);
                var srcEmission = part.Source.GetColor(EmissionId);
                var block = new MaterialPropertyBlock();

                setDimmed.Invoke(plate, new object[] { true });
                AssertRgb(PropMergedLook.CurrentEmission(part, block), srcEmission * 0.08f, "dimmed rune glow");
                AssertRgb(PropMergedLook.CurrentBase(part, block), srcBase * 0.35f, "dimmed rune colour");
                r.GetPropertyBlock(block);
                Assert.That(block.HasColor(Shader.PropertyToID("_MaskB_Base")) || block.HasColor(BaseColorId) || block.HasColor(EmissionId), Is.False,
                    "Nothing but the rune mask is recoloured: the stone keeps its baked colour.");

                setDimmed.Invoke(plate, new object[] { false });
                AssertRgb(PropMergedLook.CurrentEmission(part, block), srcEmission, "ready rune glow");
                AssertRgb(PropMergedLook.CurrentBase(part, block), srcBase, "ready rune colour");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void AssertRgb(Color actual, Color expected, string what)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.002f), what + " r");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.002f), what + " g");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.002f), what + " b");
        }

        [Test]
        public void ThePlate_KeepsEveryTriangleOfItsModel()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(PlateModel);
            Assert.IsNotNull(model, PlateModel);
            long expected = Triangles(model.transform);
            Assert.That(expected, Is.GreaterThan(0));
            Assert.That(Triangles(Visual(Prefab())), Is.EqualTo(expected), "Merging joins meshes; it never adds or drops a triangle.");
        }
    }
}
