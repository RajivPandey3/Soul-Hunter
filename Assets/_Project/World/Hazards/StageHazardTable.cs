using System.Collections.Generic;
using UnityEngine;

namespace SoulHunter.Gameplay.Hazards
{
    /// <summary>The timed VS stage hazards Soul Hunter uses (owner request 2026-09-27).</summary>
    public enum StageHazardKind
    {
        /// <summary>VS "The Stalker": an untouchable chaser that follows the player for 2 minutes.</summary>
        DeathsShadow,
        /// <summary>VS "Shooting Star": ground markers, then a strike 2 s later (30 damage).</summary>
        SoulMeteors,
        /// <summary>VS "Pile Assault" / "Pile Shooter": a still ring of enemies around the player that shoots bullets.</summary>
        PileAssault,
    }

    public struct StageHazardEntry
    {
        public StageHazardKind Kind;
        public float TimeSeconds;

        public StageHazardEntry(StageHazardKind kind, float timeSeconds)
        {
            Kind = kind;
            TimeSeconds = timeSeconds;
        }
    }

    /// <summary>
    /// Per-stage hazard schedule and the pure maths behind the hazards (EditMode-testable). VS rules come from
    /// vampire.survivors.wiki (Dairy Plant: Stalker 120 s, Pile Shooter ellipse ring, dairy carts; Gallo Tower: Shooting
    /// Star markers hit after 2 s for 30 damage; Pile Assault shoots bullets at the player). Which Soul Hunter stage gets
    /// which hazard, the times and every tuning number below are Soul Hunter choices and provisional.
    /// </summary>
    public static class StageHazardTable
    {
        // ---- Death's Shadow (VS Stalker) ----
        public const float ShadowLifetimeSeconds = 120f;  // VS: follows the player for 2 minutes
        public const float ShadowScale = 1.5f;
        public const int ShadowContactDamage = 30;
        public const float ShadowMoveSpeed = 5f;           // the player's base move speed (PlayerController)
        public const float ShadowSpawnDistance = 18f;      // just off-screen
        public static readonly int[] ShadowStages = { 5, 9 };
        public static readonly float[] ShadowMinutes = { 8f, 12f };

        // ---- Soul Meteors (VS Shooting Star) ----
        public const float MeteorDelaySeconds = 2f;        // VS: impact 2 s after the marker appears
        public const int MeteorPlayerDamage = 30;          // VS: 30 damage
        public const int MeteorEnemyDamage = 30;
        public const float MeteorRadius = 1.6f;
        public const int MeteorVolleyCount = 8;
        public const float MeteorStaggerSeconds = 0.2f;    // markers of a volley appear one after another
        public const float MeteorMinSpread = 2f;
        public const float MeteorMaxSpread = 7f;
        public static readonly int[] MeteorStages = { 6, 10 };
        public const float MeteorFirstMinute = 2f;
        public const float MeteorLastMinute = 28f;         // every even minute up to the 30:00 stage end

        // ---- Pile Assault ----
        public const int PileCount = 12;
        public const float PileRadiusX = 10f;              // an ellipse, wider than tall like the screen
        public const float PileRadiusZ = 7f;
        public const float PileDurationSeconds = 20f;
        public const float PileFireIntervalSeconds = 2.5f;
        public const float PileBulletSpeed = 4f;           // slow bullets
        public const int PileBulletDamage = 10;
        public const float PileBulletLifetime = 6f;
        public const int PileStage = 8;
        public static readonly float[] PileMinutes = { 9f, 10f, 12f, 20f };

        // ---- Rail carts (VS Dairy Plant carts) ----
        public const int RailCartStage = 3;
        public const int RailCount = 2;

        /// <summary>A missed hazard (the clock was skipped more than this far past it) does not fire.</summary>
        public const float MissedAfterSeconds = 10f;

        /// <summary>Timed hazards of a stage (1-10), sorted by time. Rail carts are not timed (HasRailCarts).</summary>
        public static List<StageHazardEntry> For(int stage)
        {
            var list = new List<StageHazardEntry>();
            if (System.Array.IndexOf(ShadowStages, stage) >= 0)
                foreach (float minute in ShadowMinutes) list.Add(new StageHazardEntry(StageHazardKind.DeathsShadow, minute * 60f));
            if (System.Array.IndexOf(MeteorStages, stage) >= 0)
                for (float minute = MeteorFirstMinute; minute <= MeteorLastMinute + 0.01f; minute += 2f)
                    list.Add(new StageHazardEntry(StageHazardKind.SoulMeteors, minute * 60f));
            if (stage == PileStage)
                foreach (float minute in PileMinutes) list.Add(new StageHazardEntry(StageHazardKind.PileAssault, minute * 60f));
            list.Sort((a, b) => a.TimeSeconds.CompareTo(b.TimeSeconds));
            return list;
        }

        public static bool HasRailCarts(int stage) => stage == RailCartStage;

        /// <summary>Meteor tint: falling ice on the Frozen Peaks, soul fire elsewhere.</summary>
        public static Color MeteorColor(int stage) =>
            stage == 6 ? new Color(0.55f, 0.85f, 1f, 1f) : new Color(1f, 0.35f, 0.25f, 1f);

        /// <summary>
        /// Where a volley's marker lands relative to the player. The first marker is on the player (standing still is
        /// never safe); the others ring the player between MeteorMinSpread and MeteorMaxSpread.
        /// </summary>
        public static Vector3 MeteorOffset(int index, int count, float angleRoll01, float distanceRoll01)
        {
            if (index <= 0 || count <= 1) return Vector3.zero;
            float step = 360f / (count - 1);
            float angle = (index - 1) * step + (Mathf.Clamp01(angleRoll01) - 0.5f) * step * 0.6f;
            float distance = Mathf.Lerp(MeteorMinSpread, MeteorMaxSpread, Mathf.Clamp01(distanceRoll01));
            return Quaternion.Euler(0f, angle, 0f) * Vector3.forward * distance;
        }

        /// <summary>A pile enemy's spot on the ellipse ring around the player.</summary>
        public static Vector3 PileOffset(int index, int count)
        {
            float angle = Mathf.PI * 2f * index / Mathf.Max(1, count);
            return new Vector3(Mathf.Cos(angle) * PileRadiusX, 0f, Mathf.Sin(angle) * PileRadiusZ);
        }

        /// <summary>Speed multiplier that gives an enemy the Death's Shadow speed whatever its data speed.</summary>
        public static float ShadowSpeedMultiplier(float enemyBaseSpeed) =>
            ShadowMoveSpeed / Mathf.Max(0.1f, enemyBaseSpeed);

        /// <summary>
        /// Candidate rails around the stage start: (centre offset, yaw). Yaw 0 runs along world X, 90 along Z. Nearest
        /// first; StageHazards takes the first ones on open ground that do not cross each other.
        /// </summary>
        public static List<(Vector3 offset, float yaw)> RailCandidates()
        {
            return new List<(Vector3, float)>
            {
                (new Vector3(0f, 0f, 10f), 0f), (new Vector3(0f, 0f, -10f), 0f),
                (new Vector3(10f, 0f, 0f), 90f), (new Vector3(-10f, 0f, 0f), 90f),
                (new Vector3(0f, 0f, 14f), 0f), (new Vector3(0f, 0f, -14f), 0f),
                (new Vector3(14f, 0f, 0f), 90f), (new Vector3(-14f, 0f, 0f), 90f),
                (new Vector3(0f, 0f, 18f), 0f), (new Vector3(0f, 0f, -18f), 0f),
            };
        }

        /// <summary>The two ends of a rail of this length, centred on centre and turned by yaw.</summary>
        public static (Vector3 a, Vector3 b) RailEnds(Vector3 centre, float yaw, float length)
        {
            Vector3 along = Quaternion.Euler(0f, yaw, 0f) * Vector3.right * (length * 0.5f);
            return (centre - along, centre + along);
        }

        /// <summary>Distance on the ground plane from p to the segment a-b.</summary>
        public static float SegmentDistanceXZ(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector2 a2 = new Vector2(a.x, a.z), b2 = new Vector2(b.x, b.z), p2 = new Vector2(p.x, p.z);
            Vector2 ab = b2 - a2;
            float len = ab.sqrMagnitude;
            float t = len > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p2 - a2, ab) / len) : 0f;
            return Vector2.Distance(p2, a2 + ab * t);
        }

        /// <summary>True when two ground segments stay at least minGap apart (they neither cross nor touch).</summary>
        public static bool SegmentsApart(Vector3 a0, Vector3 a1, Vector3 b0, Vector3 b1, float minGap)
        {
            if (SegmentsCrossXZ(a0, a1, b0, b1)) return false;
            float d = Mathf.Min(Mathf.Min(SegmentDistanceXZ(a0, a1, b0), SegmentDistanceXZ(a0, a1, b1)),
                                Mathf.Min(SegmentDistanceXZ(b0, b1, a0), SegmentDistanceXZ(b0, b1, a1)));
            return d >= minGap;
        }

        private static bool SegmentsCrossXZ(Vector3 p1, Vector3 p2, Vector3 q1, Vector3 q2)
        {
            float Cross(Vector3 o, Vector3 a, Vector3 b) => (a.x - o.x) * (b.z - o.z) - (a.z - o.z) * (b.x - o.x);
            float d1 = Cross(q1, q2, p1), d2 = Cross(q1, q2, p2), d3 = Cross(p1, p2, q1), d4 = Cross(p1, p2, q2);
            return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
        }
    }
}
