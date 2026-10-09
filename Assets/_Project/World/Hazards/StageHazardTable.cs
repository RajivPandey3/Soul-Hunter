using System.Collections.Generic;
using UnityEngine;
using SoulHunter.Gameplay.AI;
using SoulHunter.Gameplay.Combat;
using SoulHunter.Gameplay.Core;
using SoulHunter.Gameplay.Data;

namespace SoulHunter.Gameplay.Hazards
{
    /// <summary>The timed VS stage hazards Soul Hunter uses (owner request 2026-09-27).</summary>
    public enum StageHazardKind
    {
        /// <summary>VS "The Stalker XL": a chaser only instant kills can defeat; follows the player for 1 or 2 minutes.</summary>
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
        /// <summary>VS map-event chance in % (100 = always); divided by Luck when rolled (StageHazards).</summary>
        public float ChancePercent;
        /// <summary>How long the hazard stays, in seconds (0 = the kind's default).</summary>
        public float DurationSeconds;

        public StageHazardEntry(StageHazardKind kind, float timeSeconds, float chancePercent = 100f, float durationSeconds = 0f)
        {
            Kind = kind;
            TimeSeconds = timeSeconds;
            ChancePercent = chancePercent;
            DurationSeconds = durationSeconds;
        }
    }

    /// <summary>
    /// What defeated Death's Shadow (vampire.survivors.wiki/w/The_Stalker, "Behavior": "The Stalker is instantly killed
    /// upon taking damage from Pentagram, Gorgeous Moon, Infinite Corridor, Crimson Shroud, Victory Sword, Rosary, Just
    /// Vent, Grenade, or the Dairy Carts"). Soul Hunter has Pentagram, Eclipse Sigil (Gorgeous Moon), Crimson Shroud, Soul
    /// Toll (Rosary) and the Stage 3 rail carts (Dairy Cart); the others have no Soul Hunter counterpart.
    /// </summary>
    public enum ShadowKiller
    {
        /// <summary>Anything else: ordinary weapons, the stage-clear wipe. It cannot hurt Death's Shadow.</summary>
        None,
        Pentagram,
        EclipseSigil,
        CrimsonShroud,
        SoulToll,
        RailCart,
    }

    /// <summary>
    /// Per-stage hazard schedule and the pure maths behind the hazards (EditMode-testable). VS rules come from
    /// vampire.survivors.wiki (Dairy Plant: Stalker 120 s, Pile Shooter ellipse ring, dairy carts; Gallo Tower: Shooting
    /// Star markers hit after 2 s for 30 damage; Pile Assault shoots bullets at the player). Which Soul Hunter stage gets
    /// which hazard, the times and every tuning number below are Soul Hunter choices and provisional.
    /// </summary>
    public static class StageHazardTable
    {
        // ---- Death's Shadow (VS The Stalker XL) ----
        // Owner decision 2026-10-09: "use the latest VS Stalker". vampire.survivors.wiki/w/The_Stalker (raw, fetched
        // 2026-10-09) has three variants side by side; the XL (BOSS_XLSTALKER) is the one VS uses as a stage map event that
        // drops chests (Dairy Plant, The Bone Zone), which is what Death's Shadow is. Its statbox:
        // "| skills = [[HP x Level]] | health = 655,350 | movespeed = 300 | damage = 50 | knockback = 0 | knockback-max = 0
        //  | xp = 0 | res-freeze = 2 | res-kill = 0 | res-debuff = 1 | res-defang = 0".
        public const int ShadowHealthPerLevel = 655350;     // VS: health 655,350, HP x Level
        public const float ShadowVsMoveSpeed = 300f;        // VS: movespeed 300
        /// <summary>VS 300 in Soul Hunter units (EnemyAbilityDefaults.VsSpeedToUnits: VS 100 = 3 units/s), 9 units/s.</summary>
        public const float ShadowMoveSpeed = ShadowVsMoveSpeed * EnemyAbilityDefaults.VsSpeedToUnits;
        public const int ShadowContactDamage = 50;          // VS: damage 50
        public const float ShadowFreezeResistance = 2f;     // VS: res-freeze 2 (Clock Lancet's freeze chance is 1)
        public const bool ShadowInstantKillResistance = false; // VS: res-kill 0
        public const bool ShadowDebuffResistance = true;    // VS: res-debuff 1
        public const int ShadowExperience = 0;              // VS: xp 0 (it never drops a gem)
        public const float ShadowLifetimeSeconds = 120f;    // VS: "follows the player for 1 or 2 minutes" (the default)
        public const float ShadowScale = 1.5f;              // Soul Hunter's look: the stage boss model, 1.5x and dark
        public const float ShadowSpawnDistance = 18f;      // just off-screen
        /// <summary>Owner placement (2026-09-27): Soul Hunter's Stages 5 and 9.</summary>
        public static readonly int[] ShadowStages = { 5, 9 };
        /// <summary>
        /// The VS Dairy Plant rows of The Stalker XL, minute / % chance / seconds: "8:00 Event Spawn 1, lasts 2 minutes,
        /// 30% chance" and "12:00 Event Spawn 1, lasts 1 minute, 30% chance". The 0:00 row (1%) is left out: the owner kept
        /// Soul Hunter's 8:00 and 12:00 placement. Each chance is divided by Luck (wiki Luck, "Skipping events": "Most map
        /// events feature a chance for it to happen ... chanceWithLuck = eventChance / totalLuck").
        /// </summary>
        public static readonly (float minute, float chancePercent, float seconds)[] ShadowEvents =
            { (8f, 30f, 120f), (12f, 30f, 60f) };

        // The chase rhythm. The wiki: it "will follow the player around but will not actively make contact. Occasionally,
        // it will speed up or slow down, appearing to chase the player" (The_Stalker) and "greatly speeds up at fixed
        // intervals" (Random_Events). It gives no numbers, so these are Soul Hunter's balance choices (owner rule
        // 2026-10-09: choose by balance, record why):
        // - it trails at a gap for 8 s, then closes in for 2 s, and repeats. 8 + 2 s gives 12 lunges in the 120 s event
        //   and 6 in the 60 s one: often enough to be a threat, rare enough that the player can fight in between.
        // - both phases use the statbox speed (9 units/s). Soul Hunter's hero walks 5 units/s x the stage bonus (x1.40
        //   on Stages 5 and 9, 7 units/s), so a hero who keeps moving loses only 2 units of the gap per dash second and
        //   is not caught from the trailing gap in 2 s, while one who stands still is. No faster dash multiplier is
        //   invented; Curse and the run modes still speed it up ("Having a high Curse ... allows it to move very fast").
        // - the gap is its own body radius + 3 units, so the trailing shadow never touches the hero.
        public const float ShadowTrailSeconds = 8f;
        public const float ShadowDashSeconds = 2f;
        public const float ShadowTrailClearance = 3f;

        // ---- Soul Meteors (VS Shooting Star) ----
        public const float MeteorDelaySeconds = 2f;        // VS: impact 2 s after the marker appears
        public const int MeteorPlayerDamage = 30;          // VS: 30 damage
        public const int MeteorEnemyDamage = 30;
        public const float MeteorRadius = 1.6f;
        public const int MeteorVolleyCount = 6;             // balance 2026-09-28: 8 every 2 min killed a level-140 hero in 2.5 min
        public const float MeteorStaggerSeconds = 0.2f;    // markers of a volley appear one after another
        public const float MeteorMinSpread = 2f;
        public const float MeteorMaxSpread = 7f;
        public static readonly int[] MeteorStages = { 6, 10 };
        public const float MeteorFirstMinute = 4f;         // VS Gallo Tower: stars at 0, 4, 15, 22; here every 4 minutes
        public const float MeteorLastMinute = 28f;
        public const float MeteorEveryMinutes = 4f;

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
                foreach (var (minute, chance, seconds) in ShadowEvents)
                    list.Add(new StageHazardEntry(StageHazardKind.DeathsShadow, minute * 60f, chance, seconds));
            if (System.Array.IndexOf(MeteorStages, stage) >= 0)
                for (float minute = MeteorFirstMinute; minute <= MeteorLastMinute + 0.01f; minute += MeteorEveryMinutes)
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

        /// <summary>
        /// Speed multiplier that gives an enemy the Death's Shadow speed whatever its data speed. <paramref name="curseAndModes"/>
        /// is the player's Curse (as the spawner uses it for speed) x the run modes' enemy speed (Hyper, Inverse). The
        /// stage's own enemy pace is not applied: the XL has its own speed.
        /// </summary>
        public static float ShadowSpeedMultiplier(float enemyBaseSpeed, float curseAndModes = 1f) =>
            ShadowMoveSpeed * Mathf.Max(0.1f, curseAndModes) / Mathf.Max(0.1f, enemyBaseSpeed);

        /// <summary>VS "HP x Level": 655,350 x the player's level (capped at int.MaxValue).</summary>
        public static int ShadowHealth(int playerLevel) =>
            (int)System.Math.Min(int.MaxValue, (long)ShadowHealthPerLevel * System.Math.Max(1, playerLevel));

        /// <summary>The Luck-adjusted map-event roll (the same rule as the spawner's stage events).</summary>
        public static bool ShadowEventFires(float chancePercent, float luck, float roll01) =>
            EnemySpawner.EventFires(chancePercent, luck, roll01);

        /// <summary>
        /// The chest Death's Shadow drops when <paramref name="killer"/> defeats it, or null (The_Stalker: "Pentagram and
        /// Grenade cause no chest to drop, regardless of it being a good or bad proc"; Gorgeous Moon, Crimson Shroud and
        /// Rosary "cause a high-quality chest to drop"; "Dairy Cart ... cause a lower quality chest to drop").
        /// </summary>
        public static ChestTiers? ShadowChest(ShadowKiller killer)
        {
            switch (killer)
            {
                case ShadowKiller.EclipseSigil:
                case ShadowKiller.CrimsonShroud:
                case ShadowKiller.SoulToll:
                    return ChestTiers.StalkerHighQuality;
                case ShadowKiller.RailCart:
                    return ChestTiers.StalkerLowQuality;
                default:
                    return null; // Pentagram, or nothing that can kill it
            }
        }

        /// <summary>
        /// Which killer a hit or an instant kill comes from, by its damage source (<see cref="DamageSource"/>). Shadow
        /// Kael's mirrored weapons never count (they are aimed at the player).
        /// </summary>
        public static ShadowKiller KillerOf(AutoAttackWeapon weapon, string sourceName)
        {
            if (weapon != null)
            {
                if (weapon.IsHeldByEnemy) return ShadowKiller.None;
                if (weapon is EclipseSigilWeapon) return ShadowKiller.EclipseSigil;
                if (weapon is PentagramWeapon) return ShadowKiller.Pentagram;
                if (weapon is CrimsonShroudWeapon) return ShadowKiller.CrimsonShroud;
                return ShadowKiller.None;
            }
            if (sourceName == SoulHunter.Gameplay.Pickups.ScreenWipePickup.DisplayName) return ShadowKiller.SoulToll;
            if (sourceName == RailCart.SourceName) return ShadowKiller.RailCart;
            return ShadowKiller.None;
        }

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
