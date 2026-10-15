using System.Linq;
using NUnit.Framework;
using UnityEngine;
using SoulHunter.Gameplay.AI;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// 60 FPS, horde on the GPU (2026-10-15): every level's enemy and elite model is baked into a vertex animation texture,
    /// and the library in Resources finds each by the skinned mesh the body draws today.
    /// </summary>
    public class HordeVatTests
    {
        [Test]
        public void EveryEnemyAndEliteModel_IsBaked_WithItsClips_AndFoundByItsMesh()
        {
            var library = Resources.Load<HordeVatLibrary>(HordeVatLibrary.ResourceName);
            Assert.That(library, Is.Not.Null, "Soul Hunter/Content/Bake Horde VAT writes the library.");
            Assert.That(library.Assets.Length, Is.EqualTo(20), "Ten levels, an enemy and an elite each.");
            foreach (var asset in library.Assets)
            {
                Assert.That(asset.SourceMesh, Is.Not.Null, asset.name);
                Assert.That(asset.Mesh.vertexCount, Is.EqualTo(asset.SourceMesh.vertexCount), $"{asset.name}: one VAT vertex per body vertex.");
                Assert.That(asset.VertexCount, Is.EqualTo(asset.Mesh.vertexCount));
                Assert.That(asset.IndexOf("Idle"), Is.GreaterThanOrEqualTo(0), $"{asset.name} has its Idle clip.");
                Assert.That(asset.Clips.Select(c => c.Name), Is.SupersetOf(new[] { "Idle", "Run", "Attack", "Death" }));
                Assert.That(asset.Materials.Length, Is.EqualTo(asset.Mesh.subMeshCount));
                Assert.That(asset.Materials.All(m => m != null && m.enableInstancing && m.shader.name == "SoulHunter/HordeVat"), Is.True, asset.name);
                Assert.That(asset.Positions.height, Is.GreaterThanOrEqualTo(asset.TotalFrames * asset.RowsPerFrame));
                Assert.That(asset.Bounds.size.y, Is.GreaterThan(0.3f), $"{asset.name}: real size from the rig, not the mesh's own 1 cm bounds.");
                Assert.That(library.For(asset.SourceMesh), Is.SameAs(asset));
            }
        }
    }
}
