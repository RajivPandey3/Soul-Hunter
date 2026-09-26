using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SoulHunter.Gameplay.Data;
using UnityEngine;

namespace SoulHunter.Tests.EditMode
{
    /// <summary>
    /// Owner requirement (2026-09-26): on every stage at least 70% of the ground must be open for the player, and
    /// the player must not get stuck between obstacles. Each stage floor tile (20 x 20) is sampled on a 0.25 grid
    /// with the player's capsule plus some elbow room against the Obstacle layer; the open ground reachable from the
    /// tile centre (the spawn) or from the neighbouring tiles is what counts. Open ground that cannot be reached is a
    /// trap.
    /// </summary>
    public class FloorWalkabilityTests
    {
        public const float RequiredOpenShare = 0.70f;
        private const float PlayerRadius = 0.4f;      // Player.prefab capsule
        private const float ElbowRoom = 0.35f;        // a gap must be ~1.5 bodies wide to count as walkable
        private const float Step = 0.25f;
        private const float Half = 10f;

        public struct Walkability
        {
            public float Open;       // share of the tile where the player fits with elbow room
            public float Reachable;  // share of the tile reachable from the centre
            public float Trapped;    // open but unreachable
            public string TrappedAt; // where the trapped ground is (for the failure message)
        }

        public static Walkability Measure(GameObject floorPrefab)
        {
            var floor = Object.Instantiate(floorPrefab, Vector3.zero, Quaternion.identity);
            try
            {
                Physics.SyncTransforms();
                int mask = LayerMask.GetMask("Obstacle");
                float radius = PlayerRadius + ElbowRoom;
                int n = Mathf.RoundToInt(2f * Half / Step);
                var open = new bool[n, n];
                int openCount = 0;
                for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    var p = new Vector3(-Half + (i + 0.5f) * Step, 0f, -Half + (j + 0.5f) * Step);
                    // The blockers are one non-convex MeshCollider (surfaces, not solids), so a capsule wholly inside
                    // a blocker box touches nothing: a ray down onto the box's top face marks the inside as blocked.
                    bool free = !Physics.CheckCapsule(p + Vector3.up * (radius + 0.05f), p + Vector3.up * 1.6f, radius, mask, QueryTriggerInteraction.Collide)
                                && !Physics.Raycast(p + Vector3.up * 20f, Vector3.down, 20f, mask, QueryTriggerInteraction.Collide);
                    open[i, j] = free;
                    if (free) openCount++;
                }

                // Flood fill from the centre (the spawn) and from the open tile border: tiles repeat in every
                // direction, and the generator keeps a clear strip along every edge, so the ground continues there.
                var seen = new bool[n, n];
                var queue = new Queue<Vector2Int>();
                int reached = 0;
                void Seed(int x, int y)
                {
                    if (!open[x, y] || seen[x, y]) return;
                    seen[x, y] = true;
                    queue.Enqueue(new Vector2Int(x, y));
                }
                Seed(n / 2, n / 2);
                for (int k = 0; k < n; k++) { Seed(k, 0); Seed(k, n - 1); Seed(0, k); Seed(n - 1, k); }
                while (queue.Count > 0)
                {
                    var c = queue.Dequeue();
                    reached++;
                    foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                    {
                        var m = c + d;
                        if (m.x < 0 || m.y < 0 || m.x >= n || m.y >= n || seen[m.x, m.y] || !open[m.x, m.y]) continue;
                        seen[m.x, m.y] = true;
                        queue.Enqueue(m);
                    }
                }
                float total = n * n;
                var trappedAt = new List<string>();
                for (int i = 0; i < n && trappedAt.Count < 4; i++)
                for (int j = 0; j < n && trappedAt.Count < 4; j++)
                    if (open[i, j] && !seen[i, j]) trappedAt.Add($"({-Half + (i + 0.5f) * Step:0.0}, {-Half + (j + 0.5f) * Step:0.0})");
                return new Walkability { Open = openCount / total, Reachable = reached / total, Trapped = (openCount - reached) / total,
                    TrappedAt = string.Join(" ", trappedAt) };
            }
            finally
            {
                Object.DestroyImmediate(floor);
            }
        }

        [Test]
        public void EveryStage_KeepsSeventyPercentOfTheGroundOpenAndReachable_WithNoTraps()
        {
            var floors = GameContentCatalog.Load().StageFloors;
            Assert.That(floors.Length, Is.EqualTo(10));
            var report = new StringBuilder();
            var failures = new List<string>();
            for (int i = 0; i < floors.Length; i++)
            {
                var w = Measure(floors[i]);
                string line = $"L{i + 1:00} open {w.Open:P0}, reachable {w.Reachable:P0}, trapped {w.Trapped:P1}" +
                              (w.Trapped > 0f ? $" at {w.TrappedAt}" : "");
                report.AppendLine(line);
                if (w.Reachable < RequiredOpenShare || w.Trapped > 0.01f) failures.Add(line);
            }
            Debug.Log("[FloorWalkabilityTests]\n" + report);
            Assert.That(failures, Is.Empty, "Stages below 70% reachable ground or with trapped pockets:\n" + string.Join("\n", failures) + "\nAll:\n" + report);
        }
    }
}
