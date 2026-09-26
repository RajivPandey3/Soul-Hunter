using System.Collections.Generic;
using NUnit.Framework;
using SoulHunter.Gameplay.AI;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// VS rule: enemies push each other into a crowd. EnemyCrowd is the soft push that replaced single-file trains.
    /// </summary>
    public class EnemyCrowdTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects)
            {
                if (go == null) continue;
                EnemyController.ActiveEnemies.Remove(go.GetComponent<EnemyController>());
                Object.DestroyImmediate(go);
            }
            _objects.Clear();
            EnemyCrowd.Enabled = true;
            EnemyCrowd.Rebuild();
        }

        private EnemyController Enemy(Vector3 position, float scale = 1f)
        {
            var go = new GameObject("CrowdTestEnemy");
            _objects.Add(go);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * scale;
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.radius = 0.5f;
            var enemy = go.AddComponent<EnemyController>();
            if (!EnemyController.ActiveEnemies.Contains(enemy)) EnemyController.ActiveEnemies.Add(enemy);
            return enemy;
        }

        [Test]
        public void PushFrom_IsZeroWhenApart_AndGrowsWithOverlap()
        {
            Assert.That(EnemyCrowd.PushFrom(Vector3.zero, 0.5f, new Vector3(1.2f, 0f, 0f), 0.5f, 1, 2), Is.EqualTo(Vector3.zero));
            Vector3 half = EnemyCrowd.PushFrom(Vector3.zero, 0.5f, new Vector3(0.5f, 0f, 0f), 0.5f, 1, 2);
            Assert.That(half.x, Is.EqualTo(-0.5f).Within(0.001f), "Half overlap pushes at half strength, away from the neighbour.");
            Vector3 deep = EnemyCrowd.PushFrom(Vector3.zero, 0.5f, new Vector3(0.1f, 3f, 0f), 0.5f, 1, 2);
            Assert.That(deep.magnitude, Is.GreaterThan(half.magnitude));
            Assert.That(deep.y, Is.EqualTo(0f), "The push stays on the ground plane.");
        }

        [Test]
        public void PushFrom_TwoBodiesOnTheSameSpot_PartInOppositeDirections()
        {
            Vector3 a = EnemyCrowd.PushFrom(Vector3.zero, 0.5f, Vector3.zero, 0.5f, 10, 20);
            Vector3 b = EnemyCrowd.PushFrom(Vector3.zero, 0.5f, Vector3.zero, 0.5f, 20, 10);
            Assert.That(a.magnitude, Is.EqualTo(1f).Within(0.001f));
            Assert.That(Vector3.Dot(a, b), Is.LessThan(-0.99f), "Stacked bodies must split apart, not move together.");
        }

        [Test]
        public void Separation_PushesAStackApart_AndIgnoresFarEnemies()
        {
            var left = Enemy(new Vector3(-0.3f, 1f, 0f));
            var right = Enemy(new Vector3(0.3f, 1f, 0f));
            var far = Enemy(new Vector3(10f, 1f, 0f));
            EnemyCrowd.Rebuild();

            Assert.That(EnemyCrowd.Separation(left).x, Is.LessThan(0f));
            Assert.That(EnemyCrowd.Separation(right).x, Is.GreaterThan(0f));
            Assert.That(EnemyCrowd.Separation(far), Is.EqualTo(Vector3.zero));
            Assert.That(EnemyCrowd.Separation(left).magnitude, Is.LessThanOrEqualTo(1.0001f));

            EnemyCrowd.Enabled = false;
            Assert.That(EnemyCrowd.Separation(left), Is.EqualTo(Vector3.zero), "Switched off for A/B checks.");
        }

        /// <summary>
        /// Regression guard: a full horde (300, packed tight as in Stage 10) costs one grid rebuild plus 300
        /// separation reads per frame. This runs in the editor's debug-mode scripts (much slower than a player build),
        /// so the budget is loose; frame-rate acceptance comes from the player-build benchmark. The first version
        /// took 7.8 ms here.
        /// </summary>
        [Test]
        public void Separation_For300PackedEnemies_StaysUnderBudget()
        {
            var horde = new List<EnemyController>();
            for (int i = 0; i < 300; i++) horde.Add(Enemy(new Vector3((i % 20) * 0.9f, 1f, (i / 20) * 0.9f)));
            // Warm up (JIT, list growth), then time 20 physics steps.
            EnemyCrowd.Rebuild();
            foreach (var e in horde) EnemyCrowd.Separation(e);
            var clock = System.Diagnostics.Stopwatch.StartNew();
            for (int step = 0; step < 20; step++)
            {
                EnemyCrowd.Rebuild();
                foreach (var e in horde) EnemyCrowd.Separation(e);
            }
            clock.Stop();
            double perStepMs = clock.Elapsed.TotalMilliseconds / 20.0;
            Debug.Log($"[EnemyCrowdTests] 300 packed enemies: {perStepMs:0.000} ms per rebuild.");
            Assert.That(perStepMs, Is.LessThan(1.5), $"Crowd separation took {perStepMs:0.000} ms per frame.");
        }

        [Test]
        public void Separation_BigBodies_PushFromFurtherAway()
        {
            // A 3x boss (radius 1.5) overlaps an ordinary enemy 1.8 units away; two ordinary enemies would not.
            var boss = Enemy(new Vector3(0f, 1f, 0f), 3f);
            var minion = Enemy(new Vector3(1.8f, 1f, 0f));
            EnemyCrowd.Rebuild();
            Assert.That(boss.CrowdRadius, Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(EnemyCrowd.Separation(minion).x, Is.GreaterThan(0f));
        }
    }
}
