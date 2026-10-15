using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Owner, 2026-10-15: "sabhi ko single mesh or single material banao". No prefab under Assets/Prefabs still draws a
    /// model with the many-material FacetedPreview look (HordeMeshMerger.MergeEveryFaceted gives each one mesh and the
    /// shared HordeMerged material), and the merchant and the four light-source pickups are one PropMerged mesh each.
    /// </summary>
    public class AllMergeTests
    {
        private const string Faceted = "SoulHunter/Review/FacetedPreview";
        // HordeMeshMerger.LeftAsTheyAre: the asset-review ghoul study (scene L01_AssetReview only).
        private static readonly string[] LeftAsTheyAre = { "Assets/Prefabs/Characters/L01R_GraveGhoulStudy.prefab" };

        [Test]
        public void NoPrefab_DrawsWithTheManyMaterialFacetedLook()
        {
            var left = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !LeftAsTheyAre.Contains(p))
                .Where(p => AssetDatabase.LoadAssetAtPath<GameObject>(p).GetComponentsInChildren<Renderer>(true)
                    .SelectMany(r => r.sharedMaterials).Any(m => m != null && m.shader != null && m.shader.name == Faceted))
                .ToList();
            Assert.That(left, Is.Empty, "Run Soul Hunter/Content/Merge Every Faceted Model.");
        }

        [TestCase("Assets/Prefabs/Player/SH10_Hero_Aria.prefab")]
        [TestCase("Assets/Prefabs/Player/SH10_Kael.prefab")]
        [TestCase("Assets/Prefabs/Weapons/SH10_Knife.prefab")]
        [TestCase("Assets/Prefabs/Items/SH10_Health_Chicken.prefab")]
        public void AFacetedModel_IsOneMesh_WithTheSharedHordeMaterial(string path)
        {
            var renderers = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Renderer>(true)
                .Where(r => !(r is ParticleSystemRenderer)).ToList();
            Assert.That(renderers, Is.Not.Empty);
            foreach (var r in renderers)
            {
                Assert.That(r.sharedMaterials.Length, Is.EqualTo(1), r.name);
                Assert.That(r.sharedMaterial.shader.name, Is.EqualTo("SoulHunter/HordeMerged"), r.name);
                var mesh = r is SkinnedMeshRenderer s ? s.sharedMesh : r.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(mesh.subMeshCount, Is.EqualTo(1), r.name);
                if (r is SkinnedMeshRenderer) Assert.That(mesh.bindposes.Length, Is.GreaterThan(0), "the skin is kept");
            }
        }

        [TestCase("Assets/Prefabs/NPC/SH10_NPC_WanderingMerchant.prefab")]
        [TestCase("Assets/Prefabs/Items/SH10_Pickup_GildedCharm.prefab")]
        [TestCase("Assets/Prefabs/Items/SH10_Pickup_LuckCharm.prefab")]
        [TestCase("Assets/Prefabs/Items/SH10_Pickup_HellfireBreath.prefab")]
        [TestCase("Assets/Prefabs/Items/SH10_Pickup_SoulToll.prefab")]
        public void TheMerchantAndTheCharms_AreOnePropMergedMesh(string path)
        {
            var renderers = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<MeshRenderer>()
                .Where(r => r.enabled && !r.name.StartsWith("GroundGlow")).ToList();
            Assert.That(renderers.Count, Is.EqualTo(1));
            Assert.That(renderers[0].sharedMaterials.Select(m => m.shader.name), Is.EqualTo(new[] { "SoulHunter/PropMerged" }));
        }
    }
}
