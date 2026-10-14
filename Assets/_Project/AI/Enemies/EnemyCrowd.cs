using UnityEngine;

namespace SoulHunter.Gameplay.AI
{
    /// <summary>
    /// VS rule: enemies push each other, so a chasing horde forms a wide crowd instead of stacking on one path.
    /// Before this every enemy homed on the player at the same speed with trigger bodies, so they merged onto the
    /// same line and trailed a running player in single file (owner: "rail ka dabba").
    /// Bodies stay triggers (the horde never walls the player in); the push is a soft steering term. Once per
    /// physics step the whole horde is read into arrays, bucketed in a spatial grid and every push is computed in
    /// one pass of plain math; enemies then read their own result.
    /// </summary>
    public static class EnemyCrowd
    {
        /// <summary>Grid cell size in world units (one ordinary body across).</summary>
        public const float CellSize = 1f;
        /// <summary>Push speed at full overlap, as a fraction of the enemy's move speed. Provisional.</summary>
        public const float Strength = 1.2f;
        /// <summary>Off only for A/B checks (tests; profiling with -sh-crowd-off): enemies then home without pushing each other.</summary>
        public static bool Enabled = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-sh-crowd-off") < 0;
        /// <summary>Neighbours counted per enemy, to bound the cost inside a very dense crowd.</summary>
        public const int MaxNeighbours = 16;
        /// <summary>Grid cells at most; a horde spread wider than this uses bigger cells.</summary>
        private const int MaxCells = 16384;

        private static EnemyController[] _enemies = new EnemyController[512];
        private static Vector2[] _positions = new Vector2[512];
        private static float[] _radii = new float[512];
        private static int[] _ids = new int[512];
        private static int[] _cellOf = new int[512];
        private static int[] _sorted = new int[512];
        private static int[] _cellStart = new int[MaxCells + 1];
        private static int _count;
        private static int _builtFrame = -1;
        private static int _rebuilds;

        /// <summary>Rendered frames one set of pushes serves in play: a crowd moves under 0.2 units in two frames.</summary>
        public const int FramesPerRebuild = 2;

        /// <summary>Rebuilds the pushes when they are FramesPerRebuild frames old (EnemyRunner, before it moves the horde).</summary>
        internal static void EnsureBuilt()
        {
            if (!Enabled || !Application.isPlaying) return;
            int age = Time.frameCount - _builtFrame;
            if (age >= FramesPerRebuild || age < 0) Rebuild(); // below 0: a new play session without a domain reload
        }

        /// <summary>
        /// Recomputes every enemy's push. Called lazily once per rendered frame (at low frame rates one frame runs
        /// several physics steps; the crowd barely moves between them). Tests call it directly.
        /// </summary>
        public static void Rebuild()
        {
            _builtFrame = Time.frameCount;
            _count = 0;
            // In play the live list holds only enabled enemies (OnDisable removes them), so the per-enemy engine checks
            // are skipped; a body's size is read afresh for a new spawn and every 16th rebuild (lossyScale is costly).
            bool playing = Application.isPlaying;
            bool refreshRadii = !playing || (_rebuilds++ & 15) == 0;
            var live = EnemyController.ActiveEnemies;
            if (_enemies.Length < live.Count)
            {
                int size = Mathf.NextPowerOfTwo(live.Count);
                _enemies = new EnemyController[size];
                _positions = new Vector2[size];
                _radii = new float[size];
                _ids = new int[size];
                _cellOf = new int[size];
                _sorted = new int[size];
            }

            // 1. Read the horde once (the only engine calls).
            float maxRadius = 0f;
            float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
            for (int i = 0; i < live.Count; i++)
            {
                var enemy = live[i];
                // Event fliers keep their formation and path; they neither push nor get pushed.
                if (ReferenceEquals(enemy, null)) continue;
                if (!playing && (!enemy || !enemy.isActiveAndEnabled)) continue;
                if (enemy.IsFlying) continue;
                Vector3 position = playing ? enemy.CachedTransform.position : enemy.transform.position;
                int index = _count++;
                _enemies[index] = enemy;
                _positions[index] = new Vector2(position.x, position.z);
                float radius = enemy.CrowdRadiusCached(refreshRadii);
                _radii[index] = radius;
                _ids[index] = enemy.CrowdId;
                maxRadius = Mathf.Max(maxRadius, radius);
                minX = Mathf.Min(minX, position.x); maxX = Mathf.Max(maxX, position.x);
                minZ = Mathf.Min(minZ, position.z); maxZ = Mathf.Max(maxZ, position.z);
            }
            if (_count == 0) return;

            // 2. Bucket them in a flat grid (counting sort): no dictionaries, no allocation.
            float cell = CellSize;
            int width, height;
            while (true)
            {
                width = Mathf.FloorToInt((maxX - minX) / cell) + 1;
                height = Mathf.FloorToInt((maxZ - minZ) / cell) + 1;
                if ((long)width * height <= MaxCells) break;
                cell *= 2f;
            }
            int cells = width * height;
            System.Array.Clear(_cellStart, 0, cells + 1);
            for (int i = 0; i < _count; i++)
            {
                int cx = Mathf.Min(width - 1, (int)((_positions[i].x - minX) / cell));
                int cz = Mathf.Min(height - 1, (int)((_positions[i].y - minZ) / cell));
                int c = cz * width + cx;
                _cellOf[i] = c;
                _cellStart[c + 1]++;
            }
            for (int c = 0; c < cells; c++) _cellStart[c + 1] += _cellStart[c];
            // Fill using a running cursor per cell (reuses _cellStart shifted by one, then restores it).
            for (int i = 0; i < _count; i++) _sorted[_cellStart[_cellOf[i]]++] = i;
            for (int c = cells; c > 0; c--) _cellStart[c] = _cellStart[c - 1];
            _cellStart[0] = 0;

            // 3. Every push, in plain math.
            for (int i = 0; i < _count; i++)
            {
                Vector2 self = _positions[i];
                float radius = _radii[i];
                int range = Mathf.Max(1, Mathf.CeilToInt((radius + maxRadius) / cell));
                int cx = _cellOf[i] % width, cz = _cellOf[i] / width;
                int x0 = Mathf.Max(0, cx - range), x1 = Mathf.Min(width - 1, cx + range);
                int z0 = Mathf.Max(0, cz - range), z1 = Mathf.Min(height - 1, cz + range);
                Vector2 push = Vector2.zero;
                int seen = 0;
                for (int z = z0; z <= z1 && seen < MaxNeighbours; z++)
                {
                    int row = z * width;
                    for (int x = x0; x <= x1 && seen < MaxNeighbours; x++)
                    {
                        int end = _cellStart[row + x + 1];
                        for (int s = _cellStart[row + x]; s < end && seen < MaxNeighbours; s++)
                        {
                            int j = _sorted[s];
                            if (j == i) continue;
                            Vector2 delta = Push(self, radius, _positions[j], _radii[j], _ids[i], _ids[j]);
                            if (delta.x == 0f && delta.y == 0f) continue;
                            push += delta;
                            seen++;
                        }
                    }
                }
                if (push.sqrMagnitude > 1f) push.Normalize();
                _enemies[i].CrowdPush = new Vector3(push.x, 0f, push.y);
            }
            for (int i = 0; i < _count; i++) _enemies[i] = null; // do not keep destroyed enemies referenced
        }

        /// <summary>This enemy's flat push away from overlapping neighbours for the current physics step (length at most 1).</summary>
        public static Vector3 Separation(EnemyController self)
        {
            if (!Enabled || ReferenceEquals(self, null)) return Vector3.zero;
            // While EnemyRunner steps the horde the pushes are already built for this frame.
            if (EnemyController.InStep) return self.CrowdPush;
            // Recomputed once per frame, by the first enemy asking. EditMode has no frames: tests call Rebuild.
            if (_builtFrame != Time.frameCount && Application.isPlaying) Rebuild();
            return self.CrowdPush;
        }

        /// <summary>
        /// Push on a body at selfPos from one neighbour: away from it, weighted 1 - distance / (sum of radii),
        /// zero when not overlapping. Two bodies on the same spot split along a direction fixed by their ids,
        /// so they part the same way every step instead of jittering.
        /// </summary>
        public static Vector3 PushFrom(Vector3 selfPos, float selfRadius, Vector3 otherPos, float otherRadius, int selfId, int otherId)
        {
            Vector2 push = Push(new Vector2(selfPos.x, selfPos.z), selfRadius, new Vector2(otherPos.x, otherPos.z), otherRadius, selfId, otherId);
            return new Vector3(push.x, 0f, push.y);
        }

        private static Vector2 Push(Vector2 self, float selfRadius, Vector2 other, float otherRadius, int selfId, int otherId)
        {
            Vector2 offset = self - other;
            float minDistance = selfRadius + otherRadius;
            float sqr = offset.sqrMagnitude;
            if (minDistance <= 0f || sqr >= minDistance * minDistance) return Vector2.zero;
            float distance = Mathf.Sqrt(sqr);
            if (distance < 0.0001f)
            {
                // Same angle for both members of the pair (order-independent hash), opposite sides.
                int low = Mathf.Min(selfId, otherId), high = Mathf.Max(selfId, otherId);
                float angle = (((low * 73856093) ^ (high * 19349663)) % 360) * Mathf.Deg2Rad;
                if (selfId != low) angle += Mathf.PI;
                return new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
            }
            return offset / distance * (1f - distance / minDistance);
        }
    }
}
