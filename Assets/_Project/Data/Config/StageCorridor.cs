using UnityEngine;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// A walled corridor stage (owner, 2026-09-26: the Ruined Castle as VS's Inlaid Library: "a long hallway with
    /// bookshelves along the top and bottom that restricts movement to left and right"; enemies come only from the
    /// west and east ends). The corridor runs along world X, centred on where the player stood when the stage began.
    /// Spawns, stragglers, events, lanterns, stage items and plates keep inside it; CorridorWalls draws and blocks it.
    /// </summary>
    public static class StageCorridor
    {
        /// <summary>Open half-width of the corridor between the walls' inner faces.</summary>
        public static float HalfWidth { get; private set; }
        public static float CentreZ { get; private set; }
        public static bool Active => HalfWidth > 0f;
        /// <summary>Bumped on every change, so CorridorWalls knows to rebuild.</summary>
        public static int Version { get; private set; }

        public static void Begin(float halfWidth, float centreZ)
        {
            HalfWidth = Mathf.Max(0f, halfWidth);
            CentreZ = centreZ;
            Version++;
        }

        public static void End() => Begin(0f, 0f);

        /// <summary>Keeps a point inside the corridor with a margin from the walls (no change when not a corridor stage).</summary>
        public static Vector3 Clamp(Vector3 point, float margin = 1f)
        {
            if (!Active) return point;
            float limit = Mathf.Max(0f, HalfWidth - margin);
            point.z = Mathf.Clamp(point.z, CentreZ - limit, CentreZ + limit);
            return point;
        }

        public static bool Contains(Vector3 point, float margin = 0f) =>
            !Active || Mathf.Abs(point.z - CentreZ) <= HalfWidth - margin;
    }
}
