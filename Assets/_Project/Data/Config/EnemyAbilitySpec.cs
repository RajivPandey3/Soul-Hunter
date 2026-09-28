using System;
using UnityEngine;

namespace SoulHunter.Gameplay.Data
{
    /// <summary>
    /// The VS enemy and boss skills Soul Hunter can give an enemy through its EnemyData (EnemyData.Abilities).
    /// Every kind follows a named VS enemy on vampire.survivors.wiki; see EnemyAbilityDefaults for the numbers.
    /// </summary>
    public enum EnemyAbilityKind
    {
        /// <summary>Calls a ring of enemies around the player (VS The Trickster, The Directer).</summary>
        Summon = 0,
        /// <summary>Fires Fixed Direction bullets at the player (VS Twin Skulls, Twin Snakes, The Ender's Scythe Bullets).</summary>
        Shoot = 1,
        /// <summary>Dashes at the player for a moment (VS Orochimario's heads, The Stalker's charge).</summary>
        Lunge = 2,
        /// <summary>Absorbs all damage for a while after it appears (VS The Ender's shield).</summary>
        InvulnerablePhase = 3,
        /// <summary>Arms when close to the player and explodes after a fuse (VS Sig.ra Rossi).</summary>
        SelfDestruct = 4,
        /// <summary>Lays a zone on the player that hurts periodically (VS The Ender's zones, The Drowner's flood).</summary>
        HazardZone = 5,
        /// <summary>A large pulse around the enemy that halves the player's health (VS Cosmic Egg).</summary>
        HalvingPulse = 6,
    }

    /// <summary>
    /// One enemy skill. Every number left at 0 takes the kind's default (EnemyAbilityDefaults.For), so a designer can
    /// add an entry, pick its Kind and get the VS behaviour; set a number to change it. Which fields each kind reads:
    /// <list type="bullet">
    /// <item>Summon: Interval, FirstDelay, SpawnBursts, Count (enemies per ring), Radius (ring around the player), MaxAlive, Prefab (empty = the stage's normal enemy).</item>
    /// <item>Shoot: Interval, FirstDelay, ShrinkIntervalWithHealth + MinInterval, Count (bullets per volley), SpreadDegrees, Speed, Damage, Duration (bullet lifetime).</item>
    /// <item>Lunge: Interval, MaxInterval (random between the two), FirstDelay, SpeedMultiplier, Duration.</item>
    /// <item>InvulnerablePhase: Duration (from when the enemy appears), DiscardAbsorbedDamage.</item>
    /// <item>SelfDestruct: ArmDistance, Fuse, Damage, Radius, Interval (re-arm time, only for stage bosses, which do not vanish).</item>
    /// <item>HazardZone: Interval, FirstDelay, Count (zones per cast), Radius, Damage, TickSeconds, Duration, PlacementSpread.</item>
    /// <item>HalvingPulse: Interval, FirstDelay, Radius.</item>
    /// </list>
    /// </summary>
    [Serializable]
    public class EnemyAbilitySpec
    {
        public EnemyAbilityKind Kind;

        [Tooltip("Seconds between uses (0 = the kind's default). Lunge: the shortest wait")]
        [Min(0f)] public float IntervalSeconds;
        [Tooltip("Lunge: the longest wait; a random wait between Interval and this (0 = the kind's default)")]
        [Min(0f)] public float MaxIntervalSeconds;
        [Tooltip("Seconds before the first use (0 = the kind's default; below 0 = one interval; 0.01 = at once)")]
        public float FirstDelaySeconds;
        [Tooltip("Shoot: the interval shrinks linearly with health left, down to MinInterval at 0 HP (VS The Ender)")]
        public bool ShrinkIntervalWithHealth;
        [Tooltip("Shoot with ShrinkIntervalWithHealth: interval at 0 HP (0 = the kind's default)")]
        [Min(0f)] public float MinIntervalSeconds;

        [Tooltip("Summon: enemies per ring. Shoot: bullets per volley. HazardZone: zones per cast (0 = the kind's default)")]
        [Min(0)] public int Count;
        [Tooltip("Summon: rings when the enemy appears (0 = the kind's default; below 0 = none)")]
        public int SpawnBursts;
        [Tooltip("Summon: the most summoned enemies alive at once (0 = the kind's default)")]
        [Min(0)] public int MaxAlive;
        [Tooltip("Summon: the enemy prefab to call (empty = the stage's normal enemy)")]
        public GameObject Prefab;

        [Tooltip("Shoot: bullet speed in units/s (0 = the kind's default)")]
        [Min(0f)] public float Speed;
        [Tooltip("Shoot: degrees between the bullets of one volley")]
        [Min(0f)] public float SpreadDegrees;
        [Tooltip("Lunge: speed as a multiple of the enemy's move speed (0 = the kind's default)")]
        [Min(0f)] public float SpeedMultiplier;
        [Tooltip("Damage to the player: per bullet, per zone tick or per explosion (0 = the kind's default)")]
        [Min(0)] public int Damage;
        [Tooltip("Summon ring, zone, explosion or pulse radius in units (0 = the kind's default)")]
        [Min(0f)] public float Radius;
        [Tooltip("Shoot: bullet lifetime. Lunge: dash time. InvulnerablePhase: shield time. HazardZone: zone lifetime (0 = the kind's default)")]
        [Min(0f)] public float DurationSeconds;
        [Tooltip("HazardZone: seconds between damage ticks (0 = the kind's default)")]
        [Min(0f)] public float TickSeconds;
        [Tooltip("HazardZone: zones land up to this far from the player (0 = on the player)")]
        [Min(0f)] public float PlacementSpread;
        [Tooltip("SelfDestruct: arms when the player is this close (0 = the kind's default)")]
        [Min(0f)] public float ArmDistance;
        [Tooltip("SelfDestruct: seconds from arming to the explosion (0 = the kind's default)")]
        [Min(0f)] public float FuseSeconds;
        [Tooltip("InvulnerablePhase: forget the absorbed damage instead of dealing it when the shield ends (VS deals it)")]
        public bool DiscardAbsorbedDamage;

        public EnemyAbilitySpec() { }
        public EnemyAbilitySpec(EnemyAbilityKind kind) { Kind = kind; }

        /// <summary>This spec with every 0 replaced by the kind's default.</summary>
        public EnemyAbilitySpec Resolved()
        {
            var d = EnemyAbilityDefaults.For(Kind);
            var r = (EnemyAbilitySpec)MemberwiseClone();
            r.IntervalSeconds = Pick(IntervalSeconds, d.IntervalSeconds);
            r.MaxIntervalSeconds = Pick(MaxIntervalSeconds, d.MaxIntervalSeconds);
            r.FirstDelaySeconds = FirstDelaySeconds != 0f ? FirstDelaySeconds : d.FirstDelaySeconds;
            r.MinIntervalSeconds = Pick(MinIntervalSeconds, d.MinIntervalSeconds);
            r.Count = Count > 0 ? Count : d.Count;
            r.SpawnBursts = SpawnBursts != 0 ? Mathf.Max(0, SpawnBursts) : d.SpawnBursts;
            r.MaxAlive = MaxAlive > 0 ? MaxAlive : d.MaxAlive;
            r.Speed = Pick(Speed, d.Speed);
            r.SpeedMultiplier = Pick(SpeedMultiplier, d.SpeedMultiplier);
            r.Damage = Damage > 0 ? Damage : d.Damage;
            r.Radius = Pick(Radius, d.Radius);
            r.DurationSeconds = Pick(DurationSeconds, d.DurationSeconds);
            r.TickSeconds = Pick(TickSeconds, d.TickSeconds);
            r.ArmDistance = Pick(ArmDistance, d.ArmDistance);
            r.FuseSeconds = Pick(FuseSeconds, d.FuseSeconds);
            return r;
        }

        private static float Pick(float value, float fallback) => value > 0f ? value : fallback;

        /// <summary>
        /// Seconds until the next use. Shrinking with health (VS The Ender, vampire.survivors.wiki/w/The_Ender: "The
        /// interval between scythes decreases linearly based on the percentage of health remaining, to a minimum of 0.5
        /// seconds"); a random wait between Interval and MaxInterval (VS Orochimario: "every 5 to 10 seconds"); else Interval.
        /// </summary>
        public static float NextInterval(EnemyAbilitySpec resolved, float healthFraction, float random01)
        {
            if (resolved == null) return 1f;
            float interval = Mathf.Max(0.05f, resolved.IntervalSeconds);
            if (resolved.ShrinkIntervalWithHealth)
                return Mathf.Lerp(Mathf.Min(interval, Mathf.Max(0.05f, resolved.MinIntervalSeconds)), interval, Mathf.Clamp01(healthFraction));
            if (resolved.MaxIntervalSeconds > interval)
                return Mathf.Lerp(interval, resolved.MaxIntervalSeconds, Mathf.Clamp01(random01));
            return interval;
        }
    }

    /// <summary>
    /// Default numbers per ability kind. VS numbers come from vampire.survivors.wiki (pages named per entry, read
    /// 2026-09-28); anything the wiki does not give is Soul Hunter's own and marked provisional.
    /// VS speeds are in VS units: Soul Hunter's enemies move 3 units/s where the VS Zombie and Skeleton move 100
    /// (vampire.survivors.wiki/w/Zombie, /w/Skeleton), so one VS speed unit is taken as 0.03 units/s (assumption).
    /// </summary>
    public static class EnemyAbilityDefaults
    {
        public const float VsSpeedToUnits = 3f / 100f;

        public static EnemyAbilitySpec For(EnemyAbilityKind kind)
        {
            var s = new EnemyAbilitySpec(kind);
            switch (kind)
            {
                case EnemyAbilityKind.Summon:
                    // VS The Trickster (vampire.survivors.wiki/w/The_Trickster): "summons ellipsoid rings of 48 Poltergeist
                    // Gems around the player"; "two rings upon spawning and one every 30 seconds thereafter".
                    // Ring radius (8) and the cap (96 = two rings) are provisional.
                    s.IntervalSeconds = 30f; s.FirstDelaySeconds = -1f; s.SpawnBursts = 2; s.Count = 48;
                    s.Radius = 8f; s.MaxAlive = 96;
                    break;
                case EnemyAbilityKind.Shoot:
                    // VS Twin Skulls (vampire.survivors.wiki/w/Twin_Skulls): "shoots bullets, once every second"; Bullet
                    // movespeed 250, damage 25. The Ender (/w/The_Ender): Scythe Bullets every 5 s, shrinking to 0.5 s.
                    // Bullet lifetime (6 s) is provisional (the Pile Assault value).
                    s.IntervalSeconds = 1f; s.FirstDelaySeconds = -1f; s.MinIntervalSeconds = 0.5f; s.Count = 1;
                    s.Speed = 250f * VsSpeedToUnits; s.Damage = 25; s.DurationSeconds = 6f;
                    break;
                case EnemyAbilityKind.Lunge:
                    // VS Orochimario (vampire.survivors.wiki/w/Orochimario): "The heads can lunge at the player to attack
                    // them every 5 to 10 seconds". Dash speed (3x) and time (0.6 s) are not on the wiki: provisional.
                    s.IntervalSeconds = 5f; s.MaxIntervalSeconds = 10f; s.FirstDelaySeconds = -1f;
                    s.SpeedMultiplier = 3f; s.DurationSeconds = 0.6f;
                    break;
                case EnemyAbilityKind.InvulnerablePhase:
                    // VS The Ender (vampire.survivors.wiki/w/The_Ender): "During the first 90 seconds of the fight, The Ender
                    // also has a shield that absorbs all incoming damage. After the shield expires after 90 seconds, all of
                    // the damage absorbed will be instantly dealt to The Ender."
                    s.DurationSeconds = 90f;
                    break;
                case EnemyAbilityKind.SelfDestruct:
                    // VS Sig.ra Rossi (vampire.survivors.wiki/w/Sig.ra_Rossi): "upon getting close enough it triggers its
                    // self-destruct, exploding 2 seconds later for 50 damage". Arm distance (2), blast radius (2.5) and a
                    // boss's re-arm time (5 s) are provisional.
                    s.ArmDistance = 2f; s.FuseSeconds = 2f; s.Damage = 50; s.Radius = 2.5f; s.IntervalSeconds = 5f;
                    break;
                case EnemyAbilityKind.HazardZone:
                    // VS The Ender (vampire.survivors.wiki/w/The_Ender): "The zones periodically deal 12 damage in the area
                    // and last 10 seconds". The Drowner (/w/The_Drowner) floods for 20 per tick. Cast interval (8 s), tick
                    // (1 s) and radius (2) are not on the wiki: provisional.
                    s.IntervalSeconds = 8f; s.FirstDelaySeconds = -1f; s.Count = 1; s.Damage = 12;
                    s.DurationSeconds = 10f; s.TickSeconds = 1f; s.Radius = 2f;
                    break;
                case EnemyAbilityKind.HalvingPulse:
                    // VS Cosmic Egg (vampire.survivors.wiki/w/Cosmic_Egg): "intermittently casts a large, circular ...
                    // pulse ... which also causes the player character's health to be halved if they are inside of its
                    // radius". The wiki gives no interval or radius: 10 s and 6 units are Soul Hunter's own, provisional.
                    s.IntervalSeconds = 10f; s.FirstDelaySeconds = -1f; s.Radius = 6f;
                    break;
            }
            return s;
        }
    }
}
