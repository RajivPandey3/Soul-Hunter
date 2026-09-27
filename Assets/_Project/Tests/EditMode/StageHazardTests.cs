using System.Linq;
using NUnit.Framework;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Data;
using SoulHunter.Gameplay.Hazards;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// VS stage hazards (2026-09-27): the per-stage schedule, the pure maths behind markers, rings and rails, and the
    /// built assets (Soul Hunter/Content/Build Stage Hazards).
    /// </summary>
    public class StageHazardTests
    {
        [Test]
        public void DeathsShadow_ComesAt8And12Minutes_OnStages5And9()
        {
            foreach (int stage in new[] { 5, 9 })
            {
                var times = StageHazardTable.For(stage).Where(e => e.Kind == StageHazardKind.DeathsShadow).Select(e => e.TimeSeconds).ToArray();
                Assert.That(times, Is.EqualTo(new[] { 480f, 720f }), $"Stage {stage}");
            }
            Assert.That(StageHazardTable.ShadowLifetimeSeconds, Is.EqualTo(120f), "VS Stalker: follows the player for 2 minutes.");
            Assert.That(StageHazardTable.ShadowScale, Is.EqualTo(1.5f));
        }

        [Test]
        public void SoulMeteors_FallEveryFourMinutesFrom4_OnStages6And10()
        {
            // Balance 2026-09-28 (VS Gallo Tower has stars at a few set minutes): 4:00, 8:00 ... 28:00.
            var expected = Enumerable.Range(1, 7).Select(i => i * 240f).ToArray();
            foreach (int stage in new[] { 6, 10 })
            {
                var times = StageHazardTable.For(stage).Where(e => e.Kind == StageHazardKind.SoulMeteors).Select(e => e.TimeSeconds).ToArray();
                Assert.That(times, Is.EqualTo(expected), $"Stage {stage}");
            }
            Assert.That(StageHazardTable.MeteorDelaySeconds, Is.EqualTo(2f), "VS Shooting Star: impact 2 s after the marker.");
            Assert.That(StageHazardTable.MeteorPlayerDamage, Is.EqualTo(30), "VS Shooting Star: 30 damage.");
        }

        [Test]
        public void PileAssaults_OnStage8_At9_10_12And20Minutes()
        {
            var times = StageHazardTable.For(8).Where(e => e.Kind == StageHazardKind.PileAssault).Select(e => e.TimeSeconds).ToArray();
            Assert.That(times, Is.EqualTo(new[] { 540f, 600f, 720f, 1200f }));
        }

        [Test]
        public void OtherStages_HaveNoTimedHazards_AndOnlyStage3HasRails()
        {
            foreach (int stage in new[] { 1, 2, 3, 4, 7 })
                Assert.That(StageHazardTable.For(stage), Is.Empty, $"Stage {stage}");
            for (int stage = 1; stage <= 10; stage++)
                Assert.That(StageHazardTable.HasRailCarts(stage), Is.EqualTo(stage == 3), $"Stage {stage}");
            for (int stage = 1; stage <= 10; stage++)
            {
                var list = StageHazardTable.For(stage);
                for (int i = 1; i < list.Count; i++)
                    Assert.That(list[i].TimeSeconds, Is.GreaterThanOrEqualTo(list[i - 1].TimeSeconds), "Sorted by time.");
            }
        }

        [Test]
        public void MeteorVolley_StartsOnThePlayer_AndRingsIt()
        {
            int count = StageHazardTable.MeteorVolleyCount;
            Assert.That(StageHazardTable.MeteorOffset(0, count, 0.3f, 0.7f), Is.EqualTo(Vector3.zero), "Standing still is never safe.");
            var angles = new System.Collections.Generic.List<float>();
            for (int i = 1; i < count; i++)
            {
                var o = StageHazardTable.MeteorOffset(i, count, 0.5f, 0.5f);
                Assert.That(o.y, Is.EqualTo(0f));
                Assert.That(o.magnitude, Is.InRange(StageHazardTable.MeteorMinSpread - 0.001f, StageHazardTable.MeteorMaxSpread + 0.001f));
                angles.Add(Mathf.Atan2(o.z, o.x));
            }
            Assert.That(angles.Distinct().Count(), Is.EqualTo(count - 1), "Each marker lands in its own direction.");
            foreach (float roll in new[] { 0f, 1f })
                Assert.That(StageHazardTable.MeteorOffset(3, count, roll, roll).magnitude,
                    Is.InRange(StageHazardTable.MeteorMinSpread - 0.001f, StageHazardTable.MeteorMaxSpread + 0.001f));
        }

        [Test]
        public void PileRing_IsAnEllipseAroundThePlayer()
        {
            int count = StageHazardTable.PileCount;
            for (int i = 0; i < count; i++)
            {
                var o = StageHazardTable.PileOffset(i, count);
                float e = (o.x / StageHazardTable.PileRadiusX) * (o.x / StageHazardTable.PileRadiusX) +
                          (o.z / StageHazardTable.PileRadiusZ) * (o.z / StageHazardTable.PileRadiusZ);
                Assert.That(e, Is.EqualTo(1f).Within(0.001f));
            }
            Assert.That(StageHazardTable.PileRadiusX, Is.GreaterThan(StageHazardTable.PileRadiusZ), "Wider than tall, like the screen (VS ellipsoid ring).");
        }

        [Test]
        public void ShadowSpeed_IsThePlayersBaseSpeed_WhateverTheModel()
        {
            Assert.That(3f * StageHazardTable.ShadowSpeedMultiplier(3f), Is.EqualTo(StageHazardTable.ShadowMoveSpeed).Within(0.001f));
            Assert.That(2f * StageHazardTable.ShadowSpeedMultiplier(2f), Is.EqualTo(StageHazardTable.ShadowMoveSpeed).Within(0.001f));
        }

        [Test]
        public void RailMaths_SegmentDistance_AndCrossing()
        {
            Vector3 a = new Vector3(-10f, 0f, 0f), b = new Vector3(10f, 0f, 0f);
            Assert.That(StageHazardTable.SegmentDistanceXZ(a, b, new Vector3(0f, 5f, 2f)), Is.EqualTo(2f).Within(0.001f), "Height is ignored.");
            Assert.That(StageHazardTable.SegmentDistanceXZ(a, b, new Vector3(13f, 0f, 4f)), Is.EqualTo(5f).Within(0.001f), "Past the end: to the end point.");
            Assert.That(StageHazardTable.SegmentsApart(a, b, new Vector3(0f, 0f, -5f), new Vector3(0f, 0f, 5f), 1f), Is.False, "Crossing rails.");
            Assert.That(StageHazardTable.SegmentsApart(a, b, new Vector3(-10f, 0f, 20f), new Vector3(10f, 0f, 20f), 3f), Is.True, "Parallel rails 20 apart.");
            Assert.That(StageHazardTable.SegmentsApart(a, b, new Vector3(-10f, 0f, 1f), new Vector3(10f, 0f, 1f), 3f), Is.False, "Too close.");

            var candidates = StageHazardTable.RailCandidates();
            var (a0, a1) = StageHazardTable.RailEnds(candidates[0].offset, candidates[0].yaw, RailCartTrack.Length);
            var (b0, b1) = StageHazardTable.RailEnds(candidates[1].offset, candidates[1].yaw, RailCartTrack.Length);
            Assert.That(StageHazardTable.SegmentsApart(a0, a1, b0, b1, 3f), Is.True, "The preferred pair of rails does not cross.");
            foreach (var c in candidates)
                Assert.That(new Vector2(c.offset.x, c.offset.z).magnitude, Is.GreaterThan(4.5f + 1f), "Rails keep off the open spawn centre.");
        }

        [Test]
        public void RailCartPrefab_IsATrackWithAHittableCart_NotAnEnemy()
        {
            var prefab = GameContentCatalog.Load().RailCart;
            Assert.That(prefab, Is.Not.Null, "GameContentCatalog.RailCart is set (Soul Hunter/Content/Build Stage Hazards).");
            Assert.That(prefab.GetComponent<RailCartTrack>(), Is.Not.Null);
            var cart = prefab.GetComponentInChildren<RailCart>(true);
            Assert.That(cart, Is.Not.Null);
            Assert.That(cart.CompareTag("Enemy"), Is.True, "Weapons that hit the Enemy tag can push it.");
            Assert.That(cart.gameObject.layer, Is.EqualTo(LayerMask.NameToLayer("Enemy")), "Weapons that query the Enemy layer can push it.");
            Assert.That(cart.GetComponent<EnemyController>(), Is.Null, "Not an enemy: nothing aims at it, never a kill.");
            var trigger = cart.GetComponent<Collider>();
            Assert.That(trigger != null && trigger.isTrigger, Is.True, "Never blocks the player.");
            Assert.That(cart.GetComponent<HealthController>().MaxHealth, Is.EqualTo(RailCart.MaxHealth));
            Assert.That(prefab.GetComponentsInChildren<Collider>(true).Length, Is.EqualTo(1), "The rails have no colliders.");
            Assert.That(cart.transform.localPosition.x, Is.EqualTo(-RailCartTrack.HalfTravel).Within(0.01f), "The cart waits at one end.");
        }

        [Test]
        public void MarkerAndBullet_AreBuilt()
        {
            var catalog = GameContentCatalog.Load();
            Assert.That(catalog.HazardMarker, Is.Not.Null, "GameContentCatalog.HazardMarker is set.");
            var renderer = catalog.HazardMarker.GetComponentInChildren<MeshRenderer>();
            Assert.That(renderer != null && renderer.sharedMaterial != null && renderer.sharedMaterial.GetTexture("_BaseMap") != null, Is.True,
                "The marker draws the VFX v2 magic circle.");
            Assert.That(catalog.HazardMarker.GetComponentsInChildren<Collider>(true), Is.Empty, "A marker is only a picture.");
            Assert.That(catalog.HazardBullet, Is.Not.Null, "GameContentCatalog.HazardBullet is set.");
            Assert.That(catalog.HazardBullet.GetComponent<Projectile>(), Is.Not.Null);
        }
    }
}
