using NUnit.Framework;
using SoulHunter.Gameplay.Data;
using UnityEditor;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>The Ruined Castle as a walled corridor (owner, 2026-09-26; VS's Inlaid Library).</summary>
    public class CorridorTests
    {
        [TearDown]
        public void TearDown() => StageCorridor.End();

        [Test]
        public void OnlyTheRuinedCastle_IsACorridor_AndSpawnsOnlyFromItsEnds()
        {
            for (int stage = 1; stage <= 10; stage++)
            {
                Assert.That(WaveTables.CorridorHalfWidth(stage), Is.EqualTo(stage == 4 ? 9f : 0f), $"Stage {stage}");
                Assert.That(WaveTables.SideBiasFor(stage), Is.EqualTo(stage == 4 ? 1f : 0.8f), $"Stage {stage}");
            }
            var castle = AssetDatabase.LoadAssetAtPath<WaveData>(WaveTables.StageWaveAssets[3]);
            Assert.That(castle.SpawnSide, Is.EqualTo(SpawnSide.LeftRight));
            Assert.That(castle.SideBias, Is.EqualTo(1f), "The castle asset carries its corridor bias (Build Wave Tables).");
        }

        [Test]
        public void StageCorridor_ClampsPointsBetweenItsWalls()
        {
            Assert.That(StageCorridor.Active, Is.False);
            var outside = new Vector3(5f, 1f, 30f);
            Assert.That(StageCorridor.Clamp(outside), Is.EqualTo(outside), "No corridor: nothing changes.");
            StageCorridor.Begin(9f, 10f);
            Assert.That(StageCorridor.Active, Is.True);
            Assert.That(StageCorridor.Clamp(outside).z, Is.EqualTo(18f), "Kept 1 unit inside the north wall.");
            Assert.That(StageCorridor.Clamp(new Vector3(0f, 0f, -20f)).z, Is.EqualTo(2f));
            Assert.That(StageCorridor.Clamp(outside).x, Is.EqualTo(5f), "Free along the corridor.");
            Assert.That(StageCorridor.Contains(new Vector3(100f, 0f, 18.9f)), Is.True);
            Assert.That(StageCorridor.Contains(new Vector3(0f, 0f, 19.5f)), Is.False);
        }

        [Test]
        public void CastleWall_IsASolidTwentyUnitObstacle()
        {
            var prefab = GameContentCatalog.Load().CastleWall;
            Assert.That(prefab, Is.Not.Null, "GameContentCatalog.CastleWall is set (Soul Hunter/Content/Import Stage Props).");
            Assert.That(prefab.layer, Is.EqualTo(LayerMask.NameToLayer("Obstacle")), "Blocks the player only.");
            var box = prefab.GetComponent<BoxCollider>();
            Assert.That(box != null && !box.isTrigger, Is.True, "A solid collider.");
            Assert.That(box.size.x, Is.EqualTo(20f).Within(0.01f));
            Assert.That(box.size.y, Is.GreaterThanOrEqualTo(2.5f));
            var instance = Object.Instantiate(prefab);
            try
            {
                var renderers = instance.GetComponentsInChildren<MeshRenderer>();
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                Assert.That(bounds.size.x, Is.EqualTo(20f).Within(0.2f), "The module is laid along X.");
                Assert.That(bounds.min.y, Is.EqualTo(0f).Within(0.05f), "It stands on the floor.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
